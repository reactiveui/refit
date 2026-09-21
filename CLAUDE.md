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

dotnet build "InterfaceStubGenerator.Roslyn48/InterfaceStubGenerator.Roslyn48.csproj" -v:minimal --no-restore /p:NuGetAudit=false /m:1
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
- Generator snapshots live in `src/tests/Refit.GeneratorTests/_snapshots`. Run the affected test with
  `ACCEPT_SNAPSHOTS=1` to intentionally accept changed generated output, then rerun it without that
  variable to verify the snapshots.

### Generator performance

Settle generator perf with benchmarks and EventPipe traces, never by inspection or a hunch that "it's already optimal" — build overall (whole-generator) and micro (per-component) benchmarks and let the traces show where the real cost is before optimizing. Context, not a verdict that nothing can improve: the generator already uses `ForAttributeWithMetadataName`, value-equatable `readonly record struct` models + `ImmutableEquatableArray` (incremental caching depends on this), `PooledStringBuilder`, and incremental-cache regression tests. The generator builds against a single Roslyn version (4.8) and ships in a single `analyzers/dotnet/roslyn4.8/cs` slot; do not reintroduce a second slot to pick up newer Roslyn APIs without also handling legacy non-SDK consumers, which receive every slot at once (see `AnalyzerPackagingTests`).

Generator / incremental-pipeline benchmarks go in their own BenchmarkDotNet project (`src/benchmarks/Refit.Generator.Benchmarks`), separate from the runtime `Refit.Benchmarks`, built on the existing BenchmarkDotNet setup — never a bespoke driver. Profile with BenchmarkDotNet's native `[EventPipeProfiler(EventPipeProfile.GcVerbose|CpuSampling)]` diagnoser — EventPipe is the right lens for a Roslyn generator, whereas `[MemoryDiagnoser]` is a weak signal for whole-generator runs. Widening a generator member `private` -> `internal` (with `InternalsVisibleTo`) to micro-benchmark it is fine. Name benchmark classes for the component under test (parser, emitter, query/path building), not the measurement type.

Useful validation:

```bash
cd src

dotnet build "InterfaceStubGenerator.Roslyn48/InterfaceStubGenerator.Roslyn48.csproj" -v:minimal --no-restore /p:NuGetAudit=false /m:1
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

The runtime project uses PublicApiSharp. Every public or protected member is recorded in one `src/Refit/PublicAPI/<tfm>/PublicAPI.txt` per target framework. There is no Shipped/Unshipped split and no promotion step.

- A change to the public surface fails the build with `PAS0001`, `PAS0002` or `PAS0003` until the baselines are regenerated.
- Regenerate from `src` with `dotnet format analyzers Refit/Refit.csproj --diagnostics PAS0001 PAS0002 PAS0003 --severity info --no-restore`. Each run rewrites one target framework, so run it once per framework (ten runs), then build `Refit/Refit.csproj` to confirm.
- Read the baseline diff before opening the PR. Added lines are a minor release. A removed or changed line is a breaking change.
- `docs/breaking-changes.md` records the breaking changes of shipped versions, grouped by major version. Describe a new change in the pull request body, not in that file.

---

## JSON context registration

A source-generated `JsonSerializerContext` is registered at runtime; the generator never emits `[JsonSerializable]` because one JSON source generator cannot read another's output. Every member below is `#if NET8_0_OR_GREATER`.

- `RestService.ForGenerated<T>` and `HttpClientFactoryExtensions.AddRefitGeneratedClient<T>` / `AddKeyedRefitGeneratedClient<T>` have overloads that take a context, with `RefitSettings` and `bool allowReflectionFallback` variants. `RefitSettings.ForJsonContext` / `UseJsonContext` and `SystemTextJsonContentSerializer.ForContext` / `WithContext` back them, and serve the reflection-based APIs too.
- With no settings the context's own `Options` are used unchanged. With settings, the settings' serializer options are copied and kept (naming, converters, existing resolvers first, `DefaultJsonTypeInfoResolver` modifiers carried onto the context) and the context is appended as a metadata source. The context's `[JsonSourceGenerationOptions]` naming is not applied in that mode.
- Reflection-based JSON is off unless `allowReflectionFallback` is `true`; a type the context does not describe throws `NotSupportedException`. A context declared without `[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]` reads camelCase replies as default values, so every example context carries that attribute.
- `IJsonTypeInfoContentSerializer` is the optional capability for explicit `JsonTypeInfo<T>` overloads; `SystemTextJsonContentSerializer` implements it.
- A Refit method may declare `JsonTypeInfo<T>` parameters. `Parser.IsJsonTypeInfo` classifies them as `RequestParameterKind.JsonTypeInfo` (`RequestParameterModel.JsonTypeInfoTarget` holds `T`); each one is matched by `T` to the JSON body type or to the type the reply is read as. A body match makes the emitter call `GeneratedRequestRunner.CreateBodyContent(settings, body, typeInfo, ...)`; a reply match emits `SetRequestJsonTypeInfo<T>`, which stores the metadata in the request options for `RequestExecutionHelpers.DeserializeSerializedContentAsync` and `ReadStream`. Anything else reports `RF014` (`Parser.ValidateJsonTypeInfoParameters`), including a method that cannot be generated inline or generated request building being off, because the reflection request builder would treat the parameter as a body or query value.
- `GeneratedRequestRunner.JsonTypeInfo.cs` is `NET8_0_OR_GREATER` only; `JsonTypeInfoParameterGenerationTests` and `JsonTypeInfoParameterSnapshotTests` cover the generator, `GeneratedJsonTypeInfoParameterTests` runs generated clients, and `Refit.GeneratedCode.TestModels/Scenarios/IGeneratedJsonMetadataApi.cs` compiles the output under the repo analyzers.
- `RestService`, `RefitSettings` and `SystemTextJsonContentSerializer` are `partial` only on `NET8_0_OR_GREATER` (a `partial` with one declaration raises `SST1419` on the .NET Framework targets). The two `RestService.ForGenerated<T>(…, RefitSettings)` overloads carry `[OverloadResolutionPriority(1)]` so `ForGenerated<T>(client, new())` still binds to the settings overload; `Refit.csproj` compiles the polyfill on `net8.0` for it.
- `Refit.NativeAotSmoke` publishes with `ForGenerated(client, context)` and asserts an undescribed reply type fails; run it after touching this area: `dotnet publish Refit.NativeAotSmoke/Refit.NativeAotSmoke.csproj -c Release -r linux-x64` from `src`, then execute the binary under `bin/Release/net8.0/linux-x64/publish/`.

