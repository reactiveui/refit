![Refit](https://raw.githubusercontent.com/reactiveui/refit/main/images/refit-logo.svg)

# Refit

[![Build](https://github.com/reactiveui/refit/actions/workflows/ci-build.yml/badge.svg)](https://github.com/reactiveui/refit/actions/workflows/ci-build.yml)
[![NuGet](https://img.shields.io/nuget/v/Refit.svg)](https://www.nuget.org/packages/Refit/)

Refit turns a C# interface into an HTTP client. Attributes describe the request method,
URL, headers and body. The source generator builds its implementation when your app compiles.
Your app keeps control of `HttpClient`, cancellation and serialization.

Start with the [Refit documentation](https://reactiveui.net/documentation/refit/).
It explains the concepts, walks through a first request, and links each topic to runnable
[source examples](https://github.com/reactiveui/refit/tree/main/src/examples/Documentation).
Examples use .NET 10 and C# 14 unless they state another target.
Website code blocks can show a subset of a complete example and omit its test setup.

## Packages

| Package | Purpose |
| --- | --- |
| [Refit](https://www.nuget.org/packages/Refit/) | Generated HTTP clients, request attributes, serializers and response types. Includes the generator, interface analyzers and code fixes. |
| [Refit.HttpClientFactory](https://www.nuget.org/packages/Refit.HttpClientFactory/) | Register clients with dependency injection and `IHttpClientFactory`, including generated-only and keyed registrations. |
| [Refit.Testing](https://www.nuget.org/packages/Refit.Testing/) | Match outgoing requests, supply local replies, verify calls and simulate network faults. |
| [Refit.Newtonsoft.Json](https://www.nuget.org/packages/Refit.Newtonsoft.Json/) | Serialize content with Newtonsoft.Json. |
| [Refit.Xml](https://www.nuget.org/packages/Refit.Xml/) | Serialize XML content. |
| [Refit.Reflection](https://www.nuget.org/packages/Refit.Reflection/) | Optional request building through runtime reflection. Use generated clients for trimmed and Native AOT apps. |

System.Text.Json with generated metadata is the preferred JSON setup.
See [JSON configuration](https://www.reactiveui.net/documentation/refit/serialization/json/)
and [AOT guidance](https://www.reactiveui.net/documentation/refit/aot/).

## What you can build

| Surface | Documentation |
| --- | --- |
| HTTP methods, route placeholders and request inspection | [Routes](https://www.reactiveui.net/documentation/refit/requests/routes/) |
| Scalar, object and collection queries; formatting and converters | [Queries](https://www.reactiveui.net/documentation/refit/requests/queries/) |
| Static and dynamic headers, authorization and request state | [Headers](https://www.reactiveui.net/documentation/refit/requests/headers/) and [request context](https://www.reactiveui.net/documentation/refit/requests/request-context/) |
| Serialized bodies, forms, buffering and request compression | [Bodies](https://www.reactiveui.net/documentation/refit/requests/bodies/) |
| `Task<T>`, `ValueTask<T>`, `IObservable<T>` and response wrappers | [Return types](https://www.reactiveui.net/documentation/refit/results/return-types/) and [responses](https://www.reactiveui.net/documentation/refit/results/responses/) |
| Streamed JSON arrays, JSON Lines and server-sent events | [Streaming](https://www.reactiveui.net/documentation/refit/results/streaming/) |
| HTTP, transport and deserialization failures | [Errors](https://www.reactiveui.net/documentation/refit/results/errors/) |
| Local replies, request verification and simulated faults | [Testing](https://www.reactiveui.net/documentation/refit/testing/) |

See [why use Refit](https://www.reactiveui.net/documentation/refit/why-refit/) for equivalent
manual HTTP code and trade-offs. The website records known behavior discrepancies beside
the affected APIs.

## Public API map

Use the [full API reference](https://www.reactiveui.net/documentation/refit/api-reference/)
to find types, overloads, parameters and return values across all topics on one page.

| Public method group | Brief description | Documentation |
| --- | --- | --- |
| HTTP verb and route attributes | Declare the HTTP method, route template, base-path prefix and URL resolution behavior. | [Routes](https://www.reactiveui.net/documentation/refit/requests/routes/) |
| Query attributes and formatters | Add scalar, object, collection, flag, encoded, formatted and converted query values. | [Queries](https://www.reactiveui.net/documentation/refit/requests/queries/), [formatters](https://www.reactiveui.net/documentation/refit/requests/query-formatters/) and [converters](https://www.reactiveui.net/documentation/refit/requests/query-converters/) |
| Headers, authorization and request context | Add shared or per-call headers, obtain tokens and attach local handler state. | [Headers](https://www.reactiveui.net/documentation/refit/requests/headers/) and [request context](https://www.reactiveui.net/documentation/refit/requests/request-context/) |
| Bodies, forms, multipart and compression | Serialize request bodies, send forms, files or streams, choose buffering and apply request compression. | [Bodies](https://www.reactiveui.net/documentation/refit/requests/bodies/) and [multipart](https://www.reactiveui.net/documentation/refit/requests/multipart/) |
| Client creation and request builders | Create generated or reflection clients, create owned `HttpClient` instances and supply request builders. | [Client creation](https://www.reactiveui.net/documentation/refit/clients/creation/) and [request builders](https://www.reactiveui.net/documentation/refit/clients/request-builders/) |
| Settings and request metadata | Configure serializers, formatters, exception factories, transport behavior, request options, timeouts, versions and URL resolution. | [Settings](https://www.reactiveui.net/documentation/refit/clients/settings/), [metadata](https://www.reactiveui.net/documentation/refit/advanced/method-metadata/) and [helpers](https://www.reactiveui.net/documentation/refit/advanced/request-helpers/) |
| Return types, responses and adapters | Choose `Task`, `ValueTask`, `IObservable<T>`, `IAsyncEnumerable<T>`, response wrappers and custom return adapters. | [Return types](https://www.reactiveui.net/documentation/refit/results/return-types/), [responses](https://www.reactiveui.net/documentation/refit/results/responses/), [streaming](https://www.reactiveui.net/documentation/refit/results/streaming/) and [adapters](https://www.reactiveui.net/documentation/refit/results/adapters/) |
| Errors and problem details | Read typed error content, create or transform API and transport exceptions, enforce success and redact sensitive details. | [Errors](https://www.reactiveui.net/documentation/refit/results/errors/) |
| Content serializers | Use System.Text.Json, JSON Lines, synchronous or streaming serializer capabilities and custom serializers. | [JSON](https://www.reactiveui.net/documentation/refit/serialization/json/) and [content](https://www.reactiveui.net/documentation/refit/serialization/content/) |
| Alternate serializers | Configure Newtonsoft.Json and XML serializers with their settings. | [Newtonsoft.Json](https://www.reactiveui.net/documentation/refit/serialization/newtonsoft-json/) and [XML](https://www.reactiveui.net/documentation/refit/serialization/xml/) |
| HTTP client factory and dependency injection | Register generated, reflection or keyed clients, settings holders and authorization providers. | [Dependency injection](https://www.reactiveui.net/documentation/refit/clients/dependency-injection/) |
| Testing routes, replies, faults and verification | Match requests, create replies, inject failures or delays, inspect bodies and verify calls. | [Testing](https://www.reactiveui.net/documentation/refit/testing/) |
| Advanced generated request helpers | Extend or inspect request construction through query builders, parameter providers and helper contracts. | [Query builder](https://www.reactiveui.net/documentation/refit/advanced/query-builder/) and [request helpers](https://www.reactiveui.net/documentation/refit/advanced/request-helpers/) |

## Build requirements and targets

The source generator requires Roslyn 4.8 or newer and `PackageReference`.
.NET SDK 8.0.100 and Visual Studio 2022 17.8 provide that compiler baseline.
These build requirements are separate from the application's target framework.

The source projects target .NET 8, 9, 10 and 11, plus .NET Framework 4.6.2, 4.7, 4.7.1,
4.7.2, 4.8 and 4.8.1. Availability of individual framework features can differ.
Read the [AOT guide](https://www.reactiveui.net/documentation/refit/aot/) before choosing
reflection-dependent APIs for a trimmed or native app.

## Examples and changes

Build every complete documentation example from `src`:

```bash
dotnet build Refit.slnx -c Release
```

Each website page links to its own project under `examples/Documentation/Pages`. Run a page project,
such as `examples/Documentation/Pages/requests-routes/requests-routes.csproj`, to check its local
reply assertions without contacting a live service. The [documentation examples README](https://github.com/reactiveui/refit/blob/main/src/examples/Documentation/README.md)
lists the source layout and independent projects.

See [breaking changes](https://github.com/reactiveui/refit/blob/main/docs/breaking-changes.md),
[GitHub releases](https://github.com/reactiveui/refit/releases), and
[contributing](https://www.reactiveui.net/contribute/).

## Sponsors

Refit is supported by [JetBrains](https://www.jetbrains.com/) and by
[Anthropic](https://www.anthropic.com/) through [Claude](https://claude.com/).
[OpenAI](https://openai.com/) supports Refit's maintainers with [Codex](https://openai.com/codex/)
through [Codex for Open Source](https://developers.openai.com/community/codex-for-oss).

[![JetBrains](https://raw.githubusercontent.com/reactiveui/refit/main/images/jetbrains.svg)](https://www.jetbrains.com/)
[![Claude by Anthropic](https://raw.githubusercontent.com/reactiveui/refit/main/images/claude.svg)](https://claude.com/)
[![OpenAI](https://raw.githubusercontent.com/reactiveui/refit/main/images/openai.svg)](https://openai.com/codex/)

JetBrains, Claude, Anthropic, OpenAI and Codex names and logos are trademarks of their respective owners.

Refit is licensed under the [MIT license](https://github.com/reactiveui/refit/blob/main/LICENSE).
