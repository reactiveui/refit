# CLAUDE.md

This file is the single source of truth for AI/agent assistance in this repository. It consolidates build/test commands, repository layout, test runner usage, and the main constraints needed to work safely in `Refit`.

If there is any conflict between other agent instruction files and this file, follow **CLAUDE.md**.

---

## Repository Orientation

- **Repository root:** `.`
- **Primary working directory for build/test:** `./src`
- **Main solution:** `src/Refit.slnx`
- **Tests:** `src/tests/`
- **Examples:** `src/examples/`
- **Benchmarks:** `src/Benchmarks/`

---

## Solution Format: SLNX

This repository uses **SLNX** (XML-based solution format) instead of legacy `.sln`.

- Main file: `src/Refit.slnx`
- Use `dotnet build` / `dotnet test` against the `.slnx` file the same way as a `.sln`

---

## Build Environment Requirements

### Working Directory Rule

**CRITICAL:** Run `dotnet` build/test commands from `./src`, not the repository root, unless the command explicitly uses `src/`-prefixed paths.

Running `dotnet test` from the repository root can trigger Microsoft Testing Platform / VSTest invocation issues on .NET 10+ SDKs.

### Restore And Build

```bash
cd src

dotnet restore "Refit.slnx"
dotnet build "Refit.slnx"
dotnet build "Refit.slnx" -c Release
dotnet clean "Refit.slnx"
```

### Targeted Build Examples

```bash
cd src

dotnet build "InterfaceStubGenerator.Roslyn50/InterfaceStubGenerator.Roslyn50.csproj" -v:minimal --no-restore /p:NuGetAudit=false /m:1
dotnet build "tests/Refit.GeneratedCode.TestModels/Refit.GeneratedCode.TestModels.csproj" -f net8.0 -v:minimal --no-restore /p:NuGetAudit=false /m:1
dotnet build "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 -v:minimal --no-restore /p:NuGetAudit=false /m:1
```

If you run from the repository root, use explicit `src/`-prefixed paths:

```bash
dotnet build "src/Refit.slnx"
```

---

## Testing: Microsoft Testing Platform (MTP) + TUnit

This repository uses **Microsoft Testing Platform (MTP)** with **TUnit**. This differs from VSTest.

- Test support is enabled centrally in `src/Directory.Build.props`
- Test execution settings live in `src/testconfig.json`
- Command-line filtering uses **TUnit/MTP** syntax, not NUnit/xUnit/VSTest filter syntax

### Testing Best Practices

- Do **not** use repository-root `dotnet test`
- Run `dotnet` commands from `./src`
- Prefer building before testing rather than relying on stale binaries
- Place TUnit/MTP-specific arguments **after** `--`
- Test projects are executable; `dotnet run --project ...` is a valid way to invoke the TUnit/MTP runner directly

### Test Commands

```bash
cd src

# Run all tests
dotnet test "Refit.slnx"

# Run a specific project
dotnet test "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0

# Direct TUnit/MTP runner invocation
dotnet run --project "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 --no-restore --no-build

# Detailed output (argument goes after --)
dotnet test "Refit.slnx" -- --output Detailed

# List tests for a project
dotnet test "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 -- --list-tests

# Fail fast
dotnet test "Refit.slnx" -- --fail-fast
```

### TUnit `--treenode-filter` Syntax

Pattern shape:

```text
/{AssemblyName}/{Namespace}/{ClassName}/{TestMethodName}
```

Examples:

```bash
cd src

# Single test
dotnet test "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 -- \
  --treenode-filter "/*/*/*/GeneratedRequestBuilding_CanBeEmittedLoadedAndInvoked"

# All tests in a class
dotnet test "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 -- \
  --treenode-filter "/*/*/GeneratedRequestBuildingTests/*"

# Direct runner with a filter
dotnet run --project "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 --no-restore --no-build -- \
  --treenode-filter "/*/*/GeneratedRequestBuildingTests/*"
```

---

## Source Generator Work

- Source generator projects live under `src/InterfaceStubGenerator.*`.
- Generated output is emitted to `obj/Generated` by default through `src/Directory.Build.props`.
- The generated-code compliance project is `src/tests/Refit.GeneratedCode.TestModels/Refit.GeneratedCode.TestModels.csproj`.
- `RefitEmitGeneratedCodeMarkers=false` is used by generated-code compliance tests so analyzers treat generator output as normal source.
- Keep generated source compatible with the repository `.editorconfig`; avoid broad `#pragma warning disable`.

Useful validation:

```bash
cd src

dotnet build "InterfaceStubGenerator.Roslyn50/InterfaceStubGenerator.Roslyn50.csproj" -v:minimal --no-restore /p:NuGetAudit=false /m:1
dotnet build "tests/Refit.GeneratedCode.TestModels/Refit.GeneratedCode.TestModels.csproj" -f net8.0 -v:minimal --no-restore /p:NuGetAudit=false /m:1
dotnet run --project "tests/Refit.GeneratorTests/Refit.GeneratorTests.csproj" -f net8.0 --no-restore --no-build
```

---

## Repository Conventions

- Shared target frameworks and package versions are centralized in `src/Directory.Build.props` and `src/Directory.Packages.props`.
- The repository uses `LangVersion=latest`; use modern C# features where they improve clarity or generated-code quality.
- AOT and trim analyzer behavior is enabled for compatible TFMs; prefer APIs that avoid reflection, dynamic code, and runtime type lookup.
- Keep changes scoped. Do not rewrite unrelated files while touching generator/runtime paths.
- Prefer focused tests that compile or execute the real generated output when changing source generator behavior.

---

## Public API tracking

The runtime projects use the public-API analyzer, so every public/protected member is recorded under `src/Refit/PublicAPI/<tfm>/`.

- New public surface goes in `PublicAPI.Unshipped.txt` for each affected TFM while you iterate.
- **Before opening a PR, move those new entries from `PublicAPI.Unshipped.txt` into the matching `PublicAPI.Shipped.txt` (and reset each unshipped file to just `#nullable enable`).** This repo ships almost immediately after merge, so unshipped API is promoted as part of the change rather than left pending.
- For behavior changes or public API additions, also add a breaking-changes note to the README.
- Shipped entries are assumed to be carried forward; you do not need to call them out separately in the PR.

