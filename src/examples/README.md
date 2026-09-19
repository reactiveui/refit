# Refit examples

The examples use .NET 10 and C# 14 as their baseline. All C# 14 features are available.
An example that deliberately targets another framework
must name that framework and explain why it needs it.

The `Documentation/Pages` projects contain source-backed examples for the Refit website pages.
Build every page project from `src/`:

```bash
dotnet build Refit.slnx -c Release
```

Run a page project, such as `examples/Documentation/Pages/requests-routes/requests-routes.csproj`,
to check its local replies. See [the source layout](Documentation/README.md#source-layout) for independent serializer,
tooling, reflection, multipart and native projects. `Documentation/Bodies/Compression` targets
.NET 11 because its Zstandard and per-coding options require that runtime.

`Meow`, `Meow.Common` and the three `SampleUsingLocalApi` projects target .NET 8 to demonstrate
the library's lowest supported modern runtime. They use the newer C# compiler selected by
the repository SDK. The application's target runtime and the compiler's language version
are separate choices.
