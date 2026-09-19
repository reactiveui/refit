# Compiler tooling samples

This executable targets .NET 10 and C# 14. It references the Refit source projects and Roslyn 5.0.0.
Refit's shipping compiler components target .NET Standard 2.0 and compile against Roslyn 4.8.
The host uses a newer Roslyn version to parse C# 14. It does not change those shipping components.

These samples run compiler tools, rather than send HTTP requests. A generator driver calls
`InterfaceStubGeneratorV2.Initialize`; an analyzer driver calls `RefitInterfaceAnalyzer.Initialize`.
The executable checks generated code, RF003 route analysis, and both RF003/RF005 corrections.
It also exercises identifier/type helpers and the public Index/Range polyfills in both shipping assemblies.

`CompatibilitySample` compiles source strings and checks the retained compatibility diagnostics:
`AttachmentName` reports CS0618, default generated `[FormObject]` multipart use reports RF006,
direct `JsonContentSerializer` construction reports CS0619, and `BodySerializationMethod.Json`
reports CS0618. These are expected findings inside a compiler probe; the runnable project itself
builds without suppressions or warnings.

`GeneratorTooling` and `AnalyzerTooling` are project-reference aliases. `extern alias` keeps their
public `System.Index` and `System.Range` copies separate from each other and the .NET runtime types.
Normal apps use the runtime types. The polyfill samples explain the compiled tooling surface.

The host requires the ordinary .NET runtime and compiler metadata files. It is not a Native AOT sample.
The parent documentation folder's `.editorconfig` governs this project. Complete source declarations
keep XML summaries and the inherited correctness, style, security, and performance rules.
Website blocks omit compiler-host glue; complete source contains the executable assertions.

Run from the repository `src/` directory:

```bash
dotnet build examples/Documentation/Tooling/Tooling.csproj -c Release -p:LangVersion=14.0
dotnet run --project examples/Documentation/Tooling/Tooling.csproj -c Release --no-build
```
