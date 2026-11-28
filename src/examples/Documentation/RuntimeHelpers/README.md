# Runtime helper samples

Run from `refit/src`:

```sh
dotnet build examples/Documentation/RuntimeHelpers/RuntimeHelpers.csproj -c Release -p:LangVersion=14.0
dotnet run --project examples/Documentation/RuntimeHelpers/RuntimeHelpers.csproj -c Release -f net11.0 --no-build
```

This executable targets .NET 10 and .NET 11 and references the local Refit project.
It checks path/query escaping, generated attribute providers, form descriptors,
body serialization/compression, and task/observable/stream dispatch using a local
HTTP handler. It also calls a generated interface, the usual application entry point.

`HelperJsonContext` provides generated JSON metadata. `FormField<FormBody>` uses
direct static getters. Those examples illustrate the generator's approach to
avoiding type discovery. The no-format generic path overload is deliberately used
only for an integer, as the generator requires.

`MetadataSample` deliberately uses `MethodInfo` and `PropertyInfo`. The project
includes that reflected example and therefore does not claim Native AOT support.
No Native AOT publish or platform-framework matrix is covered by this executable.
The .NET 10 run checks that a Zstandard request raises `PlatformNotSupportedException`. The .NET 11 run decodes the compressed body and checks its `zstd` header.

The sample verifies both overload results and ownership/encoding contracts with
assertions. It records the current rooted-file-path limitation of absolute URL
validation; the implementation is unchanged.
