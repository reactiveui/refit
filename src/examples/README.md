# Refit examples

The website page examples target .NET 10 and .NET 11 and use C# 14. The Native AOT
documentation example targets .NET 11.

The `Documentation/Pages` projects contain source-backed examples for the Refit website pages.
Build every page project from `src/`:

```bash
dotnet build Refit.slnx -c Release
```

To run the typed JSON replies, use
`dotnet run --project examples/Documentation/Pages/testing-replies/testing-replies.csproj -f net11.0`.
Use `-f net10.0` to run the same page on .NET 10. See [the source layout](Documentation/README.md#source-layout) for independent serializer,
tooling, reflection, multipart and native projects. `Documentation/Bodies/Compression` targets
.NET 11 because its Zstandard and per-coding options require that runtime.

## Call a local HTTP service

From `src`, start the sample server. Its project profile listens on `http://localhost:5000`.

```shell
dotnet run --project examples/SampleUsingLocalApi/RestApiforTest -f net11.0 --launch-profile RestApiforTest
```

In a second terminal, start the console client from `src` and enter a number from 1 to 5 to send a request. To use another server, append `-- http://localhost:PORT` with its listening port.

```shell
dotnet run --project examples/SampleUsingLocalApi/ConsoleApplication -f net11.0
```

Both the client and the server also run with `-f net10.0`.

## Run the Meow client

Run `dotnet run --project examples/Meow -f net11.0` from `src` to check concurrent customer headers and read a large response from a local handler. Add `-- --live` to search The Cat API over the network.