---

## Pagination

`PagedEnumerable<TPage, TItem>` wraps an ordinary Refit method that returns one page and exposes a lazy sequence of items or pages. The page type is the API's own DTO or an `ApiResponse<TPage>` of it; Refit adds no page model.

**Runtime (`src/Refit`)**

- `PagedEnumerable` holds the factories `Create`, `FromCursor`, `FromOffset` and `FromLinks`. `PagedEnumerable{TPage,TItem}` implements `IAsyncEnumerable<TItem>` and adds `AsPages`, `WithPrefetch`, `WithMaxPages`, `ToObservable` and `ToPageObservable`.
- `PagePump` fetches one page per call, with one page of read-ahead and a linked cancellation token. A fetch or continuation error is captured and rethrown when the consumer reaches it. Each page is disposed when the consumer moves on.
- `PageContinuation` says whether another page follows. A `null` token, or an empty string, ends the sequence.
- `NextLinkOriginPolicy` decides which links may be followed. A refused link is never requested, and a link with user information is always refused.
- `LinkHeaderParser` and `IApiResponse.GetLink` read RFC 8288 `Link` headers.
- `PagedAttribute`, `PageTokenAttribute` and `GeneratedPaging` (helpers for generated code, hidden from IntelliSense) are the generator-facing surface.

**Generator (`src/InterfaceStubGenerator.Shared`)**

- A method returning `Refit.PagedEnumerable<TPage,TItem>` is `ReturnTypeInfo.Paged`. `Parser.Paging.cs` reads `[Paged]` and `[PageToken]`, resolves the dotted member paths against the page type at compile time, and produces a `PagingModel` on `RequestModel.Paging`. A misconfigured method reports `RF013`. A valid method whose request cannot be generated inline reports `RF007`.
- `Emitter.Inline.Paged.cs` emits two members per method: a sibling `BuildRefit<Method>PageRequest` that returns the `HttpRequestMessage` from the standard inline request construction (`ReturnTypeInfo.PageRequest`), and the paged method, which passes lambdas to `PagedEnumerable.Create` or `FromLinks`. Every page calls the sibling again, so a request is never sent twice.
- Generated code stays valid at the C# 7.3 baseline: no patterns, and `static` lambdas only when `SupportsStaticLambdas`.
- `Refit.Analyzers.Roslyn48.csproj` compiles an explicit list of shared parser and model files. A new parser or model file used by `Parser.Request.cs` is added to that list.

**Rules**

- A link-following method states exactly one of `Origins`, `SameOrigin` or `AnyOrigin`. The generator rejects a method that states none.
- A paged method is not generic and declares no `CancellationToken`; the enumeration supplies the token.
- The reflection request builder does not support `PagedEnumerable` returns.

**Examples, docs and tests**

- `src/examples/Documentation/Paging` holds mocked S3, Azure Blob Storage, Cosmos DB, Microsoft Graph, GitHub, Google Cloud Storage, Jira and DynamoDB services on `StubHttp`. The page project is `Pages/results-paging`.
- The website page is `docs/documentation/refit/results/pagination.md` in the website repository.
- `Refit.Tests` covers the runtime and generated clients (`GeneratedPagedMethodTests*`, `PagedEnumerableTests*`, `LinkHeaderParserTests`, `GeneratedPagingTests`). `Refit.GeneratorTests` covers emission and diagnostics (`PagedReturnGenerationTests`, `PagedReturnSnapshotTests`).
