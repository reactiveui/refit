# tools

Maintenance scripts for the Refit repository.

## generate-publicapi

Regenerates the **PublicAPI baseline files** consumed by
`Microsoft.CodeAnalysis.PublicApiAnalyzers` (`RS0016`, `RS0017`, `RS0037`).

Each shipped library tracks its public surface per target framework in:

```
src/<Project>/PublicAPI/<tfm>/PublicAPI.Shipped.txt
src/<Project>/PublicAPI/<tfm>/PublicAPI.Unshipped.txt
```

When you add, remove, or change public API, those files must be updated or the build
fails with `RS0016` (symbol not in baseline) / `RS0017` (baseline entry not found) /
`RS0037` (missing `#nullable enable`). These scripts do that for you.

Only projects with the MSBuild property `TrackPublicApi=true` are processed. The
`tests/`, `benchmarks/`, and `examples/` trees, the `InterfaceStubGenerator` source
generators, and the `Refit.NativeAotSmoke` app opt out centrally in
`src/Directory.Build.props`, so they are never touched.

### Usage

Linux / macOS:

```bash
tools/generate-publicapi.sh                 # all tracked libraries, all TFMs
tools/generate-publicapi.sh HttpClient      # only projects whose path contains 'HttpClient'
tools/generate-publicapi.sh Refit.Xml
```

Windows (PowerShell):

```powershell
./tools/generate-publicapi.ps1                     # all tracked libraries
./tools/generate-publicapi.ps1 -Filter HttpClient  # path filter
```

The optional argument is a case-sensitive substring matched against each project's
path, so you can scope a run to a single library while iterating.

### What it does per TFM

1. Resets both `PublicAPI/<tfm>/PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`
   to just `#nullable enable`, so the analyzer reports the *entire* current surface.
2. Runs `dotnet format analyzers <proj> -f <tfm> --diagnostics RS0016 RS0017 RS0037
   --severity info`, which fills `PublicAPI.Unshipped.txt` with the current public API.
3. Folds that surface into `PublicAPI.Shipped.txt` (ordinally sorted, deduped) and
   resets `PublicAPI.Unshipped.txt` back to the bare header. This repo keeps the full
   surface in **Shipped** with **Unshipped empty**, so a later API change shows up as
   new Unshipped lines.

### Platform notes

Refit ships only cross-platform libraries — there are **no Apple, Android, or
Windows-desktop target frameworks**. Every TFM, including the `.NET Framework` legs
(built via `EnableWindowsTargeting=true`), builds on Windows, Linux, and macOS alike,
so either script produces the same baselines on any OS. The bash and PowerShell
versions write LF-only files and sort identically, so their output is byte-for-byte
equal.

The scripts set `MinVerVersionOverride` (default `255.255.255-dev`) so versioning does
not depend on git history; override it by exporting/setting the variable first.

### When to run

* After changing any public (or `protected` on a public type) API.
* After adding a new target framework to a tracked library.
* After bumping an analyzer package that changes how the public surface is rendered.

Review the resulting `PublicAPI.Unshipped.txt` diff before committing — it is the
human-auditable record of your public API change.
