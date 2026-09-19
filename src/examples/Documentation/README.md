# Website documentation examples

These examples use .NET 10 and C# 14. All C# 14 features are available.
An example for another target framework must state its target and reason.

The C# blocks on the Refit website are excerpts from these source files.
Website excerpts can omit setup and runtime checks. These complete projects retain them.
Build and run these examples against local Refit source when checking a page.

The local `.editorconfig` keeps compiler, correctness, security, performance, XML documentation
and other style rules inherited and enabled. These complete projects satisfy those rules.

Local variables use explicit types so a website reader can see the type without navigating to another
method. SST2271 stays enabled with its `never` preference for `var`; the IDE preferences agree.
This preference applies only to this documentation folder.

Run from `src/`:

```bash
dotnet build Refit.slnx -c Release
dotnet run --project examples/Documentation/Pages/requests-routes/requests-routes.csproj -c Release --no-build
```

The language-version argument applies C# 14 to local source dependencies too, even if a newer installed
SDK selects C# 15 for the repository's `latest` setting.

## Source layout

Each website page has a project under `Pages/`. A page project links the existing subject source
files and runs its local HTTP assertions. Website pages show relevant portions of those complete
examples and link the corresponding project on GitHub.

Independent projects separate subjects with different dependencies or execution requirements:

| Project | Purpose and execution |
| --- | --- |
| `Aot/NativeClient` | Native AOT runner for compatible main subjects, using generated request and JSON metadata. |
| `Clients/Reflection` | Runtime request builders and reflection client creation; JIT execution. |
| `Serialization/Other` | Newtonsoft.Json and XML serializers; JIT execution. |
| `Tooling` | Generator, analyzer, code-fix and polyfill contracts, including expected compile diagnostics; JIT execution. |
| `RuntimeHelpers` | Infrastructure helpers and reflected method metadata; JIT execution. |
| `Content` | Custom content serializers and return adapters; generated clients support native execution. |
| `Multipart` | Generated file and stream uploads, including native execution. |
| `Multipart/Legacy` | Reflection-mode FormObject support; JIT execution. |
| `Bodies/Compression` | .NET 11 compressor options and Zstandard. This target supplies APIs absent from .NET 10. |

All page and independent projects are included in `Refit.slnx`. Build the solution from `src/` with
`dotnet build Refit.slnx -c Release`, then use each project's README for its run and publish commands.
Native publishing requires the platform's compiler and linker. An ordinary build does not
verify native execution.
