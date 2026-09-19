# Multipart documentation examples

Run from the Refit repository's `src` directory:

```sh
dotnet build examples/Documentation/Multipart/Multipart.csproj -c Release -p:LangVersion=14.0
dotnet run --project examples/Documentation/Multipart/Multipart.csproj -c Release --no-build
dotnet publish examples/Documentation/Multipart/Multipart.csproj -c Release -r linux-x64 -p:LangVersion=14.0
examples/Documentation/Multipart/bin/Release/net10.0/linux-x64/publish/Multipart
```

The main project uses only generated request construction, generated JSON metadata and a local handler.
Its checks cover part metadata, custom `MultipartItem` constructors and `CreateContent`, repeated attachments,
file-stream disposal and preservation of caller-owned streams. It makes no network calls.

`Legacy/Legacy.csproj` is a separate JIT-only executable for `[FormObject]`:

```sh
dotnet build examples/Documentation/Multipart/Legacy/Legacy.csproj -c Release -p:LangVersion=14.0
dotnet run --project examples/Documentation/Multipart/Legacy/Legacy.csproj -c Release --no-build
```

It references `Refit.Reflection`, calls `RestService.For` and explicitly selects
`RefitGeneratedRequestBuilding=false`. Every method is intended to use runtime reflection,
so generated request construction is disabled as a runtime mode rather than a diagnostic suppression.
Compiler, analyzer and XML documentation severities are inherited unchanged. This project is not published with native AOT.
The separate Tooling probes assert `RF006` for default generated-mode `[FormObject]` and `CS0618` for obsolete attachment naming.
