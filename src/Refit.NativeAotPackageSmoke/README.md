# Native AOT package consumer

This application consumes Refit from packages packed from this checkout, then runs as a Native AOT binary.
`Refit.NativeAotSmoke` covers the same runtime with project references. This project adds what only a packed
consumer shows:

- the three packages restore together at one version;
- the generator and analyzers arrive through the package's analyzer slot;
- the `buildTransitive` props apply, so `RefitRequireGeneratedRequests` turns any reflection fallback into a
  build error;
- every scenario runs after native publication, with trim and AOT warnings treated as errors.

The project stays out of `Refit.slnx`. It has its own `Directory.Build.props`, `Directory.Build.targets`,
`Directory.Packages.props` and `NuGet.config`, so no repository setting reaches it. Refit packages resolve only
from `obj/feed`.

## Scenarios

| Scenario | What the binary checks |
| --- | --- |
| Generated-only DI | `AddRefitGeneratedClient<T>` with a JSON context resolves and calls a client. |
| Keyed DI | `AddKeyedRefitGeneratedClient<T>` resolves a second client by key with its own base address. |
| Query converter | An `IQueryConverter<T>` writes the query string. |
| Multipart | A stream part and a JSON-serialized part upload with context metadata. |
| Error body | `ApiException.GetContentAsAsync<T>` reads a registered error model; `ApiResponse<T>` keeps the status. |
| Return adapter | A source-local `IReturnTypeAdapter<TReturn, TResult>` defers the call until it is invoked. |
| Streamed replies | One `IAsyncEnumerable<T>` method reads a JSON array, JSON Lines and server-sent events. |
| Early disposal and cancellation | Leaving a stream early, or cancelling a stalled read, closes the response. |
| JSON Lines upload | An `IAsyncEnumerable<T>` body streams to the handler. |
| Paging | A `PagedEnumerable<TPage, TItem>` method follows cursor tokens across pages. |
| Missing metadata | A reply type the context does not describe fails instead of using reflection. |

## Run it locally

Run from `src`. The pack commands write to this project's `obj/feed`.

```bash
for project in Refit Refit.HttpClientFactory Refit.Testing; do
  dotnet pack "$project/$project.csproj" -c Release -o Refit.NativeAotPackageSmoke/obj/feed
done

dotnet publish Refit.NativeAotPackageSmoke/Refit.NativeAotPackageSmoke.csproj -c Release -r linux-x64 -o Refit.NativeAotPackageSmoke/obj/publish
Refit.NativeAotPackageSmoke/obj/publish/Refit.NativeAotPackageSmoke
```

Use your platform's runtime identifier, such as `win-x64` or `osx-arm64`. The binary exits with a non-zero code
when a scenario fails. Each restore removes previously restored Refit packages first, so a repack is always used.

The `Native AOT` workflow packs once and runs this consumer on Windows, Linux and macOS.
