#!/usr/bin/env bash
#
# generate-publicapi.sh — (re)generate PublicAPI baseline files for every shipped
# Refit library, across each target framework that builds on this machine.
#
# The Microsoft.CodeAnalysis.PublicApiAnalyzers (RS0016 / RS0017 / RS0037) require a
# per-TFM pair of tracking files:
#
#     <Project>/PublicAPI/<tfm>/PublicAPI.Shipped.txt
#     <Project>/PublicAPI/<tfm>/PublicAPI.Unshipped.txt
#
# This script seeds those files and uses `dotnet format analyzers` to capture the
# project's current public surface (RS0016), drop stale entries (RS0017), and record
# nullability (RS0037), then folds the surface into Shipped (this repo keeps the full
# surface in Shipped with Unshipped empty).
#
# Only projects with MSBuild property TrackPublicApi=true are processed; the tests/,
# benchmarks/, and examples/ trees, the source generators, and the AOT smoke app opt
# out centrally in src/Directory.Build.props.
#
# Each (project, TFM) pair is independent — `dotnet format` builds an in-memory
# MSBuildWorkspace and only writes its own PublicAPI/<tfm>/ files — so the pairs run
# in parallel through a bounded pool (override the width with JOBS=<n>).
#
# Refit ships only cross-platform libraries (no Apple/Android/Windows-desktop TFMs),
# so every target framework — including the .NET Framework legs via EnableWindowsTargeting
# — builds on Linux, macOS, or Windows alike.
#
# Usage:
#   tools/generate-publicapi.sh [project-name-filter]
#
# Examples:
#   tools/generate-publicapi.sh                 # all tracked libraries, all TFMs
#   tools/generate-publicapi.sh HttpClient      # only projects whose path contains 'HttpClient'
#   JOBS=4 tools/generate-publicapi.sh          # cap parallelism at 4
#
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(cd "$SCRIPT_DIR/../src" && pwd)"
cd "$SRC_DIR"

# MSBuild properties that `dotnet format` cannot accept via -p:; pass through the env.
export EnableWindowsTargeting=true
export CheckEolTargetFramework=false
export MinVerVersionOverride="${MinVerVersionOverride:-255.255.255-dev}"

FILTER="${1:-}"
export DIAGS="RS0016 RS0017 RS0037"
JOBS="${JOBS:-$(nproc 2>/dev/null || echo 4)}"
[ "$JOBS" -gt 8 ] && JOBS=8

echo "PublicAPI baseline generation"
echo "  src        : $SRC_DIR"
echo "  filter     : ${FILTER:-<none>}"
echo "  diagnostics: $DIAGS"
echo "  MinVer     : $MinVerVersionOverride"
echo "  jobs       : $JOBS"
echo

projects=()
while IFS= read -r p; do projects+=("$p"); done < <(
  find . -name '*.csproj' \
    -not -path '*/tests/*' -not -path '*/benchmarks/*' -not -path '*/examples/*' \
    | sort
)

# Collect (project|tfm) work items; the worker seeds, generates, and folds each pair.
items=()
restore_set=()
skipped=0
for proj in "${projects[@]}"; do
  if [ -n "$FILTER" ] && [[ "$proj" != *"$FILTER"* ]]; then continue; fi

  track="$(dotnet msbuild "$proj" -getProperty:TrackPublicApi -nologo 2>/dev/null | tr -d '[:space:]')"
  if [ "$track" != "true" ]; then
    echo "skip  (TrackPublicApi != true): $proj"
    skipped=$((skipped + 1))
    continue
  fi

  tfms="$(dotnet msbuild "$proj" -getProperty:TargetFrameworks -nologo 2>/dev/null | tr -d '[:space:]')"
  if [ -z "$tfms" ]; then
    tfms="$(dotnet msbuild "$proj" -getProperty:TargetFramework -nologo 2>/dev/null | tr -d '[:space:]')"
  fi
  if [ -z "$tfms" ]; then
    echo "skip  (no TargetFramework(s)): $proj"
    skipped=$((skipped + 1))
    continue
  fi

  projdir="$(dirname "$proj")"
  echo "queue $proj"
  echo "    TFMs: $tfms"
  restore_set+=("$proj")
  IFS=';' read -ra tfm_arr <<<"$tfms"
  for tfm in "${tfm_arr[@]}"; do
    [ -z "$tfm" ] && continue
    mkdir -p "$projdir/PublicAPI/$tfm"
    items+=("$proj|$tfm")
  done
