![Refit](images/logo.png)

## Refit: The automatic type-safe REST library for modern .NET

[![Build](https://github.com/reactiveui/refit/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/refit/actions/workflows/ci-build.yml) [![codecov](https://codecov.io/github/reactiveui/refit/branch/main/graph/badge.svg?token=2guEgHsDU2)](https://codecov.io/github/reactiveui/refit)

|         | Refit                                                                                       | Refit.HttpClientFactory                                                                                                         | Refit.Newtonsoft.Json                                                                                                       | Refit.Testing                                                                                                    |
|---------|---------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| *NuGet* | [![NuGet](https://img.shields.io/nuget/v/Refit.svg)](https://www.nuget.org/packages/Refit/) | [![NuGet](https://img.shields.io/nuget/v/Refit.HttpClientFactory.svg)](https://www.nuget.org/packages/Refit.HttpClientFactory/) | [![NuGet](https://img.shields.io/nuget/v/Refit.Newtonsoft.Json.svg)](https://www.nuget.org/packages/Refit.Newtonsoft.Json/) | [![NuGet](https://img.shields.io/nuget/v/Refit.Testing.svg)](https://www.nuget.org/packages/Refit.Testing/) |

Refit is a library heavily inspired by Square's
[Retrofit](http://square.github.io/retrofit) library, and it turns your REST
API into a live interface:

```csharp
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
}
```

The `RestService` class generates an implementation of `IGitHubApi` that uses
`HttpClient` to make its calls:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com");
var octocat = await gitHubApi.GetUser("octocat");
```

.NET supports registering Refit clients via HttpClientFactory:

```csharp
services
    .AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));
```

To test the clients you build with Refit, the [`Refit.Testing`](https://www.nuget.org/packages/Refit.Testing) package
lets you stub responses and verify requests with a declarative route table — see
[Testing your Refit clients](#testing-your-refit-clients).

# Table of Contents

* [Sponsors](#sponsors)
* [Where does this work?](#where-does-this-work)
    * [Breaking changes in V13.x](#breaking-changes-in-v13x)
    * [Breaking changes in V12.x](#breaking-changes-in-v12x)
    * [Breaking changes in 11.x](#breaking-changes-in-11x)
    * [Breaking changes in 6.x](#breaking-changes-in-6x)
* [Source generation](#source-generation)
    * [Generated-only client creation](#generated-only-client-creation)
    * [Generated request building](#generated-request-building)
* [API Attributes](#api-attributes)
* [Querystrings](#querystrings)
    * [Dynamic Querystring Parameters](#dynamic-querystring-parameters)
    * [Collections as Querystring parameters](#collections-as-querystring-parameters)
    * [Unescape Querystring parameters](#unescape-querystring-parameters)
    * [Custom Querystring Parameter formatting](#custom-querystring-parameter-formatting)
* [Body content](#body-content)
    * [Buffering and the Content-Length header](#buffering-and-the-content-length-header)
    * [JSON content](#json-content)
    * [JSON Lines content](#json-lines-content)
    * [XML Content](#xml-content)
    * [Form posts](#form-posts)
* [Setting request headers](#setting-request-headers)
    * [Static headers](#static-headers)
    * [Dynamic headers](#dynamic-headers)
    * [Bearer Authentication](#bearer-authentication)
    * [Reducing header boilerplate with DelegatingHandlers (Authorization headers worked example)](#reducing-header-boilerplate-with-delegatinghandlers-authorization-headers-worked-example)
    * [Redefining headers](#redefining-headers)
    * [Removing headers](#removing-headers)
* [Passing state into DelegatingHandlers](#passing-state-into-delegatinghandlers)
    * [Support for Polly and Polly.Context](#support-for-polly-and-pollycontext)
    * [Target Interface type](#target-interface-type)
    * [MethodInfo of the method on the Refit client interface that was invoked](#methodinfo-of-the-method-on-the-refit-client-interface-that-was-invoked)
* [Multipart uploads](#multipart-uploads)
* [Retrieving the response](#retrieving-the-response)
* [Using generic interfaces](#using-generic-interfaces)
* [Interface inheritance](#interface-inheritance)
    * [Headers inheritance](#headers-inheritance)
* [Default Interface Methods](#default-interface-methods)
* [Using HttpClientFactory](#using-httpclientfactory)
* [Providing a custom HttpClient](#providing-a-custom-httpclient)
* [Handling exceptions](#handling-exceptions)
    * [When returning Task&lt;IApiResponse&gt;, Task&lt;IApiResponse&lt;T&gt;&gt;, or Task&lt;ApiResponse&lt;T&gt;&gt;](#when-returning-taskiapiresponse-taskiapiresponset-or-taskapiresponset)
    * [When returning Task&lt;T&gt;](#when-returning-taskt)
    * [Inspecting the error body synchronously](#inspecting-the-error-body-synchronously)
    * [Reading the request body that was sent](#reading-the-request-body-that-was-sent)
    * [Providing a custom ExceptionFactory](#providing-a-custom-exceptionfactory)
    * [Providing a custom TransportExceptionFactory](#providing-a-custom-transportexceptionfactory)
    * [ApiException deconstruction with Serilog](#apiexception-deconstruction-with-serilog)
* [Testing your Refit clients](#testing-your-refit-clients)

### Sponsors

Refit is sponsored by the following:

[![lombiq logo](images/lombiq.svg)](https://lombiq.com)[![jetbrains logo](images/jetbrains.svg)](https://www.jetbrains.com)[![claude logo](images/claude.svg)](https://claude.com)

### Where does this work?

Refit currently supports the following platforms and modern .NET targets:

* WinUI
* Desktop .NET Framework 4.6.2+
* .NET 8 / 9 / 10 / 11
* Blazor
* Uno Platform

### SDK Requirements

### V13.x.x

#### Breaking changes in V13.x

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

#### New in V13.x

* **`Refit.Testing`** — a new first-party package for testing Refit clients without a mocking library or a live
  server. You describe expected calls as a route table (`Route` → `Reply`) and point a real client at it via
  `StubHttp.CreateClient<T>(...)`. Route templates mirror your interface attributes, typed replies (`Reply.With<T>`)
  are serialized with the client's own serializer, and the sent request body can be read back as a typed object with
  `LastRequestBodyAsync<T>()`. It also includes `NetworkBehavior` for seeded latency/fault injection and
  `StubApiResponse<T>` for unit-testing code that consumes `IApiResponse<T>`. See
  [Testing your Refit clients](#testing-your-refit-clients).

### V12.x.x

#### Breaking changes in V12.x

Refit 12.0 is a large release centered on a near-complete rewrite of request building. The source generator now builds
eligible HTTP requests inline at compile time instead of going through the reflection request-builder pipeline, with the
reflection path kept as a fallback for shapes that cannot be generated inline.

Two breaking changes are called out for migration:

* `IApiResponse<T>` no longer shadows base interface members. The `new`-shadowed `Error`, `ContentHeaders`,
  `IsSuccessStatusCode`, and `IsSuccessful` members were removed from the generic interface. Source that reads these
  members still binds to the inherited base members, but assemblies compiled against v8-v11 should be recompiled.
  If you used `IsSuccessful` to narrow `Content` to non-null on an `IApiResponse<T>` value, use `HasContent` or
  `IsSuccessfulWithContent` instead.
* The default `System.Text.Json` serializer now reads numbers from JSON strings by setting
  `JsonNumberHandling.AllowReadingFromString`. To opt back out, set `NumberHandling = JsonNumberHandling.Strict` on
  your `JsonSerializerOptions`.

See the [Refit 12.0.0 release notes](https://github.com/reactiveui/refit/releases/tag/v12.0.0) for the full release
details.

### V11.x.x

#### Breaking changes in 11.x

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

### Updates in 8.0.x

Fixes for some issues experienced, this led to some breaking changes.
See [Releases](https://github.com/reactiveui/refit/releases) for full details.

### V6.x.x

Refit 6 requires Visual Studio 16.8 or higher, or the .NET SDK 5.0.100 or higher. It can target any .NET Standard 2.0
platform.

Refit 6 does not support the old `packages.config` format for NuGet references (as they do not support analyzers/source
generators). You must
[migrate to PackageReference](https://devblogs.microsoft.com/nuget/migrate-packages-config-to-package-reference/) to use
Refit v6 and later.

#### Breaking changes in 6.x

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

### Source generation

Refit ships Roslyn source generators with the main `Refit` package. Projects that reference Refit through
`PackageReference` get generated client implementations at build time without adding another package.

Generated Refit client source is compatible with C# 7.3 as the baseline. When the consuming project uses C# 8 or newer
and nullable reference types are available, the generator also emits nullable directives and nullable annotations for
the generated client.

The generated clients are still created with the normal APIs:

```csharp
var api = RestService.For<IGitHubApi>("https://api.github.com");
```

or through `Refit.HttpClientFactory`:

```csharp
services
    .AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));
```

The generator uses the same `RefitSettings` you pass to `RestService.For<T>` or `AddRefitClient<T>`. That means generated
clients continue to honor settings such as:

* `ContentSerializer`
* `UrlParameterFormatter`
* `UrlParameterKeyFormatter`
* `CollectionFormat`
* `AuthorizationHeaderValueGetter`
* `ExceptionFactory`
* `DeserializationExceptionFactory`
* `TransportExceptionFactory`
* `HttpRequestMessageOptions`
* `Version` and `VersionPolicy`

#### Generated-only client creation

For Native AoT or trimming-sensitive applications, `RestService.ForGenerated<T>` creates a client only when Refit's
source generator registered an implementation for that interface:

```csharp
using var client = new HttpClient
{
    BaseAddress = new Uri("https://api.github.com")
};

var api = RestService.ForGenerated<IGitHubApi>(client);
```

`ForGenerated<T>` does not fall back to the runtime reflection client. When the generated client can build all requests
directly, Refit also avoids creating the reflection request builder for that client. If the generated implementation is
not available at runtime, Refit throws an `InvalidOperationException` that points back to source generation setup.

#### Generated request building

Refit's source generator now emits request construction directly for supported methods by default. Instead of generating
a method body that calls the reflective runtime request-building pipeline through `BuildRestResultFuncForMethod`, the
generated client can create the `HttpRequestMessage`, apply headers/properties/body content, and dispatch it through
Refit's generated-request runtime helpers.

This default path reduces runtime reflection, method metadata lookup, object-array argument packing, and delegate
construction for request shapes the generator can safely model. It currently covers common request features including:

* parameters that appear in the path template
* static `[Headers]`
* dynamic `[Header]` parameters
* `[HeaderCollection]` dictionaries
* `[Body]` content
* `[Property]` parameters
* `[Property]` interface properties
* cancellation tokens
* `Task`, `Task<T>`, `Task<ApiResponse<T>>`, and related response wrappers
* `IAsyncEnumerable<T>` for streamed responses

If a method uses a shape the generator cannot safely emit yet, Refit falls back to the existing runtime request-builder
path for that method.

You can explicitly turn generated request building off in a project file:

```xml
<PropertyGroup>
  <RefitGeneratedRequestBuilding>false</RefitGeneratedRequestBuilding>
</PropertyGroup>
```

That keeps the source-generated interface implementation but uses the legacy reflective request-building call path
inside generated methods.

If you need to disable Refit source generation entirely, set:

```xml
<PropertyGroup>
  <DisableRefitSourceGenerator>true</DisableRefitSourceGenerator>
</PropertyGroup>
```

Most applications should leave both settings unset.

#### Analyzer diagnostics and code fixes

Refit also ships analyzers with the main package. They report common interface issues at compile time, including:

* methods or properties on Refit interfaces that cannot be generated or called by Refit
* route templates that use backslashes instead of forward slashes
* methods with more than one `CancellationToken` parameter
* `[HeaderCollection]` parameters that are not `IDictionary<string, string>`

Where the fix is mechanical, Refit includes a code fix. Current fixes can replace route backslashes with forward
slashes and change invalid `[HeaderCollection]` parameter types to `IDictionary<string, string>`.

### API Attributes

Every method must have an HTTP attribute that provides the request method and
relative URL. There are six built-in annotations: Get, Post, Put, Delete, Patch and
Head. The relative URL of the resource is specified in the annotation.

```csharp
[Get("/users/list")]
```

You can also specify query parameters in the URL:

```csharp
[Get("/users/list?sort=desc")]
```

A request URL can be updated dynamically using replacement blocks and
parameters on the method. A replacement block is an alphanumeric string
surrounded by { and }.

If the name of your parameter doesn't match the name in the URL path, use the
`AliasAs` attribute.

```csharp
[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId);
```

A request url can also bind replacement blocks to a custom object

```csharp
[Get("/group/{request.groupId}/users/{request.userId}")]
Task<List<User>> GroupList(UserGroupRequest request);

class UserGroupRequest{
    int groupId { get;set; }
    int userId { get;set; }
}

```

Parameters that are not specified as a URL substitution will automatically be
used as query parameters. This is different than Retrofit, where all
parameters must be explicitly specified.

The comparison between parameter name and URL parameter is *not*
case-sensitive, so it will work correctly if you name your parameter `groupId`
in the path `/group/{groupid}/show` for example.

```csharp
[Get("/group/{groupid}/users")]
Task<List<User>> GroupList(int groupId, [AliasAs("sort")] string sortOrder);

GroupList(4, "desc");
>>> "/group/4/users?sort=desc"
```

Round-tripping route parameter syntax: Forward slashes aren't encoded when using a double-asterisk (\*\*) catch-all
parameter syntax.

During link generation, the routing system encodes the value captured in a double-asterisk (\*\*) catch-all parameter (
for example, {**myparametername}) except the forward slashes.

The type of round-tripping route parameter must be string.

```csharp
[Get("/search/{**page}")]
Task<List<Page>> Search(string page);

Search("admin/products");
>>> "/search/admin/products"
```

By default Refit throws if a route template contains a placeholder with no matching method argument. If you want to
resolve a placeholder later yourself (for example an API-versioning token rewritten inside a `DelegatingHandler`), set
`AllowUnmatchedRouteParameters` on `RefitSettings`. The unmatched `{token}` is then left in the URL verbatim instead of
throwing.

```csharp
var settings = new RefitSettings { AllowUnmatchedRouteParameters = true };
var api = RestService.For<IVersionedApi>("https://api.example.com", settings);

// [Get("/api/{version:apiVersion}/values")]
// the {version:apiVersion} token is left in the path for a DelegatingHandler to replace.
```

#### Base address and URL resolution

By default Refit requires relative paths to start with `/`, prepends the base address path itself, and trims a trailing
slash from the base address. If you would rather have the base address and relative URL combined the same way
`HttpClient` and `System.Uri` do (RFC 3986), set `UrlResolution` on `RefitSettings`:

```csharp
var settings = new RefitSettings { UrlResolution = UrlResolutionMode.Rfc3986 };
var api = RestService.For<IMyApi>("https://api.example.com/api/v1/", settings);
```

Under `UrlResolutionMode.Rfc3986` the leading-slash requirement is relaxed and the trailing slash on the base address is
significant, exactly as with `HttpClient`:

```csharp
// base address "https://api.example.com/api/v1/"
[Get("values")]   // -> https://api.example.com/api/v1/values  (appended)
[Get("/values")]  // -> https://api.example.com/values         (leading slash replaces the base path)
```

### Querystrings

#### Dynamic Querystring Parameters

If you specify an `object` as a query parameter, all public properties which are not null are used as query parameters.
This previously only applied to GET requests, but has now been expanded to all HTTP request methods, partly thanks to
Twitter's hybrid API that insists on non-GET requests with querystring parameters.
Use the `Query` attribute to change the behavior to 'flatten' your query parameter object. If using this Attribute you
can specify values for the Delimiter and the Prefix which are used to 'flatten' the object.

```csharp
public class MyQueryParams
{
    [AliasAs("order")]
    public string SortOrder { get; set; }

    public int Limit { get; set; }

    public KindOptions Kind { get; set; }
}

public enum KindOptions
{
    Foo,

    [EnumMember(Value = "bar")]
    Bar
}


[Get("/group/{id}/users")]
Task<List<User>> GroupList([AliasAs("id")] int groupId, MyQueryParams params);

[Get("/group/{id}/users")]
Task<List<User>> GroupListWithAttribute([AliasAs("id")] int groupId, [Query(".","search")] MyQueryParams params);


params.SortOrder = "desc";
params.Limit = 10;
params.Kind = KindOptions.Bar;

GroupList(4, params)
>>> "/group/4/users?order=desc&Limit=10&Kind=bar"

GroupListWithAttribute(4, params)
>>> "/group/4/users?search.order=desc&search.Limit=10&search.Kind=bar"
```

A similar behavior exists if using a Dictionary, but without the advantages of the `AliasAs` attributes and of course no
intellisense and/or type safety.

You can also specify querystring parameters with [Query] and have them flattened in non-GET requests, similar to:

```csharp
[Post("/statuses/update.json")]
Task<Tweet> PostTweet([Query]TweetParams params);
```

Where `TweetParams` is a POCO, and properties will also support `[AliasAs]` attributes.

If you need to keep internal-only properties on your query DTO, mark them with one of the standard ignore attributes and
Refit will skip them when building the query string:

- `[IgnoreDataMember]`
- `[System.Text.Json.Serialization.JsonIgnore]`
- `[Newtonsoft.Json.JsonIgnore]`

#### Collections as Querystring parameters

Use the `Query` attribute to specify format in which collections should be formatted in query string

```csharp
[Get("/users/list")]
Task Search([Query(CollectionFormat.Multi)]int[] ages);

Search(new [] {10, 20, 30})
>>> "/users/list?ages=10&ages=20&ages=30"

[Get("/users/list")]
Task Search([Query(CollectionFormat.Csv)]int[] ages);

Search(new [] {10, 20, 30})
>>> "/users/list?ages=10%2C20%2C30"
```

You can also specify collection format in `RefitSettings`, that will be used by default, unless explicitly defined in
`Query` attribute.

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        CollectionFormat = CollectionFormat.Multi
    });
```

#### Unescape Querystring parameters

Use the `QueryUriFormat` attribute to specify if the query parameters should be url escaped

```csharp
[Get("/query")]
[QueryUriFormat(UriFormat.Unescaped)]
Task Query(string q);

Query("Select+Id,Name+From+Account")
>>> "/query?q=Select+Id,Name+From+Account"
```

#### Custom Querystring parameter formatting

**Formatting Keys**

To customize the format of query keys, you have two main options:

1. **Using the `AliasAs` Attribute**:

   You can use the `AliasAs` attribute to specify a custom key name for a property. This attribute will always take
   precedence over any key formatter you specify.

   ```csharp
   public class MyQueryParams
   {
       [AliasAs("order")]
       public string SortOrder { get; set; }

       public int Limit { get; set; }
   }

   [Get("/group/{id}/users")]
   Task<List<User>> GroupList([AliasAs("id")] int groupId, [Query] MyQueryParams params);

   params.SortOrder = "desc";
   params.Limit = 10;

   GroupList(1, params);
   ```

   This will generate the following request:

   ```
   /group/1/users?order=desc&Limit=10
   ```

2. **Using the `RefitSettings.UrlParameterKeyFormatter` Property**:

   By default, Refit uses the property name as the query key without any additional formatting. If you want to apply a
   custom format across all your query keys, you can use the `UrlParameterKeyFormatter` property. Remember that if a
   property has an `AliasAs` attribute, it will be used regardless of the formatter.

   The following example uses the built-in `CamelCaseUrlParameterKeyFormatter`:

   ```csharp
   public class MyQueryParams
   {
       public string SortOrder { get; set; }

       [AliasAs("queryLimit")]
       public int Limit { get; set; }
   }

   [Get("/group/users")]
   Task<List<User>> GroupList([Query] MyQueryParams params);

   params.SortOrder = "desc";
   params.Limit = 10;
   ```

   The request will look like:

   ```
   /group/users?sortOrder=desc&queryLimit=10
   ```

**Note**: The `AliasAs` attribute always takes the top priority. If both the attribute and a custom key formatter are
present, the `AliasAs` attribute's value will be used.

**Built-in key formatters and naming-convention presets**:

Refit ships `CamelCaseUrlParameterKeyFormatter`, `SnakeCaseUrlParameterKeyFormatter`, and
`KebabCaseUrlParameterKeyFormatter`. To apply a single naming convention consistently across query keys, form field
names **and** the JSON request body, use the `RefitSettings` presets — each wires up the matching
`UrlParameterKeyFormatter` and `JsonNamingPolicy` together:

```csharp
var api = RestService.For<IMyApi>("https://api.example.com", RefitSettings.SnakeCase());
// also available: RefitSettings.KebabCase() and RefitSettings.CamelCase()
```

**Per-property key prefix and delimiter**:

A `[Query]` attribute on a property of a complex query object customizes that property's key as
`{prefix}{delimiter}{name}` (matching how form fields are named):

```csharp
public class Form
{
    [Query("-", "dontlog")]
    public string Password { get; set; }
}
// => ?dontlog-Password=...
```

**Serializing a value object via `ToString()`**:

By default a complex query parameter is flattened into its public properties. To instead send a single value using the
object's `ToString()` under the parameter's own name, mark it with an explicit empty format `[Query(Format = "")]` (or
`[Query(TreatAsString = true)]`):

```csharp
[Get("/info")]
Task<string> GetInfo([Query(Format = "")] Size size); // => ?size=medium  (uses size.ToString())
```

#### Formatting URL Parameter Values with the `UrlParameterFormatter`

In Refit, the `UrlParameterFormatter` property within `RefitSettings` allows you to customize how parameter values are
formatted in the URL. This can be particularly useful when you need to format dates, numbers, or other types in a
specific manner that aligns with your API's expectations.

**Using `UrlParameterFormatter`**:

Assign a custom formatter that implements the `IUrlParameterFormatter` interface to the `UrlParameterFormatter`
property.

```csharp
public class CustomDateUrlParameterFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        if (value is DateTime dt)
        {
            return dt.ToString("yyyyMMdd");
        }

        return value?.ToString();
    }
}

var settings = new RefitSettings
{
    UrlParameterFormatter = new CustomDateUrlParameterFormatter()
};
```

In this example, a custom formatter is created for date values. Whenever a `DateTime` parameter is encountered, it
formats the date as `yyyyMMdd`.

**Formatting Dictionary Keys**:

When dealing with dictionaries, it's important to note that keys are treated as values. If you need custom formatting
for dictionary keys, you should use the `UrlParameterFormatter` as well.

For instance, if you have a dictionary parameter and you want to format its keys in a specific way, you can handle that
in the custom formatter:

```csharp
public class CustomDictionaryKeyFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        // Handle dictionary keys
        if (attributeProvider is PropertyInfo prop && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // Custom formatting logic for dictionary keys
            return value?.ToString().ToUpperInvariant();
        }

        return value?.ToString();
    }
}

var settings = new RefitSettings
{
    UrlParameterFormatter = new CustomDictionaryKeyFormatter()
};
```

In the above example, the dictionary keys will be converted to uppercase.

### Body content

One of the parameters in your method can be used as the body, by using the
Body attribute:

```csharp
[Post("/users/new")]
Task CreateUser([Body] User user);
```

There are four possibilities for supplying the body data, depending on the
type of the parameter:

* If the type is `Stream`, the content will be streamed via `StreamContent`
* If the type is `string`, the string will be used directly as the content unless `[Body(BodySerializationMethod.Json)]`
  is set which will send it as a `StringContent`
* If the parameter has the attribute `[Body(BodySerializationMethod.UrlEncoded)]`,
  the content will be URL-encoded (see [form posts](#form-posts) below)
* If the parameter has the attribute `[Body(BodySerializationMethod.JsonLines)]`,
  an enumerable body is sent as [JSON Lines](https://jsonlines.org) (see [JSON Lines content](#json-lines-content) below)
* For all other types, the object will be serialized using the content serializer specified in
  RefitSettings (JSON is the default).

#### Buffering and the `Content-Length` header

By default, Refit streams the body content without buffering it. This means you can
stream a file from disk, for example, without incurring the overhead of loading
the whole file into memory. The downside of this is that no `Content-Length` header
is set _on the request_. If your API needs you to send a `Content-Length` header with
the request, you can disable this streaming behavior by setting the `buffered` argument
of the `[Body]` attribute to `true`:

```csharp
Task CreateUser([Body(buffered: true)] User user);
```

#### JSON content

JSON requests and responses are serialized/deserialized using an instance of the `IHttpContentSerializer` interface.
Refit provides two implementations out of the box: `SystemTextJsonContentSerializer` (which is the default JSON
serializer) and `NewtonsoftJsonContentSerializer`. The first uses `System.Text.Json` APIs and is focused on high
performance and low memory usage, while the latter uses the known `Newtonsoft.Json` library and is more versatile and
customizable. You can read more about the two serializers and the main differences between the
two [at this link](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to).

The default `System.Text.Json` serializer uses camelCase property names, case-insensitive matching, and reads numbers
from JSON strings (`NumberHandling = AllowReadingFromString`). Override any of these by starting from Refit's defaults,
tweaking the `JsonSerializerOptions`, and passing them in:

```csharp
var options = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
options.NumberHandling = JsonNumberHandling.Strict; // opt out of reading numbers from strings

var settings = new RefitSettings(new SystemTextJsonContentSerializer(options));
```

##### Fast-path source-generated serialization

The default options add custom converters and set `NumberHandling`, which the System.Text.Json source generator does
not support on its serialization [fast-path](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation-modes#serialization-optimization-fast-path-mode),
so the default serializer always uses the (slower) metadata logic. If you want the fast-path, start from
`GetFastPathJsonSerializerOptions()` instead — it omits the converters and `NumberHandling` so the options stay
fast-path eligible — and assign a source-generated `TypeInfoResolver`:

```csharp
var options = SystemTextJsonContentSerializer.GetFastPathJsonSerializerOptions();
options.TypeInfoResolver = MyJsonContext.Default; // a JsonSerializerContext you declare

var settings = new RefitSettings(new SystemTextJsonContentSerializer(options));
```

```csharp
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MyRequest))]
internal partial class MyJsonContext : JsonSerializerContext;
```

Two caveats: dropping the converters means `object`-typed members are no longer inferred and enums are no longer written
as camelCase strings (add your own converters only if you accept losing the fast-path); and the fast-path runs through
the **synchronous** serialization primitives (`SerializeToUtf8Bytes`, `Serialize(Utf8JsonWriter, ...)`) — the one API
that bypasses it is the built-in `JsonSerializer.SerializeAsync(Stream, ...)`.

By default Refit serializes request bodies with `JsonContent`, which uses exactly that `SerializeAsync(Stream)` path, so
the fast-path is not used even with fast-path-eligible options. To opt in, set `RequestBodySerialization` on
`RefitSettings` to one of the synchronous modes, which serialize through the fast-path:

- `RequestBodySerializationMode.Buffered` — serialize synchronously to UTF-8 bytes up front and send as a
  `ByteArrayContent`. Sets a `Content-Length` header; best for small-to-medium bodies.
- `RequestBodySerializationMode.Streamed` — serialize through a `Utf8JsonWriter` written to the request stream and
  flushed asynchronously. No `Content-Length`, but peak memory is bounded (pooled writer chunks rather than the whole
  body); best for large uploads.

```csharp
var options = SystemTextJsonContentSerializer.GetFastPathJsonSerializerOptions();
options.TypeInfoResolver = MyJsonContext.Default;

var settings = new RefitSettings(new SystemTextJsonContentSerializer(options))
{
    RequestBodySerialization = RequestBodySerializationMode.Buffered, // or .Streamed for large uploads
};
```

Both modes require the content serializer to implement `ISynchronousContentSerializer` (the default
`SystemTextJsonContentSerializer` does); otherwise the body falls back to the default asynchronous serialization.

For instance, here is how to create a new `RefitSettings` instance using the `Newtonsoft.Json`-based serializer (you'll
also need to add a `PackageReference` to `Refit.Newtonsoft.Json`):

```csharp
var settings = new RefitSettings(new NewtonsoftJsonContentSerializer());
```

If you're using `Newtonsoft.Json` APIs, you can customize their behavior by setting the
`Newtonsoft.Json.JsonConvert.DefaultSettings` property:

```csharp
JsonConvert.DefaultSettings =
    () => new JsonSerializerSettings() {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = {new StringEnumConverter()}
    };

// Serialized as: {"day":"Saturday"}
await PostSomeStuff(new { Day = DayOfWeek.Saturday });
```

As these are global settings they will affect your entire application. It
might be beneficial to isolate the settings for calls to a particular API.
When creating a Refit generated live interface, you may optionally pass a
`RefitSettings` that will allow you to specify what serializer settings you
would like. This allows you to have different serializer settings for separate
APIs:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        ContentSerializer = new NewtonsoftJsonContentSerializer(
            new JsonSerializerSettings {
                ContractResolver = new SnakeCasePropertyNamesContractResolver()
        }
    )});

var otherApi = RestService.For<IOtherApi>("https://api.example.com",
    new RefitSettings {
        ContentSerializer = new NewtonsoftJsonContentSerializer(
            new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
        }
    )});
```

Property serialization/deserialization can be customized using Json.NET's
JsonProperty attribute:

```csharp
public class Foo
{
    // Works like [AliasAs("b")] would in form posts (see below)
    [JsonProperty(PropertyName="b")]
    public string Bar { get; set; }
}
```

##### JSON source generator

To apply the benefits of the
new [JSON source generator](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/) for
System.Text.Json added in .NET 6, you can use `SystemTextJsonContentSerializer` with a custom instance of
`RefitSettings` and `JsonSerializerOptions`:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        ContentSerializer = new SystemTextJsonContentSerializer(MyJsonSerializerContext.Default.Options)
    });
```

When using `System.Text.Json` polymorphism features such as `[JsonDerivedType]` / `[JsonPolymorphic]`, Refit serializes
request bodies using the **declared Refit method parameter type** rather than the boxed runtime `object`. This ensures
type discriminators configured on the base contract are preserved in outgoing request payloads.

#### <a name="json-lines-content"></a>JSON Lines content

Some APIs accept a batch of records as [JSON Lines](https://jsonlines.org) (newline-delimited JSON), where each line is
a self-contained JSON document. Mark an enumerable body parameter with `[Body(BodySerializationMethod.JsonLines)]` and
Refit serializes each element with the configured content serializer, writing one document per line:

```csharp
public interface IDocumentApi
{
    [Post("/collections/companies/documents/import")]
    Task ImportAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<Company> documents);
}
```

The request body is streamed (one serialized element per line, separated by `\n`) with a content type of
`application/x-ndjson`. A non-enumerable value is sent as a single line. If you need a different content type (for
example `text/plain`), set it with a [static header](#static-headers) or a [dynamic header](#dynamic-headers).

#### Streaming responses with `IAsyncEnumerable<T>`

Declare a method that returns `IAsyncEnumerable<T>` to consume a large response without buffering the whole body. Refit
reads the response with `HttpCompletionOption.ResponseHeadersRead` and yields each element as it is deserialized:

```csharp
public interface IDocumentApi
{
    [Get("/documents")]
    IAsyncEnumerable<Company> GetDocuments();
}

await foreach (var company in api.GetDocuments())
{
    // each item is yielded as soon as it is read off the wire
}
```

The frame format is auto-detected from the response content type: a content type of `application/jsonl`,
`application/x-ndjson`, or `application/x-jsonlines` is read as JSON Lines (one value per line); anything else is read
as a single streamed JSON array. To observe cancellation, add a `CancellationToken` parameter, or use
`WithCancellation(token)` on the enumerable.

Streaming requires the configured `ContentSerializer` to implement `IStreamingContentSerializer`. The default
`SystemTextJsonContentSerializer` does; a serializer that does not will throw `NotSupportedException` when the sequence
is enumerated.

#### XML Content

XML requests and responses are serialized/deserialized using _System.Xml.Serialization.XmlSerializer_.
By default, Refit will use JSON content serialization, to use XML content configure the ContentSerializer to use the
`XmlContentSerializer`:

```csharp
var gitHubApi = RestService.For<IXmlApi>("https://www.w3.org/XML",
    new RefitSettings {
        ContentSerializer = new XmlContentSerializer()
    });
```

Property serialization/deserialization can be customized using attributes found in the _System.Xml.Serialization_
namespace:

```csharp
    public class Foo
    {
        [XmlElement(Namespace = "https://www.w3.org/XML")]
        public string Bar { get; set; }
    }
```

The _System.Xml.Serialization.XmlSerializer_ provides many options for serializing, those options can be set by
providing an `XmlContentSerializerSettings` to the `XmlContentSerializer` constructor:

```csharp
var gitHubApi = RestService.For<IXmlApi>("https://www.w3.org/XML",
    new RefitSettings {
        ContentSerializer = new XmlContentSerializer(
            new XmlContentSerializerSettings
            {
                XmlReaderWriterSettings = new XmlReaderWriterSettings()
                {
                    ReaderSettings = new XmlReaderSettings
                    {
                        IgnoreWhitespace = true
                    }
                }
            }
        )
    });
```

#### <a name="form-posts"></a>Form posts

For APIs that take form posts (i.e. serialized as `application/x-www-form-urlencoded`),
initialize the Body attribute with `BodySerializationMethod.UrlEncoded`.

The parameter can be an `IDictionary`:

```csharp
public interface IMeasurementProtocolApi
{
    [Post("/collect")]
    Task Collect([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
}

var data = new Dictionary<string, object> {
    {"v", 1},
    {"tid", "UA-1234-5"},
    {"cid", new Guid("d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c")},
    {"t", "event"},
};

// Serialized as: v=1&tid=UA-1234-5&cid=d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c&t=event
await api.Collect(data);
```

Or you can just pass any object and all _public, readable_ properties will
be serialized as form fields in the request. This approach allows you to alias
property names using `[AliasAs("whatever")]` which can help if the API has
cryptic field names:

```csharp
public interface IMeasurementProtocolApi
{
    [Post("/collect")]
    Task Collect([Body(BodySerializationMethod.UrlEncoded)] Measurement measurement);
}

public class Measurement
{
    // Properties can be read-only and [AliasAs] isn't required
    public int v { get { return 1; } }

    [AliasAs("tid")]
    public string WebPropertyId { get; set; }

    [AliasAs("cid")]
    public Guid ClientId { get; set; }

    [AliasAs("t")]
    public string Type { get; set; }

    public object IgnoreMe { private get; set; }
}

var measurement = new Measurement {
    WebPropertyId = "UA-1234-5",
    ClientId = new Guid("d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c"),
    Type = "event"
};

// Serialized as: v=1&tid=UA-1234-5&cid=d1e9ea6b-2e8b-4699-93e0-0bcbd26c206c&t=event
await api.Collect(measurement);
```

If you have a type that has `[JsonProperty(PropertyName)]` attributes setting property aliases, Refit will use those
too (`[AliasAs]` will take precedence where you have both).
This means that the following type will serialize as `one=value1&two=value2`:

```csharp

public class SomeObject
{
    [JsonProperty(PropertyName = "one")]
    public string FirstProperty { get; set; }

    [JsonProperty(PropertyName = "notTwo")]
    [AliasAs("two")]
    public string SecondProperty { get; set; }
}

```

**NOTE:** This use of `AliasAs` applies to querystring parameters and form body posts, but not to response objects; for
aliasing fields on response objects, you'll still need to use `[JsonProperty("full-property-name")]`.

##### Sending `null` values

By default, a property whose value is `null` is omitted from the form body (and from object querystrings). To send
an explicit empty value (`key=`) for a `null` property instead of omitting it, set `SerializeNull` on its `[Query]`
attribute:

```csharp
public class Measurement
{
    [Query(SerializeNull = true)]
    public string? Note { get; set; }
}

// With Note = null, serialized as: ...&Note=
```

##### Reflection-free form serialization

When generated request building is enabled (the default), Refit's source generator emits the form-field flattening
for strongly-typed bodies at compile time, so no reflection is used at request time. This applies when the body is a
concrete class or struct serialized with the built-in `SystemTextJsonContentSerializer`. Bodies typed as `object`,
an `IDictionary`, a collection, or serialized with a custom `IHttpContentSerializer` fall back to the reflection-based
path, which remains fully supported and produces identical output.

### Setting request headers

#### Static headers

You can set one or more static request headers for a request applying a `Headers`
attribute to the method:

```csharp
[Headers("User-Agent: Awesome Octocat App")]
[Get("/users/{user}")]
Task<User> GetUser(string user);
```

Static headers can also be added to _every request in the API_ by applying the
`Headers` attribute to the interface:

```csharp
[Headers("User-Agent: Awesome Octocat App")]
public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);

    [Post("/users/new")]
    Task CreateUser([Body] User user);
}
```

#### Dynamic headers

If the content of the header needs to be set at runtime, you can add a header
with a dynamic value to a request by applying a `Header` attribute to a parameter:

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user, [Header("Authorization")] string authorization);

// Will add the header "Authorization: token OAUTH-TOKEN" to the request
var user = await GetUser("octocat", "token OAUTH-TOKEN");
```

Adding an `Authorization` header is such a common use case that you can add an access token to a request by applying an
`Authorize` attribute to a parameter and optionally specifying the scheme:

```csharp
[Get("/users/{user}")]
Task<User> GetUser(string user, [Authorize("Bearer")] string token);

// Will add the header "Authorization: Bearer OAUTH-TOKEN}" to the request
var user = await GetUser("octocat", "OAUTH-TOKEN");

//note: the scheme defaults to Bearer if none provided
```

If you need to set multiple headers at runtime, you can add a `IDictionary<string, string>`
and apply a `HeaderCollection` attribute to the parameter and it will inject the headers into the request:

[//]: # ({% raw %})

```csharp

[Get("/users/{user}")]
Task<User> GetUser(string user, [HeaderCollection] IDictionary<string, string> headers);

var headers = new Dictionary<string, string> {{"Authorization","Bearer tokenGoesHere"}, {"X-Tenant-Id","123"}};
var user = await GetUser("octocat", headers);
```

[//]: # ({% endraw %})

#### Bearer Authentication

Most APIs need some sort of Authentication. The most common is OAuth Bearer authentication. A header is added to each
request of the form: `Authorization: Bearer <token>`. Refit makes it easy to insert your logic to get the token however
your app needs, so you don't have to pass a token into each method.

1. Add `[Headers("Authorization: Bearer")]` to the interface or methods which need the token.
2. Set `AuthorizationHeaderValueGetter` in the `RefitSettings` instance. Refit will call your delegate each time it
   needs to obtain the token, so it's a good idea for your mechanism to cache the token value for some period within the
   token lifetime.

`AuthorizationHeaderValueGetter` works whether you create clients with `RestService.For<T>("https://...")` or supply
your own `HttpClient` via `RestService.For<T>(httpClient, settings)`. If your API methods accept a `CancellationToken`,
that token is propagated to the getter delegate.

#### Reducing header boilerplate with DelegatingHandlers (Authorization headers worked example)

Although we make provisions for adding dynamic headers at runtime directly in Refit,
most use-cases would likely benefit from registering a custom `DelegatingHandler` in order to inject the headers as part
of the `HttpClient` middleware pipeline
thus removing the need to add lots of `[Header]` or `[HeaderCollection]` attributes.

In the example above we are leveraging a `[HeaderCollection]` parameter to inject an `Authorization` and `X-Tenant-Id`
header.
This is quite a common scenario if you are integrating with a 3rd party that uses OAuth2. While it's ok for the
occasional endpoint,
it would be quite cumbersome if we had to add that boilerplate to every method in our interface.

In this example we will assume our application is a multi-tenant application that is able to pull information about a
tenant through
some interface `ITenantProvider` and has a data store `IAuthTokenStore` that can be used to retrieve an auth token to
attach to the outbound request.

```csharp

 //Custom delegating handler for adding Auth headers to outbound requests
 class AuthHeaderHandler : DelegatingHandler
 {
     private readonly ITenantProvider tenantProvider;
     private readonly IAuthTokenStore authTokenStore;

    public AuthHeaderHandler(ITenantProvider tenantProvider, IAuthTokenStore authTokenStore)
    {
         this.tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
         this.authTokenStore = authTokenStore ?? throw new ArgumentNullException(nameof(authTokenStore));
         // InnerHandler must be left as null when using DI, but must be assigned a value when
         // using RestService.For<IMyApi>
         // InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await authTokenStore.GetToken();

        //potentially refresh token here if it has expired etc.

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Tenant-Id", tenantProvider.GetTenantId());

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

//Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<ITenantProvider, TenantProvider>();
    services.AddTransient<IAuthTokenStore, AuthTokenStore>();
    services.AddTransient<AuthHeaderHandler>();

    //this will add our refit api implementation with an HttpClient
    //that is configured to add auth headers to all requests

    //note: AddRefitClient<T> requires a reference to Refit.HttpClientFactory
    //note: the order of delegating handlers is important and they run in the order they are added!

    services.AddRefitClient<ISomeThirdPartyApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
        .AddHttpMessageHandler<AuthHeaderHandler>();
        //you could add Polly here to handle HTTP 429 / HTTP 503 etc
}

//Your application code
public class SomeImportantBusinessLogic
{
    private ISomeThirdPartyApi thirdPartyApi;

    public SomeImportantBusinessLogic(ISomeThirdPartyApi thirdPartyApi)
    {
        this.thirdPartyApi = thirdPartyApi;
    }

    public async Task DoStuffWithUser(string username)
    {
        var user = await thirdPartyApi.GetUser(username);
        //do your thing
    }
}
```

If you aren't using dependency injection then you could achieve the same thing by doing something like this:

```csharp
var api = RestService.For<ISomeThirdPartyApi>(new HttpClient(new AuthHeaderHandler(tenantProvider, authTokenStore))
    {
        BaseAddress = new Uri("https://api.example.com")
    }
);

var user = await thirdPartyApi.GetUser(username);
//do your thing
```

#### Redefining headers

Unlike Retrofit, where headers do not overwrite each other and are all added to
the request regardless of how many times the same header is defined, Refit takes
a similar approach to the approach ASP.NET MVC takes with action filters &mdash;
**redefining a header will replace it**, in the following order of precedence:

* `Headers` attribute on the interface _(lowest priority)_
* `Headers` attribute on the method
* `Header` attribute or `HeaderCollection` attribute on a method parameter _(highest priority)_

```csharp
[Headers("X-Emoji: :rocket:")]
public interface IGitHubApi
{
    [Get("/users/list")]
    Task<List> GetUsers();

    [Get("/users/{user}")]
    [Headers("X-Emoji: :smile_cat:")]
    Task<User> GetUser(string user);

    [Post("/users/new")]
    [Headers("X-Emoji: :metal:")]
    Task CreateUser([Body] User user, [Header("X-Emoji")] string emoji);
}

// X-Emoji: :rocket:
var users = await GetUsers();

// X-Emoji: :smile_cat:
var user = await GetUser("octocat");

// X-Emoji: :trollface:
await CreateUser(user, ":trollface:");
```

**Note:** This redefining behavior only applies to headers _with the same name_. Headers with different names are not
replaced. The following code will result in all headers being included:

```csharp
[Headers("Header-A: 1")]
public interface ISomeApi
{
    [Headers("Header-B: 2")]
    [Post("/post")]
    Task PostTheThing([Header("Header-C")] int c);
}

// Header-A: 1
// Header-B: 2
// Header-C: 3
var user = await api.PostTheThing(3);
```

#### Removing headers

Headers defined on an interface or method can be removed by redefining
a static header without a value (i.e. without `: <value>`) or passing `null` for
a dynamic header. _Empty strings will be included as empty headers._

```csharp
[Headers("X-Emoji: :rocket:")]
public interface IGitHubApi
{
    [Get("/users/list")]
    [Headers("X-Emoji")] // Remove the X-Emoji header
    Task<List> GetUsers();

    [Get("/users/{user}")]
    [Headers("X-Emoji:")] // Redefine the X-Emoji header as empty
    Task<User> GetUser(string user);

    [Post("/users/new")]
    Task CreateUser([Body] User user, [Header("X-Emoji")] string emoji);
}

// No X-Emoji header
var users = await GetUsers();

// X-Emoji:
var user = await GetUser("octocat");

// No X-Emoji header
await CreateUser(user, null);

// X-Emoji:
await CreateUser(user, "");
```

### Passing state into DelegatingHandlers

If there is runtime state that you need to pass to a `DelegatingHandler` you can add a property with a dynamic value to
the underlying `HttpRequestMessage.Properties`
by applying a `Property` attribute to a parameter:

```csharp
public interface IGitHubApi
{
    [Post("/users/new")]
    Task CreateUser([Body] User user, [Property("SomeKey")] string someValue);

    [Post("/users/new")]
    Task CreateUser([Body] User user, [Property] string someOtherKey);
}
```

The attribute constructor optionally takes a string which becomes the key in the `HttpRequestMessage.Properties`
dictionary.
If no key is explicitly defined then the name of the parameter becomes the key.
If a key is defined multiple times the value in `HttpRequestMessage.Properties` will be overwritten.
The parameter itself can be any `object`. Properties can be accessed inside a `DelegatingHandler` as follows:

> ⚠️ **Important for `IHttpClientFactory` users:** `DelegatingHandler` instances are pooled and can live longer than a
> single request scope. Avoid reading per-request state from services that may be scoped/cached across handler lifetimes (
> for example a tenant/customer resolver stored on the handler). For per-request values like `CustomerId`, pass the value
> through `[Property]` so each request carries its own state.

```csharp
class RequestPropertyHandler : DelegatingHandler
{
    public RequestPropertyHandler(HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // See if the request has a the property
        if(request.Properties.ContainsKey("SomeKey"))
        {
            var someProperty = request.Properties["SomeKey"];
            //do stuff
        }

        if(request.Properties.ContainsKey("someOtherKey"))
        {
            var someOtherProperty = request.Properties["someOtherKey"];
            //do stuff
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```

Note: in .NET 5 `HttpRequestMessage.Properties` has been marked `Obsolete` and Refit will instead populate the value
into the new `HttpRequestMessage.Options`.

#### Support for Polly and Polly.Context

Because Refit supports `HttpClientFactory` it is possible to configure Polly policies on your HttpClient.
If your policy makes use of `Polly.Context` this can be passed via Refit by adding
`[Property("PolicyExecutionContext")] Polly.Context context`
as behind the scenes `Polly.Context` is simply stored in `HttpRequestMessage.Properties` under the key
`PolicyExecutionContext` and is of type `Polly.Context`. It's only recommended to pass the `Polly.Context` this way if
your use case requires that the `Polly.Context` be initialized with dynamic content only known at runtime. If your
`Polly.Context` only requires the same content every time (e.g an `ILogger` that you want to use to log from inside your
policies) a cleaner approach is to inject the `Polly.Context` via a `DelegatingHandler` as described
in [#801](https://github.com/reactiveui/refit/issues/801#issuecomment-1137318526)

#### Target Interface Type and method info

There may be times when you want to know what the target interface type is of the Refit instance. An example is where
you
have a derived interface that implements a common base like this:

```csharp
public interface IGetAPI<TEntity>
{
    [Get("/{key}")]
    Task<TEntity> Get(long key);
}

public interface IUsersAPI : IGetAPI<User>
{
}

public interface IOrdersAPI : IGetAPI<Order>
{
}
```

You can access the concrete type of the interface for use in a handler, such as to alter the URL of the request:

[//]: # ({% raw %})

```csharp
class RequestPropertyHandler : DelegatingHandler
{
    public RequestPropertyHandler(HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the type of the target interface
        Type interfaceType = (Type)request.Properties[HttpMessageRequestOptions.InterfaceType];

        var builder = new UriBuilder(request.RequestUri);
        // Alter the Path in some way based on the interface or an attribute on it
        builder.Path = $"/{interfaceType.Name}{builder.Path}";
        // Set the new Uri on the outgoing message
        request.RequestUri = builder.Uri;

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```

[//]: # ({% endraw %})

The full method information (`RestMethodInfo`) is also always available in the request options. The `RestMethodInfo`
contains more information about the method being called such as the full `MethodInfo` when using reflection is needed:

[//]: # ({% raw %})

```csharp
class RequestPropertyHandler : DelegatingHandler
{
    public RequestPropertyHandler(HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {}

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the method info
        if (request.Options.TryGetValue(new HttpRequestOptionsKey<RestMethodInfo>(HttpRequestMessageOptions.RestMethodInfo), out RestMethodInfo restMethodInfo))
        {
            var builder = new UriBuilder(request.RequestUri);
            // Alter the Path in some way based on the method info or an attribute on it
            builder.Path = $"/{restMethodInfo.MethodInfo.Name}{builder.Path}";
            // Set the new Uri on the outgoing message
            request.RequestUri = builder.Uri;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
```

[//]: # ({% endraw %})

Note: in .NET 5 `HttpRequestMessage.Properties` has been marked `Obsolete` and Refit will instead populate the value
into the new `HttpRequestMessage.Options`. Refit provides `HttpRequestMessageOptions.InterfaceType` and
`HttpRequestMessageOptions.RestMethodInfo` to respectively access the interface type and REST method info from the
options.

### Multipart uploads

Methods decorated with `Multipart` attribute will be submitted with multipart content type.
At this time, multipart methods support the following parameter types:

- `string` (parameter name will be used as name and string value as value)
- `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan` (and `DateOnly` / `TimeOnly` on supported targets) — sent as their
  plain text form (not JSON-quoted)
- byte array
- `Stream`
- `FileInfo`

Name of the field in the multipart data priority precedence:

* `multipartItem.Name` if specified and not null (optional); dynamic, allows naming form data part at execution time.
* `[AliasAs]` attribute  (optional) that decorate the streamPart parameter in the method signature (see below); static,
  defined in code.
* `MultipartItem` parameter name (default) as defined in the method signature; static, defined in code.

A custom boundary can be specified with an optional string parameter to the `Multipart` attribute. If left empty, this
defaults to `----MyGreatBoundary`.

To specify the file name and content type for byte array (`byte[]`), `Stream` and `FileInfo` parameters, use of a
wrapper class is required.
The wrapper classes for these types are `ByteArrayPart`, `StreamPart` and `FileInfoPart`.

```csharp
public interface ISomeApi
{
    [Multipart]
    [Post("/users/{id}/photo")]
    Task UploadPhoto(int id, [AliasAs("myPhoto")] StreamPart stream);
}
```

To pass a `Stream` to this method, construct a StreamPart object like so:

```csharp
someApiInstance.UploadPhoto(id, new StreamPart(myPhotoStream, "photo.jpg", "image/jpeg"));
```

Note: The `AttachmentName` attribute that was previously described in this section has been deprecated and its use is
not recommended.

### Retrieving the response

Note that in Refit unlike in Retrofit, there is no option for a synchronous
network request - all requests must be async, either via `Task` or via
`IObservable`. There is also no option to create an async method via a Callback
parameter unlike Retrofit, because we live in the async/await future.

Similarly to how body content changes via the parameter type, the return type
will determine the content returned.

Returning Task without a type parameter will discard the content and solely
tell you whether or not the call succeeded:

```csharp
[Post("/users/new")]
Task CreateUser([Body] User user);

// This will throw if the network call fails
await CreateUser(someUser);
```

If the type parameter is 'HttpResponseMessage' or 'string', the raw response
message or the content as a string will be returned respectively.

```csharp
// Returns the content as a string (i.e. the JSON data)
[Get("/users/{user}")]
Task<string> GetUser(string user);

// Returns the raw response, as an IObservable that can be used with the
// Reactive Extensions
[Get("/users/{user}")]
IObservable<HttpResponseMessage> GetUser(string user);
```

There is also a generic wrapper class called `ApiResponse<T>` that can be used as a return type. Using this class as a
return type allows you to retrieve not just the content as an object, but also any metadata associated with the
request/response. This includes information such as response headers, the http status code and reason phrase (e.g. 404
Not Found), the response version, the original request message that was sent and in the case of an error, an
`ApiException` object containing details of the error. Following are some examples of how you can retrieve the response
metadata.

```csharp
//Returns the content within a wrapper class containing metadata about the request/response
[Get("/users/{user}")]
Task<ApiResponse<User>> GetUser(string user);

//Calling the API
var response = await gitHubApi.GetUser("octocat");

//Determining if a success status code was received and there wasn't any other error
//(for example, during content deserialization)
if(response.IsSuccessful)
{
    //YAY! Do the thing...
}

if (response.IsReceived)
{
    //Getting the status code (returns a value from the System.Net.HttpStatusCode enumeration)
    var httpStatus = response.StatusCode;

    //Retrieving a well-known header value (e.g. "Server" header)
    var serverHeaderValue = response.Headers.Server != null ? response.Headers.Server.ToString() : string.Empty;

    //Retrieving a custom header value
    var customHeaderValue = string.Join(',', response.Headers.GetValues("A-Custom-Header"));

    //Looping through all the headers
    foreach(var header in response.Headers)
    {
        var headerName = header.Key;
        var headerValue = string.Join(',', header.Value);
    }

    //Finally, retrieving the content in the response body as a strongly-typed object
    var user = response.Content;
}
```

A successful response does **not** imply a response body. Per [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110.html) a
204 (No Content), a 304 (Not Modified), a response to `HEAD`, or any 2xx with a zero-length or literal `null` body
deserializes to `Content == null` *without* an error — so `IsSuccessful` / `IsSuccessStatusCode` being `true` tells you
the request succeeded, not that `Content` is present. Use one of the content-presence members below when you need the
deserialized body:

* `HasContent` — the covariance-safe way to null-check `Content` on its own; when `true` the compiler knows `Content` is
  non-null.
* `IsSuccessfulWithContent` — a single check that combines success **and** content presence (`IsSuccessful && Content is
  not null`); when `true` the compiler knows `Content` is non-null.

```csharp
if (response.HasContent)
{
    // response.Content is non-null here
    Process(response.Content);
}

// Or, when you want "succeeded and has a body" in one narrowing check:
if (response.IsSuccessfulWithContent)
{
    // response.Content is non-null here
    Process(response.Content);
}
```

`EnsureSuccessStatusCodeAsync()` and `EnsureSuccessfulAsync()` are available directly on `IApiResponse<T>` (not just the
concrete `ApiResponse<T>`), throwing the captured `ApiException` / `ApiRequestException` when the request was not
successful:

```csharp
IApiResponse<User> response = await gitHubApi.GetUser("octocat");
await response.EnsureSuccessStatusCodeAsync();
```

#### Do I need to dispose the response?

`ApiResponse<T>` (and `IApiResponse` / `IApiResponse<T>`) implement `IDisposable`, but for the common case you do **not**
need to dispose them and you are **not** leaking sockets. For every return type *except* the streaming ones below, Refit
buffers the body into memory, returns the connection to the pool, and **disposes the underlying `HttpResponseMessage`
before handing the result back to you**. Calling `Dispose()` (or a `using`) on those responses is a harmless, idempotent
no-op — fine to keep to satisfy analyzers, but not required.

Disposal genuinely matters only when you ask Refit for the **live** response, where the body is not buffered and you own
the connection until you dispose it:

* `HttpResponseMessage`, `HttpContent`, `Stream`
* `ApiResponse<HttpResponseMessage>`, `ApiResponse<HttpContent>`, `ApiResponse<Stream>`

```csharp
// Streaming: you own the response — dispose it (e.g. with `using`)
[Get("/files/{id}")]
Task<ApiResponse<Stream>> DownloadAsync(string id);

using var response = await api.DownloadAsync("42");
await using var stream = response.Content; // released when the response is disposed
```

The rule is decided in `RestMethodInfoInternal.DetermineIfResponseMustBeDisposed`: those three types (and their
`ApiResponse<>` wrappers) are the only ones Refit does not auto-dispose for you.

### Using generic interfaces

When using something like ASP.NET Web API, it's a fairly common pattern to have a whole stack of CRUD REST services.
Refit now supports these, allowing you to define a single API interface with a generic type:

```csharp
public interface IReallyExcitingCrudApi<T, in TKey> where T : class
{
    [Post("")]
    Task<T> Create([Body] T payload);

    [Get("")]
    Task<List<T>> ReadAll();

    [Get("/{key}")]
    Task<T> ReadOne(TKey key);

    [Put("/{key}")]
    Task Update(TKey key, [Body]T payload);

    [Delete("/{key}")]
    Task Delete(TKey key);
}
```

Which can be used like this:

```csharp
// The "/users" part here is kind of important if you want it to work for more
// than one type (unless you have a different domain for each type)
var api = RestService.For<IReallyExcitingCrudApi<User, string>>("http://api.example.com/users");
```

### Interface inheritance

When multiple services that need to be kept separate share a number of APIs, it is possible to leverage interface
inheritance to avoid having to define the same Refit methods multiple times in different services:

```csharp
public interface IBaseService
{
    [Get("/resources")]
    Task<Resource> GetResource(string id);
}

public interface IDerivedServiceA : IBaseService
{
    [Delete("/resources")]
    Task DeleteResource(string id);
}

public interface IDerivedServiceB : IBaseService
{
    [Post("/resources")]
    Task<string> AddResource([Body] Resource resource);
}
```

In this example, the `IDerivedServiceA` interface will expose both the `GetResource` and `DeleteResource` APIs, while
`IDerivedServiceB` will expose `GetResource` and `AddResource`.

#### Headers inheritance

When using inheritance, existing header attributes will be passed along as well, and the inner-most ones will have
precedence:

```csharp
[Headers("User-Agent: AAA")]
public interface IAmInterfaceA
{
    [Get("/get?result=Ping")]
    Task<string> Ping();
}

[Headers("User-Agent: BBB")]
public interface IAmInterfaceB : IAmInterfaceA
{
    [Get("/get?result=Pang")]
    [Headers("User-Agent: PANG")]
    Task<string> Pang();

    [Get("/get?result=Foo")]
    Task<string> Foo();
}
```

Here, `IAmInterfaceB.Pang()` will use `PANG` as its user agent, while `IAmInterfaceB.Foo` and `IAmInterfaceB.Ping` will
use `BBB`.
Note that if `IAmInterfaceB` didn't have a header attribute, `Foo` would then use the `AAA` value inherited from
`IAmInterfaceA`.
If an interface is inheriting more than one interface, the order of precedence is the same as the one in which the
inherited interfaces are declared:

```csharp
public interface IAmInterfaceC : IAmInterfaceA, IAmInterfaceB
{
    [Get("/get?result=Foo")]
    Task<string> Foo();
}
```

Here `IAmInterfaceC.Foo` would use the header attribute inherited from `IAmInterfaceA`, if present, or the one inherited
from `IAmInterfaceB`, and so on for all the declared interfaces.

### Default Interface Methods

Starting with C# 8.0, default interface methods (a.k.a. DIMs) can be defined on interfaces. Refit interfaces can provide
additional logic using DIMs, optionally combined with private and/or static helper methods:

```csharp
public interface IApiClient
{
    // implemented by Refit but not exposed publicly
    [Get("/get")]
    internal Task<string> GetInternal();
    // Publicly available with added logic applied to the result from the API call
    public async Task<string> Get()
        => FormatResponse(await GetInternal());
    private static String FormatResponse(string response)
        => $"The response is: {response}";
}
```

The type generated by Refit will implement the method `IApiClient.GetInternal`. If additional logic is required
immediately before or after its invocation, it shouldn't be exposed directly and can thus be hidden from consumers by
being marked as `internal`.
The default interface method `IApiClient.Get` will be inherited by all types implementing `IApiClient`, including - of
course - the type generated by Refit.
Consumers of the `IApiClient` will call the public `Get` method and profit from the additional logic provided in its
implementation (optionally, in this case, with the help of the private static helper `FormatResponse`).
To support runtimes without DIM-support (.NET Core 2.x and below or .NET Standard 2.0 and below), two additional types
would be required for the same solution.

```csharp
internal interface IApiClientInternal
{
    [Get("/get")]
    Task<string> Get();
}
public interface IApiClient
{
    public Task<string> Get();
}
internal class ApiClient : IApiClient
{
    private readonly IApiClientInternal client;
    public ApiClient(IApiClientInternal client) => this.client = client;
    public async Task<string> Get()
        => FormatResponse(await client.Get());
    private static String FormatResponse(string response)
        => $"The response is: {response}";
}
```

### Using HttpClientFactory

Refit has first class support for `IHttpClientFactory`. Add a reference to `Refit.HttpClientFactory`
and call
the provided extension method in your `ConfigureServices` method to configure your Refit interface:

```csharp
services.AddRefitClient<IWebApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
        // Add additional IHttpClientBuilder chained methods as required here:
        // .AddHttpMessageHandler<MyHandler>()
        // .SetHandlerLifetime(TimeSpan.FromMinutes(2));
```

Optionally, a `RefitSettings` object can be included:

```csharp
var settings = new RefitSettings();
// Configure refit settings here

services.AddRefitClient<IWebApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
        // Add additional IHttpClientBuilder chained methods as required here:
        // .AddHttpMessageHandler<MyHandler>()
        // .SetHandlerLifetime(TimeSpan.FromMinutes(2));

// or injected from the container
services.AddRefitClient<IWebApi>(provider => new RefitSettings() { /* configure settings */ })
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
        // Add additional IHttpClientBuilder chained methods as required here:
        // .AddHttpMessageHandler<MyHandler>()
        // .SetHandlerLifetime(TimeSpan.FromMinutes(2));

```

Note that some of the properties of `RefitSettings` will be ignored because the `HttpClient` and `HttpClientHandlers`
will be managed by the `HttpClientFactory` instead of Refit.

Refit registers each client with `IHttpClientFactory` under a deterministic name. You can compute that same name with
`UniqueName.ForType<T>()`, for example to configure the underlying named `HttpClient` directly:

```csharp
services.AddHttpClient(UniqueName.ForType<IWebApi>())
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
```

You can then get the api interface using constructor injection:

```csharp
public class HomeController : Controller
{
    public HomeController(IWebApi webApi)
    {
        _webApi = webApi;
    }

    private readonly IWebApi _webApi;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var thing = await _webApi.GetSomethingWeNeed(cancellationToken);
        return View(thing);
    }
}
```

### Providing a custom HttpClient

You can supply a custom `HttpClient` instance by simply passing it as a parameter to the `RestService.For<T>` method:

```csharp
RestService.For<ISomeApi>(new HttpClient()
{
    BaseAddress = new Uri("https://www.someapi.com/api/")
});
```

However, when supplying a custom `HttpClient` instance, `HttpMessageHandlerFactory` will not be used because you already
control the handler pipeline.

`AuthorizationHeaderValueGetter` does still work with `RestService.For<T>(httpClient, settings)` when the request
includes an `Authorization` header placeholder (for example `[Headers("Authorization: Bearer")]`).

If you still want to be able to configure the `HtttpClient` instance that `Refit` provides while still making use of the
above settings, simply expose the `HttpClient` on the API interface:

```csharp
interface ISomeApi
{
    // This will automagically be populated by Refit if the property exists
    HttpClient Client { get; }

    [Headers("Authorization: Bearer")]
    [Get("/endpoint")]
    Task<string> SomeApiEndpoint();
}
```

Then, after creating the REST service, you can set any `HttpClient` property you want, e.g. `Timeout`:

```csharp
SomeApi = RestService.For<ISomeApi>("https://www.someapi.com/api/", new RefitSettings()
{
    AuthorizationHeaderValueGetter = (rq, ct) => GetTokenAsync()
});

SomeApi.Client.Timeout = timeout;
```

### Native AoT / trimming guidance

Refit's recommended **source-generator-first** setup for Native AoT and trimmed applications is:

1. Use normal Refit interfaces so the Refit source generator produces the client implementation at build time.
2. Use `RestService.ForGenerated<T>(...)` when the application should require a source-generated client at runtime.
   Otherwise, prefer `RestService.For<T>(...)` over reflection-heavy manual patterns around `Type` where possible.
3. Supply source-generated `System.Text.Json` metadata for your DTOs.

For the default `SystemTextJsonContentSerializer` on .NET 8+, Refit prefers `JsonTypeInfo` metadata from your configured
`JsonSerializerOptions` when it is available. That means Native AoT apps can improve compatibility by supplying
source-generated metadata through a `JsonSerializerContext` or `TypeInfoResolver` on the serializer options they pass
into `SystemTextJsonContentSerializer`.

```csharp
[JsonSerializable(typeof(Todo))]
public partial class TodoJsonContext : JsonSerializerContext
{
}

var settings = new RefitSettings(
    new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = TodoJsonContext.Default
        }
    )
);

var api = RestService.For<ITodoApi>("https://api.example.com", settings);
```

#### Generated-only client registration with `IHttpClientFactory`

`AddRefitClient<T>` is annotated with `RequiresUnreferencedCode` because it can fall back to the reflection-based
registration path. For Native AoT or trimmed applications that rely on the source generator, use
`AddRefitGeneratedClient<T>` (from `Refit.HttpClientFactory`) instead. It registers the client through
`IHttpClientFactory` and `RestService.ForGenerated<T>`, so there is no reflection fallback and no
`RequiresUnreferencedCode` warning, while the usual `IHttpClientBuilder` configuration (base address, message handlers,
resilience pipelines) still applies:

```csharp
services.AddRefitGeneratedClient<IWebApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));

// optionally with settings, a settings factory resolved from the container, or a custom HttpClient name
services.AddRefitGeneratedClient<IWebApi>(settings);
services.AddRefitGeneratedClient<IWebApi>(provider => new RefitSettings { /* configure settings */ });
services.AddRefitGeneratedClient<IWebApi>(settings, "my-named-client");
```

If no source-generated client exists for the interface, resolving it throws an `InvalidOperationException` that points
you back to the generator output - the registration never silently falls back to reflection.

If a generated Refit client cannot be found at runtime, Refit now explicitly points you back to the source
generator/build output and recommends generated clients plus source-generated `System.Text.Json` metadata for Native AoT
scenarios.

Refit also ships analyzers for newer Roslyn toolchains, including a Roslyn 5.0 build for newer Visual Studio versions.

### Handling exceptions

Refit has different exception handling behavior depending on if your Refit interface methods return `Task<T>` or if they
return `Task<IApiResponse>`, `Task<IApiResponse<T>>`, or `Task<ApiResponse<T>>`.

#### <a id="when-returning-taskapiresponset"></a>When returning `Task<IApiResponse>`, `Task<IApiResponse<T>>`, or
`Task<ApiResponse<T>>`

Refit traps any `HttpRequestException` or `TaskCanceledException` raised by the `HttpClient` in an
`ApiRequestException`.
Refit also traps any `ApiException` raised by the `ExceptionFactory` when processing the response, and any errors that
occur when attempting to deserialize the response to `ApiResponse<T>`.
In both cases, it will populate the exception into the `Error` property on `ApiResponse<T>` without throwing the
exception.

You can then decide what to do like so:

```csharp
var response = await _myRefitClient.GetSomeStuff();
if(response.IsSuccessful)
{
   //do your thing
}
else
{
    // If you want to distinguish between request and response errors
    if (response.HasRequestError(out var requestError))
        _logger.LogError(requestError, "An error occurred while sending the request.");
    else if (response.HasResponseError(out var responseError))
        _logger.LogError(responseError, responseError.Content);

    // Or just log the error directly
    _logger.LogError(response.Error, "An error occurred while calling the API.");
}
```

> [!NOTE]
> The `IsSuccessful` property checks whether the response status code is in the range 200-299 and there wasn't any other
> error (for example, during content deserialization). If you just want to check the HTTP response status code, you can
> use the `IsSuccessStatusCode` property. Neither property implies a non-null `Content` — a successful response can have
> no body (e.g. 204 No Content or a literal `null` body). Use `HasContent` or `IsSuccessfulWithContent` when you need the
> deserialized `Content` to be present.

#### When returning `Task<T>`

Refit throws any exception raised by the `HttpClient` and wraps it in an `ApiRequestException`.
It also throws any `ApiException` raised by the `ExceptionFactory` when processing the response and any errors that
occur when attempting to deserialize the response to `Task<T>`.

```csharp
// ...
try
{
   var result = await awesomeApi.GetFooAsync("bar");
}
catch (ApiRequestException exception)
{
    //exception handling for when a response was not received from the server
}
catch (ApiException exception)
{
   //exception handling for when a response was received from the server
}
// Or to not distinguish between request/response exceptions
catch (ApiExceptionBase exception)
{
   //exception handling for when an error occurs during the request/response
}
// ...
```

Refit can also throw `ValidationApiException` instead which in addition to the information present on `ApiException`
also contains `ProblemDetails` when the service implements the [RFC 7807](https://tools.ietf.org/html/rfc7807)
specification for problem details and the response content type is `application/problem+json`

For specific information on the problem details of the validation exception, simply catch `ValidationApiException`:

```csharp
// ...
try
{
   var result = await awesomeApi.GetFooAsync("bar");
}
catch (ValidationApiException validationException)
{
   // handle validation here by using validationException.Content,
   // which is type of ProblemDetails according to RFC 7807

   // If the response contains additional properties on the problem details,
   // they will be added to the validationException.Content.Extensions collection.
}
catch (ApiException exception)
{
   // other exception handling
}
// ...
```

#### Inspecting the error body synchronously

`ApiException` exposes `GetContentAsAsync<T>()` to deserialize the error body, but it cannot be awaited inside an
exception filter (`catch ... when ...`), which the CLR requires to be synchronous. Because the body is already
buffered into the `Content` string, you can deserialize it synchronously with `GetContentAs<T>()` or the
non-throwing `TryGetContentAs<T>(out T? content)`. This lets you route to a specific handler only when the error
body matches a well-known shape, and fall through to a generic handler otherwise:

```csharp
try
{
    var result = await awesomeApi.GetFooAsync("bar");
}
catch (ApiException exception) when (exception.TryGetContentAs<Error>(out var error))
{
    // handle the strongly-typed `error` here
}
catch (ApiException exception)
{
    // generic handling when the body was missing or not an `Error`
}
```

`TryGetContentAs<T>` returns `false` (and never throws) when there is no content, the content does not deserialize,
or the configured serializer cannot deserialize synchronously. `GetContentAs<T>` returns `default` when there is no
content and throws `NotSupportedException` when the configured `IHttpContentSerializer` does not implement
`ISynchronousContentDeserializer`. The built-in `SystemTextJsonContentSerializer`, `NewtonsoftJsonContentSerializer`,
and `XmlContentSerializer` all implement it; a custom serializer can opt in by implementing
`ISynchronousContentDeserializer`.

#### Reading the request body that was sent

By default `HttpClient` disposes the request content once a request is sent, so the body cannot be read back from
`ApiException.RequestMessage.Content`. Set `RefitSettings.CaptureRequestContent = true` to buffer the request body
into a string before sending and expose it on `ApiExceptionBase.RequestContent`:

```csharp
var settings = new RefitSettings { CaptureRequestContent = true };
var awesomeApi = RestService.For<IAwesomeApi>("https://api.example.com", settings);

try
{
    await awesomeApi.PostFooAsync(payload);
}
catch (ApiException exception) when (exception.HasRequestContent)
{
    // exception.RequestContent holds exactly what Refit sent on the wire
    logger.LogError("Failed request body: {Body}", exception.RequestContent);
}
```

`CaptureRequestContent` defaults to `false` because it buffers the entire body into memory; avoid it for large or
streamed uploads. When it is disabled, `HasRequestContent` is `false` and `RequestContent` is `null`.

#### Providing a custom `ExceptionFactory`

You can also override default exceptions behavior that are raised by the `ExceptionFactory` when processing the result
by providing a custom exception factory in `RefitSettings`. For example, you can suppress all `ApiException`s with the
following:

```csharp
var nullTask = Task.FromResult<Exception>(null);

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        ExceptionFactory = httpResponse => nullTask;
    });
```

For exceptions raised when attempting to deserialize the response use DeserializationExceptionFactory described bellow.

#### Providing a custom `DeserializationExceptionFactory`

You can override default deserialization exceptions behavior that are raised by the `DeserializationExceptionFactory`
when processing the result by providing a custom exception factory in `RefitSettings`. For example, you can suppress all
deserialization exceptions with the following:

```csharp
var nullTask = Task.FromResult<Exception>(null);

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings {
        DeserializationExceptionFactory = (httpResponse, exception) => nullTask;
    });
```

#### Providing a custom `TransportExceptionFactory`

You can control the exception that is surfaced when `HttpClient.SendAsync` throws a transport-level error (for example,
`HttpRequestException`, `SocketException`, or `TaskCanceledException`) by setting `TransportExceptionFactory` on
`RefitSettings`. The factory receives the `HttpRequestMessage` that was being sent, the raw exception, and the
`CancellationToken` that was passed to the call, and returns the exception that Refit will ultimately throw
(non-`ApiResponse` path) or store in `IApiResponse.Error` (`ApiResponse` path).

By default, Refit wraps the transport exception in an `ApiRequestException` that carries the full request context,
except when the exception is an `OperationCanceledException` and the `CancellationToken` is already cancelled — in
that case the original exception is returned unchanged. You can replace that behavior entirely, for example to
re-throw the original exception unchanged in all cases:

```csharp
var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com",
    new RefitSettings
    {
        TransportExceptionFactory = (request, ex, ct) => ex
    });
```

Because `RefitSettings` is captured in the closure, you can also reconstruct the default behavior with your own
adjustments:

```csharp
var settings = new RefitSettings();

settings.TransportExceptionFactory = (request, ex, ct) =>
{
    // Pass cancellation through so ApiResponse callers still throw rather
    // than capturing a cancelled request as a soft error.
    if (ex is OperationCanceledException && ct.IsCancellationRequested)
        return ex;

    // Wrap everything else in ApiRequestException, just like the default.
    return new ApiRequestException(request, request.Method, settings, ex);
};

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com", settings);
```

#### `ApiException` deconstruction with Serilog

For users of [Serilog](https://serilog.net), you can enrich the logging of `ApiException` using the
[Serilog.Exceptions.Refit](https://www.nuget.org/packages/Serilog.Exceptions.Refit) NuGet package. Details of how to
integrate this package into your applications can be
found [here](https://github.com/RehanSaeed/Serilog.Exceptions#serilogexceptionsrefit).

### Testing your Refit clients

The first-party [`Refit.Testing`](https://www.nuget.org/packages/Refit.Testing) package lets you test the Refit
clients your app depends on without a mocking library or a live server. You describe the calls you expect as a
**route table** — each entry pairs a `Route` (which request to match) with a `Reply` (what to send back) — and point
a real Refit client at it:

```csharp
using Refit.Testing;

var http = new StubHttp
{
    { Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")) },
    { Route.Post("/users"),     Reply.Status(HttpStatusCode.Created) },
};

var api = http.CreateClient<IGitHubApi>("https://api.github.com");

var user = await api.GetUser(7);

// assert the body your client sent, as a typed object:
var sent = await http.LastRequestBodyAsync<NewUser>();
await http.VerifyAllCalledAsync();
```

Because the handler is Refit-aware, route templates mirror your `[Get("/users/{id}")]` attributes, `Reply.With<T>`
serializes with the client's own serializer (no hand-written JSON), and you can read the sent request body back as a
typed object. It also ships `NetworkBehavior` for seeded latency/fault injection and `StubApiResponse<T>` for
unit-testing code that consumes `IApiResponse<T>` directly.

See the [testing guide](docs/testing.md) for the full walkthrough.
