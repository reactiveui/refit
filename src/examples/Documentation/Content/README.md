# Content and return adapters

From `src/` run `dotnet run --project examples/Documentation/Content/Content.csproj -c Release -f net11.0`.
This standalone example writes JSON Lines, reads array/JSON Lines/SSE streams,
uses inferred-object conversion with generated metadata, and executes a generated deferred return adapter.
All HTTP replies use a local handler. Input streams passed directly to the serializer remain caller-owned.

For native verification, use `dotnet publish examples/Documentation/Content/Content.csproj -c Release -f net11.0 -r linux-x64`.
The obsolete `JsonContentSerializer` is intentionally absent: its attribute rejects compilation and all methods throw.