done
echo

if [ "${#items[@]}" -eq 0 ]; then
  echo "Nothing to generate. projects skipped: $skipped"
  exit 0
fi

# Restore once per project so the parallel `dotnet format` workers never race on restore
# (they each load a read-only workspace afterwards).
echo "Restoring ${#restore_set[@]} project(s)..."
for proj in "${restore_set[@]}"; do
  dotnet restore "$proj" -v quiet || echo "    WARN: restore reported issues for $proj"
done
echo

# Worker: regenerate one (project, TFM) pair and fold the surface into Shipped.
generate_one() {
  local item="$1"
  local proj="${item%%|*}"
  local tfm="${item##*|}"
  local projdir apidir shipped unshipped tag
  projdir="$(dirname "$proj")"
  apidir="$projdir/PublicAPI/$tfm"
  shipped="$apidir/PublicAPI.Shipped.txt"
  unshipped="$apidir/PublicAPI.Unshipped.txt"
  tag="$(printf '%s' "$item" | tr '/|.' '___')"
  local bsh="$RESULTS_DIR/$tag.shipped.bak"
  local bun="$RESULTS_DIR/$tag.unshipped.bak"
  # Back up any existing baseline so a build failure restores it instead of wiping it.
  [ -f "$shipped" ] && cp "$shipped" "$bsh"
  [ -f "$unshipped" ] && cp "$unshipped" "$bun"
  # Empty both to the bare header so the analyzer reports the entire current surface.
  printf '#nullable enable\n' >"$shipped"
  printf '#nullable enable\n' >"$unshipped"
  if dotnet format analyzers "$proj" -f "$tfm" --diagnostics $DIAGS --severity info -v quiet; then
    # `dotnet format` records the surface in Unshipped; fold it into Shipped (ordinally
    # sorted+deduped, as the analyzer emits) and reset Unshipped to the bare header default.
    {
      printf '#nullable enable\n'
      grep -vxF '#nullable enable' "$unshipped" | grep -v '^[[:space:]]*$' | LC_ALL=C sort -u
    } >"$shipped"
    printf '#nullable enable\n' >"$unshipped"
    printf 'OK   [%s] %s\n' "$tfm" "$proj"
    : >"$RESULTS_DIR/$tag.ok"
  else
    # Restore the prior baseline (if any) so nothing is wiped for a TFM we can't build here.
    [ -f "$bsh" ] && cp "$bsh" "$shipped"
    [ -f "$bun" ] && cp "$bun" "$unshipped"
    printf 'FAIL [%s] %s\n' "$tfm" "$proj"
    : >"$RESULTS_DIR/$tag.fail"
  fi
}
export -f generate_one

RESULTS_DIR="$(mktemp -d)"
export RESULTS_DIR
trap 'rm -rf "$RESULTS_DIR"' EXIT

echo "Generating ${#items[@]} (project, TFM) baseline(s) across $JOBS job(s)..."
printf '%s\n' "${items[@]}" | xargs -P "$JOBS" -I{} bash -c 'generate_one "$1"' _ {}
echo

generated="$(find "$RESULTS_DIR" -name '*.ok' | wc -l | tr -d '[:space:]')"
failed="$(find "$RESULTS_DIR" -name '*.fail' | wc -l | tr -d '[:space:]')"

echo "Done. generated: $generated TFM baseline(s), failed: $failed, projects skipped: $skipped"
[ "$failed" -eq 0 ]
