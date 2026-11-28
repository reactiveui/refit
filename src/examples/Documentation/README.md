# Website documentation examples

The website page projects run on .NET 10 and .NET 11 using C# 14. The native client
publishes for .NET 11.

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
dotnet run --project examples/Documentation/Pages/requests-routes/requests-routes.csproj -c Release -f net11.0 --no-build
```

Use `-f net10.0` to check the same page on .NET 10.

## Test an HTTP client

| Task | Runnable example |
| --- | --- |
| Match routes, queue or reuse replies, simulate faults, verify requests and stub API responses. | [Testing.cs](Testing/Testing.cs) |
| Return a model or a JSON object with runtime-defined keys using source-generated metadata. | [TypedJsonReply.cs](Testing/TypedJsonReply.cs) |
| Release response chunks, stall or fail a stream, inspect uploads and advance a test clock. | [TestingStreaming.cs](Testing/TestingStreaming.cs) |
| Exercise a retry policy with standard and provider-specific timing headers. | [ServerRetryHints.cs](Resilience/ServerRetryHints.cs) |

Run [testing-replies](Pages/testing-replies/testing-replies.csproj) for the reply examples and [clients-resilience](Pages/clients-resilience/clients-resilience.csproj) for retry behavior.

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
