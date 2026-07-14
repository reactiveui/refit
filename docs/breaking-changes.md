# Breaking changes and release notes

Every breaking change and notable addition, newest first. Each major version links back to the feature documentation
in the [main README](../README.md).

* [V14.x.x](#v14xx)
* [V13.x.x](#v13xx)
* [V12.x.x](#v12xx)
* [V11.x.x](#v11xx)
* [Updates in 8.0.x](#updates-in-80x)
* [V6.x.x](#v6xx)

## V14.x.x

### Breaking changes in V14.x

Refit 14 finishes the move to generated request building. Interfaces whose methods all generate inline no longer touch
the reflection request builder at any point, which is what makes them trim and Native AOT clean — and that shift is
visible in three places.

* **Some hot-path async members now return `ValueTask` instead of `Task`** (allocation reductions for the breaking
  release). `ApiResponse<T>.EnsureSuccessStatusCodeAsync()`/`EnsureSuccessfulAsync()` (and the matching
  `IApiResponse<T>` extension methods) and `DefaultApiExceptionFactory.CreateAsync` now return `ValueTask<...>`, making
  their common success path allocation-free. The `RefitSettings` async delegates `ExceptionFactory`,
  `DeserializationExceptionFactory`, and `AuthorizationHeaderValueGetter` now use `ValueTask`-returning `Func` shapes,
  removing a per-request `Task` allocation from the default (synchronously-completing) exception factory. `async`
  lambdas assigned to these delegates need no change; a delegate that returned `Task.FromResult(x)` should return
  `new ValueTask<T>(x)`, and a caller that stored an `EnsureSuccess...Async()` result as a `Task` should `await` it or
  call `.AsTask()`. `TransportExceptionFactory` is unaffected (it was already synchronous).
* **The reflection request builder is now an opt-in package.** The `Refit` package no longer carries it. If any of
  your interface methods cannot generate inline — the RF006 diagnostic reports exactly which — add a reference to the
  [`Refit.Reflection`](https://www.nuget.org/packages/Refit.Reflection) package and everything works as before, with
  no code changes. Without it, `RestService.For` and `AddRefitClient` throw a `NotSupportedException` naming the
  package when they reach a method that needs reflection. Applications whose interfaces all generate inline should not
  install it: that is what keeps a client trimmable and Native AOT clean.
* **Interface validation for fully generated interfaces happens when a request is built, not when the client is
  created.** `RestService.For<T>` still validates reflection-backed interfaces eagerly, but an interface whose methods
  all generate inline never touches the reflection request builder — an invalid route template (for example a
  placeholder with no matching parameter) now throws the same `ArgumentException` on the first call instead of inside
  `RestService.For`.
* **`ApiException` stack traces no longer contain the generated method frame** for methods that construct requests
  inline (they return the runner task directly instead of awaiting inside an async wrapper). Use
  `ApiException.HttpMethod`/`ApiException.Uri` to identify the failing request.
* **Query objects are flattened by their declared type, not their runtime type.** A complex parameter whose public
  properties become query pairs is now flattened at compile time, the same way the `System.Text.Json` source generator
  treats a declared type. The reflection request builder called `value.GetType()`, so passing a *derived* instance
  through a base-typed parameter also contributed the derived type's extra properties. Generated request building emits
  only the properties declared on the parameter's type:

  ```csharp
  public record BaseRecord(string Value);
  public sealed record DerivedRecord(string Name) : BaseRecord("value");

  [Get("/search")]
  Task<string> Search(BaseRecord query);

  Search(new DerivedRecord("ada"))
  >>> "/search?Value=value"                 // V14, generated: declared type only
  >>> "/search?Name=ada&Value=value"        // V13, reflection: runtime type
  ```

  Only polymorphic query objects are affected; a parameter whose declared type is the type you actually pass is
  unchanged. If you rely on the old behavior, declare the parameter as the derived type.

* **Flattened query-object keys honor the content serializer's property names.** A query object's property keys now
  resolve with the same precedence form-encoded field names already used: `[AliasAs]` first, then the configured
  content serializer's field name (`[JsonPropertyName]` for the default `System.Text.Json` serializer, `[JsonProperty]`
  for `Refit.Newtonsoft.Json`), then the URL parameter key formatter. Previously query keys consulted only `[AliasAs]`
  and the key formatter, so a `[JsonPropertyName]` on a flattened property had no effect on the query string.

  ```csharp
  public sealed class Filter
  {
      [JsonPropertyName("created_after")]
      public DateTimeOffset? CreatedAfter { get; set; }
  }

  [Get("/search")]
  Task<string> Search([Query] Filter filter);

  Search(new Filter { CreatedAfter = ... })
  >>> "/search?created_after=..."   // V14: serializer field name
  >>> "/search?CreatedAfter=..."    // V13: CLR name
  ```

  Both request builders apply this. The generated path bakes the `[JsonPropertyName]` name at compile time (matching
  the generated form path); the reflection path resolves it through the configured serializer, so it also picks up
  `[JsonProperty]` when `Refit.Newtonsoft.Json` is installed. To keep the pre-V14 behavior, set
  `RefitSettings.HonorContentSerializerPropertyNamesInQuery = false`; `[AliasAs]` still takes precedence in either mode.

* **URL-encoded bodies flatten nested objects, dictionaries, and collections instead of emitting the type name.** A
  `[Body(BodySerializationMethod.UrlEncoded)]` model whose property is a nested object, an `IDictionary`, or a plain
  collection under the default collection format previously serialized the property's `ToString()` — the type name for
  a complex object or dictionary. Those properties now flatten exactly like a flattened `[Query]` object: a nested
  object contributes `parent.child=...` pairs, a dictionary contributes `parent.key=value` pairs, and a collection
  joins its elements (comma-separated under the default format). Field naming keeps the existing precedence (`[AliasAs]`,
  then the content serializer's property name, then the key formatter), and a nested property's `[Query(delimiter,
  prefix)]` composes its keys.

  ```csharp
  public sealed class SignupForm
  {
      public string Name { get; set; }
      public Address Detail { get; set; }
      public Dictionary<string, string> Extra { get; set; }
  }

  Submit(new SignupForm { Name = "ada", Detail = new() { City = "Wien" }, Extra = new() { ["k"] = "v" } })
  >>> "Name=ada&Detail.City=Wien&Extra.k=v"                       // V14: flattened
  >>> "Name=ada&Detail=RefitGeneratorTest.Address&Extra=System..." // pre-V14: ToString
  ```

  This is a behavior change to previously unusable output, so it is extremely unlikely anyone depended on it. Complex
  elements of a collection are still `ToString()`-ed per element (the same limitation as the query path).
* **An empty token from `AuthorizationHeaderValueGetter` now omits the `Authorization` header instead of sending a
  blank one** (#1688). Previously a getter that returned `null`, an empty string, or whitespace produced a blank
  `Authorization: <scheme>` header (and could throw for some scheme/value combinations). Refit now clears the header for
  that request, so a single client can mix authenticated and anonymous calls by returning a token or an empty string. If
  you relied on a blank header being sent, return a non-empty value.

### New in V14.x

* **Inline query-string generation.** Query parameters — auto-appended parameters, `[AliasAs]`, `[Query(Format = ...)]`,
  and scalar collections with every `CollectionFormat` — now generate reflection-free request construction, so the most
  common Refit method shapes work with generated-only clients (`AddRefitGeneratedClient`, `RestService.ForGenerated`)
  and under Native AOT. Implicit body detection (a complex parameter on POST/PUT/PATCH without `[Body]`) generates
  inline too, and value formatting for statically-known types (numbers, dates, GUIDs, enums with `[EnumMember]`) is
  resolved at compile time while still honoring a custom `IUrlParameterFormatter` when one is configured.
* **Reflection-free query-object flattening.** A complex query parameter's public readable properties are now walked at
  compile time and emitted as straight-line code — no reflection, no delegates, and no per-request descriptor array —
  honoring `[AliasAs]`, the content serializer's property name (`[JsonPropertyName]`), `[Query]`
  prefix/delimiter/format/`SerializeNull`, and the `IgnoreDataMember`/`JsonIgnore` attributes. Value-typed properties
  are formatted without boxing. A property that is itself a collection of simple elements (for example `int[]`,
  `List<TEnum>`, `IReadOnlyList<string>`) flattens too, honoring its `CollectionFormat`; a customized
  `IUrlParameterFormatter` still gets the reflection builder's two formatting passes. A property that is itself a
  concrete nested object flattens recursively under a dotted key (`Address.City=...`), composing each level through the
  key formatter and honoring per-level `[Query(Prefix)]`; a self-referential (cyclic) type keeps using the reflection
  builder. See the declared-type and property-name notes above.
* **Reflection-free dictionary query parameters.** `IDictionary<TKey, TValue>` and `Dictionary<TKey, TValue>` with
  simple keys and values expand inline, one query pair per entry, with the same semantics as before: entries with a
  null value are omitted, blank keys drop the pair, enum keys render through `[EnumMember]`, and a parameter-level
  `[Query(Prefix)]` is prepended to every key. Dictionaries with `object` values still use the reflection request
  builder, because the value's runtime type decides whether it is recursed into — unless you attach an
  `[QueryConverter]` (below).
* **`[QueryConverter]` for shapes only known at runtime.** Attach `[QueryConverter(typeof(MyConverter))]` to a query
  parameter whose shape the generator cannot flatten from the declared type — an `object` value, a polymorphic base
  type, a `Dictionary<string, object>`. The converter (`IQueryConverter<T>`) writes query pairs directly into the
  pooled `GeneratedQueryStringBuilder`, so the parameter generates inline and stays reflection- and allocation-free.
  It is a source-generation-only feature (like `[QueryName]`/`[Encoded]`): a method that carries it but cannot generate
  inline for another reason reports `RF007`. The reflection request builder is unaffected; it keeps walking the runtime
  type. `Refit` ships `SystemTextJsonQueryConverter<T>`, which flattens any type registered in your configured
  `System.Text.Json` context by walking its `JsonTypeInfo`.
* **`[QueryName]` valueless query flags.** The equivalent of Retrofit's `@QueryName`: the parameter's value becomes a
  bare `?flag` segment with no `=value`; collections render one flag per element. See
  [Valueless query flags](../README.md#valueless-query-flags).
* **`[Encoded]` per-parameter encoding opt-out.** The equivalent of Retrofit's `encoded = true`: a query or path value
  (including round-tripping `{**param}` segments) passes through verbatim so pre-encoded values are not double-encoded.
  See [Unescape Querystring parameters](../README.md#unescape-querystring-parameters).
* **RF006 fallback analyzer.** Methods that must use the reflection request builder are reported at compile time, so
  generated-only and Native AOT clients fail the build instead of throwing at runtime. RF007 reports the use of
  source-generation-only attributes (`[QueryName]`, `[Encoded]`) on methods that cannot generate inline.
* **Generated fallback methods no longer leak IL2026/IL3050.** Projects with `EnableTrimAnalyzer` and warnings as
  errors build cleanly; the compile-time RF006 diagnostic is the signal that a method still needs reflection.
* **Hot-path allocation reductions.** The response pipeline avoids a per-stream linked `CancellationTokenSource` when a
  token can't cancel, uses `HttpHeaders.NonValidated` for generated header checks (no value re-parsing), and returns
  `ValueTask` from the internal deserialization chain so empty/`204` responses complete without a `Task` allocation —
  alongside the public `ValueTask` moves noted in the breaking changes above.
* **Generic interface methods generate inline.** A generic Refit method (for example `Task<T> Get<T>(string id)` or
  `Task<TResponse> Post<TRequest, TResponse>([Body] TRequest body)`) no longer falls back to the reflection request
  builder — the type parameter flows straight through to the generated runner (`SendAsync<T, TBody>`), so generic
  methods are reflection-free and Native AOT clean, with their generic constraints preserved. The narrow exceptions that
  still require the reflection builder are the ones an *open* type parameter genuinely can't generate: a complex query
  object (its query pairs are only known per value) and a form-url-encoded `[Body]` (its `[DynamicallyAccessedMembers]`
  contract can't be satisfied by an open type parameter). RF006 continues to report exactly which methods still fall
  back.
* **Pluggable return-type adapters (`IReturnTypeAdapter<TReturn, TResult>`).** Surface any custom return type (for
  example `IObservable<T>` or a `Result<T>` wrapper) from an interface method by implementing the interface. The source
  generator discovers adapters declared in your project at compile time and emits a direct `Adapt` call — no reflection,
  so adapter-backed methods stay trim and Native AOT clean; the reflection request builder resolves adapters registered
  in `RefitSettings.ReturnTypeAdapters`. See [Custom return types](../README.md#custom-return-types-ireturntypeadapter).
* **Scoped (per-request) authorization tokens via DI (`AddAuthorizationHeaderValueProvider`).** A new
  `IHttpClientBuilder` extension in `Refit.HttpClientFactory` resolves the `Authorization` token from dependency
  injection per request. Because `IHttpClientFactory` pools message handlers, it creates a fresh DI scope for every
  request and resolves your delegate `(IServiceProvider, HttpRequestMessage, CancellationToken) -> ValueTask<string>`
  from that scope, disposing it when the request completes — no `Microsoft.AspNetCore.*` dependency required (#1679). See
  [Scoped (per-request) authorization tokens with dependency injection](../README.md#scoped-per-request-authorization-tokens-with-dependency-injection).

## V13.x.x

### Breaking changes in V13.x

Refit 13 hardens the default security posture from a security audit. The new behavior is safe by default; the changes
below only affect code that previously relied on the less-secure defaults.

* **XML responses no longer process DTDs.** `XmlContentSerializer` now forces `DtdProcessing.Prohibit` and clears the
  `XmlResolver` on every read, blocking XML External Entity (XXE) and entity-expansion ("billion laughs") attacks. If
  you were deserializing trusted XML that depends on a DTD or external entities, that content will now throw. We
  **strongly recommend against re-enabling DTD processing**, but if your XML comes from a fully trusted source you can
  opt out via the obsolete `XmlReaderWriterSettings.AllowDtdProcessing` flag (marked `[Obsolete]` deliberately, so it
  surfaces a compiler warning) - you must also configure `DtdProcessing`/`XmlResolver` yourself on `ReaderSettings`.
  Prefer pre-processing untrusted documents instead.
* **Newtonsoft.Json no longer inherits an unsafe global `TypeNameHandling`.** When you do not pass explicit
  `JsonSerializerSettings`, `NewtonsoftJsonContentSerializer` now forces `TypeNameHandling.None` even if
  `JsonConvert.DefaultSettings` configured a different value, closing a known remote-code-execution gadget vector on
  response bodies. If you genuinely need polymorphic (`$type`) deserialization, opt in explicitly by passing your own
  `JsonSerializerSettings` (ideally constrained with a `SerializationBinder`).

The release also adds two opt-in, non-breaking knobs on `RefitSettings` for hardening exception handling:

* `RefitSettings.ExceptionRedactor` — a hook invoked before an `ApiException` propagates, so you can scrub the
  `Authorization` header, request/response bodies, and `Set-Cookie` before they reach logging or telemetry pipelines
  that serialize exceptions.
* `RefitSettings.MaxExceptionContentLength` — caps how many characters of an error response body are read into
  `ApiException.Content`, bounding memory use against hostile or oversized error responses. Defaults to unbounded.

### New in V13.x

* **`Refit.Testing`** — a new first-party package for testing Refit clients without a mocking library or a live
  server. You describe expected calls as a route table (`Route` → `Reply`) and point a real client at it via
  `StubHttp.CreateClient<T>(...)`. Route templates mirror your interface attributes, typed replies (`Reply.With<T>`)
  are serialized with the client's own serializer, and the sent request body can be read back as a typed object with
  `LastRequestBodyAsync<T>()`. It also includes `NetworkBehavior` for seeded latency/fault injection and
  `StubApiResponse<T>` for unit-testing code that consumes `IApiResponse<T>`. See
  [Testing your Refit clients](../README.md#testing-your-refit-clients).
## V12.x.x

### Breaking changes in V12.x

Refit 12.0 is a large release centered on a near-complete rewrite of request building. The source generator now builds
eligible HTTP requests inline at compile time instead of going through the reflection request-builder pipeline, with the
reflection path kept as a fallback for shapes that cannot be generated inline.

Two breaking changes are called out for migration:

* `IApiResponse<T>` no longer shadows base interface members. The `new`-shadowed `Error`, `ContentHeaders`,
  `IsSuccessStatusCode`, and `IsSuccessful` members were removed from the generic interface. Source that reads these
  members still binds to the inherited base members, but assemblies compiled against v8-v11 should be recompiled.
  If you used `IsSuccessful` to narrow `Content` to non-null on an `IApiResponse<T>` value, use `HasContent` or
  `IsSuccessfulWithContent` instead.

  Because the shadow is gone, `Error` is now typed as `ApiExceptionBase?`, which exposes only request-side context
  (`RequestContent`, `HttpMethod`, `Uri`, `RequestMessage`). The response body lives on the derived `ApiException`, so
  `response.Error.Content` no longer compiles. The feature was not removed; the body simply moved to the derived type.
  Migrate like this:

  ```csharp
  // Before (v8-v11)
  var content = response.Error.Content;

  // After (v12+) - null-safe typed access to the response body
  if (response.HasResponseError(out var apiException))
      logger.LogError(apiException, apiException.Content);

  // Or, as a shorthand cast when you just want the body
  var content = (response.Error as ApiException)?.Content;
  ```
* The default `System.Text.Json` serializer now reads numbers from JSON strings by setting
  `JsonNumberHandling.AllowReadingFromString`. To opt back out, set `NumberHandling = JsonNumberHandling.Strict` on
  your `JsonSerializerOptions`.

See the [Refit 12.0.0 release notes](https://github.com/reactiveui/refit/releases/tag/v12.0.0) for the full release
details.

## V11.x.x

### Breaking changes in 11.x

Refit 11 introduces `ApiRequestException` to represent requests that fail before receiving a response from the server.
This exception will now wrap previous exceptions such as `HttpRequestException` and `TaskCanceledException` when they
occur during request execution.

* If you were not wrapping responses with `IApiResponse` and were catching these exceptions directly, you will need to
  update your code to catch `ApiRequestException` instead.
* If you were wrapping responses with `IApiResponse`, these exceptions will no longer be thrown and will instead be
  captured in the `IApiResponse.Error` property.
  You can use the new `IApiResponse.HasRequestError(out var apiRequestException)` method to safely check and retrieve
  the `ApiRequestException` instance.

The `IApiResponse.Error` property's type has also changed to `ApiExceptionBase`, which is the new base class for
`ApiException` and `ApiRequestException`.
If your code accessed members specific to `ApiException` (i.e. anything related to the response from the server), you
can use the new `IApiResponse.HasResponseError(out var apiException)` method to safely check and retrieve the
`ApiException` instance.

All response-related properties of `IApiResponse` are now nullable.
The new `IApiResponse.IsReceived` property can be used to check if a response was received from the server, and will
mark those properties as non-null.
The original `IApiResponse.IsSuccessful` and `IApiResponse.IsSuccessStatusCode` properties can still be used to check if
the response was received and is successful.

## Updates in 8.0.x

Fixes for some issues experienced, this led to some breaking changes.
See [Releases](https://github.com/reactiveui/refit/releases) for full details.

## V6.x.x

Refit 6 requires Visual Studio 16.8 or higher, or the .NET SDK 5.0.100 or higher. It can target any .NET Standard 2.0
platform.

Refit 6 does not support the old `packages.config` format for NuGet references (as they do not support analyzers/source
generators). You must
[migrate to PackageReference](https://devblogs.microsoft.com/nuget/migrate-packages-config-to-package-reference/) to use
Refit v6 and later.

### Breaking changes in 6.x

Refit 6
makes [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview) the
default JSON serializer. If you'd like to continue to use `Newtonsoft.Json`, add the `Refit.Newtonsoft.Json` NuGet
package and set your `ContentSerializer` to `NewtonsoftJsonContentSerializer` on your `RefitSettings` instance.
`System.Text.Json` is faster and uses less memory, though not all features are supported.
The [migration guide](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-5-0)
contains more details.

`IContentSerializer` was renamed to `IHttpContentSerializer` to better reflect its purpose. Additionally, two of its
methods were renamed, `SerializeAsync<T>` -> `ToHttpContent<T>` and `DeserializeAsync<T>` -> `FromHttpContentAsync<T>`.
Any existing implementations of these will need to be updated, though the changes should be minor.

##### Updates in 6.3

Refit 6.3 splits out the XML serialization via `XmlContentSerializer` into a separate package, `Refit.Xml`. This
is to reduce the dependency size when using Refit with Web Assembly (WASM) applications. If you require XML, add a
reference
to `Refit.Xml`.

