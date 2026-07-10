# Testing Refit clients with `Refit.Testing`

`Refit.Testing` is a first-party package for testing the Refit clients your app depends on, without a mocking
library and without a live server. You describe the calls you expect as a **route table** — each entry pairs a
`Route` (which request to match) with a `Reply` (what to send back) — and point a real Refit client at it.

Because the library is Refit-aware, it does three things a general-purpose HTTP mock cannot:

- **Route templates** mirror the `[Get("/users/{id}")]` attributes on your interface, so you don't restate URLs.
- **Typed replies** are serialized with the *client's own* serializer, so you never hand-write JSON.
- **Typed request capture** deserializes the body your client sent back into an object you can assert on.

## Contents

- [Installation](#installation)
- [Quick start](#quick-start)
- [Creating a client](#creating-a-client)
- [Routes: matching requests](#routes-matching-requests)
- [Replies: sending responses](#replies-sending-responses)
- [Verifying the calls that were made](#verifying-the-calls-that-were-made)
- [Inspecting the request that was sent](#inspecting-the-request-that-was-sent)
- [Sequenced and reusable routes](#sequenced-and-reusable-routes)
- [Simulating network conditions](#simulating-network-conditions)
- [Testing retries and timeouts](#testing-retries-and-timeouts)
- [Unit-testing code that consumes `IApiResponse<T>`](#unit-testing-code-that-consumes-iapiresponset)
- [API reference](#api-reference)

## Installation

```sh
dotnet add package Refit.Testing
```

The package targets the same frameworks as Refit itself, so it works wherever your tests run.

## Quick start

Given a Refit interface:

```csharp
public interface IGitHubApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);

    [Post("/users")]
    Task<User> CreateUser([Body] NewUser user);
}
```

A test looks like this:

```csharp
using Refit.Testing;

[Test]
public async Task GetsAUser()
{
    var http = new StubHttp
    {
        { Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")) },
    };

    var api = http.CreateClient<IGitHubApi>("https://api.github.com");

    var user = await api.GetUser(7);

    Assert.Equal("octocat", user.Login);
    await http.VerifyAllCalledAsync();
}
```

`Reply.With(...)` serializes the `User` with the same serializer the client uses, and `Route.Get("/users/{id}")`
matches the URL the `[Get("/users/{id}")]` attribute produces — no JSON strings, no full URLs.

## Creating a client

`StubHttp` is an `HttpMessageHandler`. The most direct way to get a client wired to it is `CreateClient<T>`:

```csharp
var api = http.CreateClient<IGitHubApi>("https://api.github.com");
```

This is shorthand for `RestService.For<IGitHubApi>("https://api.github.com", http.ToSettings())`. To keep a custom
serializer or other `RefitSettings`, pass them and they are routed through the handler:

```csharp
var settings = new RefitSettings(new SystemTextJsonContentSerializer(myOptions));
var api = http.CreateClient<IGitHubApi>("https://api.github.com", settings);
```

For trim- or AOT-compiled test hosts, use `CreateGeneratedClient<T>`, which uses the source-generated client
instead of the reflection path (a generated implementation must be registered for the interface):

```csharp
var api = http.CreateGeneratedClient<IGitHubApi>("https://api.github.com");
```

If you build the `HttpClient` yourself (for example to exercise `HttpClientFactory` wiring), `ToSettings()` and
`ToSettings(existing)` return a `RefitSettings` whose handler factory points at the stub.

## Routes: matching requests

Build the common routes with the `Route` factory, one method per verb:

```csharp
Route.Get("/users/{id}")
Route.Post("/users")
Route.Delete("/users/{id}")
Route.Any("/health")                 // matches any HTTP method
Route.For(new HttpMethod("PATCH"), "/users/{id}")
```

The template is matched a path segment at a time:

- `{name}` matches any single non-empty segment (`/users/{id}` matches `/users/7`).
- A **relative** template (`/users/1`) matches the request's absolute path.
- An **absolute** template (`https://api.github.com/users/1`) matches the whole scheme/host/path.
- `"*"` matches any path.

The query string is matched separately (see below), so `Route.Get("/search")` matches `/search?q=refit`.

For finer matching, construct a `RouteMatcher` directly and set only the properties you need:

```csharp
new RouteMatcher
{
    Method = HttpMethod.Get,
    Template = "/search",
    Query = [("q", "refit")],                 // partial: these pairs must be present, others allowed
    Headers = [("X-Trace", "abc")],           // request or content headers
}
```

| Property           | Matches when …                                                                 |
| ------------------ | ------------------------------------------------------------------------------ |
| `Method`           | the HTTP method equals this (`null` matches any method)                         |
| `Template`         | the path matches (see template rules above) — **required**                     |
| `Query`            | every listed query pair is present (extras allowed)                            |
| `ExactQuery`       | the raw query string equals this exactly                                       |
| `ExactQueryParams` | the decoded query pairs are exactly this set (no extras, order-insensitive)    |
| `Headers`          | every listed header is present with the given value                            |
| `Body`             | the request body equals this string exactly                                    |
| `FormData`         | every listed form field is present in the form-encoded body                    |
| `Where`            | the synchronous predicate returns `true`                                       |
| `WhereAsync`       | the asynchronous predicate returns `true` (e.g. reading a streamed body)       |
| `Reusable`         | see [Sequenced and reusable routes](#sequenced-and-reusable-routes)            |

An incoming request that matches no route **throws** rather than returning a canned 404, so a mistyped URL fails
the test loudly instead of surfacing later as a confusing deserialization error.

## Replies: sending responses

Build responses with the `Reply` factory:

```csharp
Reply.With(new User(7, "octocat"))                    // typed body, serialized by the client's serializer, 200
Reply.With(new User(7, "octocat"), HttpStatusCode.Created)
Reply.Json("{\"id\":7}")                              // raw JSON body, 200
Reply.Json("{\"error\":\"nope\"}", HttpStatusCode.BadRequest)
Reply.Text("pong")                                    // text/plain body
Reply.Text("<b>hi</b>", "text/html")
Reply.Status(HttpStatusCode.NoContent)                // bare status, no body
Reply.Content(new ByteArrayContent(bytes))            // explicit HttpContent
Reply.From(request => new HttpResponseMessage(HttpStatusCode.OK) { Content = ... })   // full control
Reply.From(async request => { ... return response; })                                 // async responder
```

`Reply.With<T>` is the idiomatic choice: it serializes the object with the serializer configured on the client, so
the response deserializes back into your model exactly as a real server's would. For the rare case that needs total
control over the `HttpResponseMessage`, use `Reply.From`. As with routes, you can also build a `StubResponse`
directly for uncommon combinations (e.g. a text body with a non-200 status).

## Verifying the calls that were made

Each non-reusable route is **one-shot**: it satisfies exactly one request and is then consumed. `VerifyAllCalled`
asserts that every one was hit:

```csharp
http.VerifyAllCalled();          // throws if any expected route went unmatched
```

When the request under test is fire-and-forget (an observable subscription, a background send), it may not have
completed by the time you verify. `VerifyAllCalledAsync` waits for the outstanding requests to arrive before
asserting, with a default one-second timeout you can override:

```csharp
await http.VerifyAllCalledAsync();
await http.VerifyAllCalledAsync(TimeSpan.FromSeconds(5));
```

## Inspecting the request that was sent

Every request the handler receives is recorded in `Requests`:

```csharp
await api.GetUser(7);

Assert.Equal("/users/7", http.Requests[0].RequestUri!.AbsolutePath);
Assert.Equal(HttpMethod.Get, http.Requests[0].Method);
```

To assert on the **body** your client sent, deserialize it as a typed object — the body is buffered at send time,
so it is available even after the client has disposed the request:

```csharp
await api.CreateUser(new NewUser("mona"));

var sent = await http.LastRequestBodyAsync<NewUser>();
Assert.Equal("mona", sent!.Login);

// or by index, when several requests were made
var first = await http.RequestBodyAsync<NewUser>(0);
```

## Sequenced and reusable routes

Multiple routes for the same endpoint are consumed **in declared order**, so successive calls get successive
responses:

```csharp
var http = new StubHttp
{
    { Route.Get("/status"), Reply.Json("{\"state\":\"pending\"}") },
    { Route.Get("/status"), Reply.Json("{\"state\":\"done\"}") },
};
// first GET /status -> pending, second GET /status -> done
```

Set `Reusable = true` for a background stub that may match any number of requests and is **not** required by
`VerifyAllCalled` — useful for an endpoint that is polled, or a catch-all:

```csharp
var http = new StubHttp
{
    { new RouteMatcher { Template = "*", Reusable = true }, Reply.Status(HttpStatusCode.OK) },
};
```

One-shot routes take priority over reusable ones, so you can special-case a single call while a reusable route
handles the rest.

## Simulating network conditions

`NetworkBehavior` injects deterministic, seeded latency and failures across every matched request — the equivalent
of Retrofit's `NetworkBehavior`. Pass it to the `StubHttp` constructor (or set the `Behavior` property):

```csharp
var behavior = new NetworkBehavior(seed: 1)
{
    Delay = TimeSpan.FromMilliseconds(200),   // base latency per call
    Variance = 0.5,                           // +/- jitter as a fraction of Delay
    FailurePercent = 0.1,                     // chance of a thrown transport failure
    ErrorPercent = 0.2,                       // chance of an HTTP error response
    ErrorStatusCode = HttpStatusCode.InternalServerError,
};

var http = new StubHttp(behavior)
{
    { Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")) },
};
```

Seeding makes runs reproducible: the same seed produces the same sequence of delays and failures, so a flaky-path
test is deterministic. Set `FailureFactory` to control the exception a simulated transport failure throws.

## Testing retries and timeouts

Sequenced one-shot routes are matched in declared order, so a transient fault followed by a success exercises a
retry policy — a Polly handler, or any `DelegatingHandler` — stacked above the stub:

```csharp
var http = new StubHttp
{
    { Route.Get("/users/{id}"), Reply.Status(HttpStatusCode.ServiceUnavailable) },  // first attempt
    { Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")) },               // the retry
};

var settings = new RefitSettings { HttpMessageHandlerFactory = () => new MyRetryHandler(http) };
var api = RestService.For<IUserApi>("https://api.test", settings);

var user = await api.GetUser(7);        // succeeded on the retry
Assert.Equal(2, http.Requests.Count);   // both attempts were made
```

To retry a *transport* fault rather than a status code, have the first reply throw:
`Reply.From(HttpResponseMessage (_) => throw new HttpRequestException("boom"))`.

Timeouts fall out of `NetworkBehavior.Delay`: make the injected latency exceed the client's `Timeout` and the
request is aborted. Refit's transport-exception factory surfaces the resulting cancellation as an
`ApiRequestException` whose `InnerException` is the `TaskCanceledException`:

```csharp
var http = new StubHttp(new NetworkBehavior { Delay = TimeSpan.FromSeconds(10), Variance = 0 })
{
    { Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")) },
};

using var client = new HttpClient(http)
{
    BaseAddress = new Uri("https://api.test"),
    Timeout = TimeSpan.FromMilliseconds(50),
};

await Assert.ThrowsAsync<ApiRequestException>(() => RestService.For<IUserApi>(client).GetUser(7));
```

## Unit-testing code that consumes `IApiResponse<T>`

When the code under test is handed an `IApiResponse<T>` directly (rather than making an HTTP call), use
`StubApiResponse<T>` — a hand-written `IApiResponse<T>` whose members are all `init`-only:

```csharp
IApiResponse<User> response = new StubApiResponse<User>
{
    IsSuccessStatusCode = true,
    StatusCode = HttpStatusCode.OK,
    Content = new User(7, "octocat"),
    HasContent = true,
};

// flows exactly as a real response would:
if (response.IsSuccessfulWithContent)
{
    Use(response.Content); // non-null here
}
```

Prefer `StubHttp` for end-to-end tests; reach for `StubApiResponse<T>` only when a method signature hands your code
an `IApiResponse<T>` to react to.

## API reference

| Type                 | Purpose                                                                       |
| -------------------- | ----------------------------------------------------------------------------- |
| `StubHttp`           | The route-table `HttpMessageHandler`; also creates clients and captures bodies |
| `Route`              | Factory for the common `RouteMatcher` shapes (`Get`, `Post`, …)               |
| `RouteMatcher`       | Declarative request matcher (method, template, query, headers, body, predicates) |
| `Reply`              | Factory for `StubResponse` values (`With<T>`, `Json`, `Status`, `From`, …)    |
| `StubResponse`       | The response returned for a matched route                                     |
| `NetworkBehavior`    | Seeded latency and fault injection                                            |
| `StubApiResponse<T>` | A hand-written `IApiResponse<T>` for unit tests that don't go through HTTP     |

Key `StubHttp` members:

- `CreateClient<T>(hostUrl[, settings])` / `CreateGeneratedClient<T>(hostUrl[, settings])` — build a wired client
- `ToSettings()` / `ToSettings(settings)` — a `RefitSettings` routed through the handler
- `Requests` — every request received, in order
- `LastRequestBodyAsync<T>()` / `RequestBodyAsync<T>(index)` — the sent body as a typed object
- `VerifyAllCalled()` / `VerifyAllCalledAsync([timeout])` — assert every one-shot route was hit
- `Behavior` — the `NetworkBehavior` applied to each matched request
