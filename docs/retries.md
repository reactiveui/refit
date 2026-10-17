# HTTP-aware retries

`HttpRetryHandler` is an opt-in `DelegatingHandler` for Refit and ordinary `HttpClient` callers on
.NET 8 and later. It needs no source-generator changes or resilience package.

```csharp
var handler = new HttpRetryHandler(new HttpRetryOptions
{
    MaxRetries = 3,
    DelayProvider = RetryDelayProviders.GitHub,
})
{
    InnerHandler = new HttpClientHandler(),
};
using var client = new HttpClient(handler)
{
    BaseAddress = new Uri("https://api.github.com"),
};
var api = RestService.ForGenerated<IGitHubApi>(client);
```

With `Refit.HttpClientFactory`, add a fresh handler to the existing client registration:

```csharp
services.AddRefitGeneratedClient<IGitHubApi>()
    .ConfigureHttpClient(static client => client.BaseAddress = new Uri("https://api.github.com"))
    .AddHttpMessageHandler(static () => new HttpRetryHandler(new HttpRetryOptions
    {
        DelayProvider = RetryDelayProviders.GitHub,
    }));
```

You can also return a complete handler chain from `RefitSettings.HttpMessageHandlerFactory`.
An authentication handler placed **inside** the retry handler runs for each attempt, so it can
refresh credentials. A handler placed outside runs once per logical call.

## Defaults and ownership

- Three extra attempts after the initial send. `MaxRetries = 0` bypasses retries and buffering.
- Retry HTTP 408, 429, 500, 502, 503 and 504, plus `HttpRequestException`.
- A 403 retries only when the configured delay reader returns a valid retry hint. An ordinary
  authorization failure does not retry. A successful response never retries by default.
- GET, HEAD, OPTIONS and TRACE may retry. Other methods require `RetryUnsafeMethods = true`.
  Opt in only when replay is safe for the endpoint, for example with a server-supported
  idempotency key. Cancellation and exceptions other than `HttpRequestException` never retry.
- Missing or malformed hints use exponential backoff starting at one second, capped at
  30 seconds, with full jitter. Disable jitter with `UseJitter = false`.
- Valid server hints replace fallback backoff and are not shortened by jitter. A negative
  custom interval or a hint above `MaxDelay` stops retries and returns the response.
- Each attempt gets a fresh request, copied headers, HTTP version, options and content headers.
  Bodies are serialized once into a bounded buffer, one MiB by default. Oversized bodies fail
  before sending. Retries are not a suitable choice for unbounded streaming uploads.
- Failed responses and cloned requests are disposed by the handler. The final response and
  original request remain caller-owned. Response content is not buffered or inspected.

Options are copied when constructing the handler. Custom callbacks must be thread-safe.
`TimeProvider` supplies both the clock for absolute hints and the timers for backoff.
Cancellation covers buffering, sends and waits. `HttpClient.Timeout` still bounds the logical
call, including retry waits.

## Provider delay readers

| Reader | Headers, in priority order |
| --- | --- |
| `RetryAfter` (default) | Standard `Retry-After`, integer seconds or HTTP date. |
| `Azure` | `x-ms-retry-after-ms`, `retry-after-ms` in milliseconds, then `Retry-After`. |
| `Aws` | `x-amz-retry-after` in milliseconds, then `Retry-After`. |
| `GitHub` | `Retry-After`; otherwise `x-ratelimit-reset` in Unix seconds when status is 403/429 and `x-ratelimit-remaining` is zero. |
| `Jira` | `Retry-After`; otherwise `X-RateLimit-Reset` as ISO 8601 on HTTP 429. |

Microsoft Graph and Slack use standard `Retry-After`, so the default reader covers them.
Readers reject ambiguous multiple values, malformed numbers and overflow. Past absolute
timestamps map to zero. An invalid higher-priority header falls through to the next reader.

The AWS reader supports documented headers, not the AWS SDK's complete retry policy.
Providers can also put service-specific hints inside JSON bodies. Microsoft Graph batches
can report throttled operations inside an HTTP 200 response. Those cases need application-level
handling, not replay of the whole HTTP response. A GitHub secondary limit with no timing headers
cannot be distinguished from an ordinary 403 by a header-only adapter.

### Custom headers and classification

```csharp
var options = new HttpRetryOptions
{
    DelayProvider = static (response, now, token) =>
        RetryDelayProviders.ReadHeader(
            response, "X-Retry-Milliseconds", RetryHeaderFormat.Milliseconds, now, token)
        ?? RetryDelayProviders.RetryAfter(response, now, token),
    ShouldRetry = static response =>
        response.StatusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable,
};
```

`RetryHeaderFormat` supports seconds, milliseconds, Unix seconds, standard `Retry-After`
and ISO 8601 dates. `ShouldRetry` replaces default response classification.
`DelayProvider = null` disables header hints. `RetryHttpRequestExceptions = false` disables
transport retries without affecting response retries.

## Deterministic tests

`Refit.Testing` supplies `RetryReply.After` for seconds or dates and `RetryReply.Header`
for provider-specific or deliberately invalid hints. Each match produces a fresh response.
`StubHttp.CreateRetryHandler()` uses the stub's clock. The overload taking explicit options
uses their clock instead.

```csharp
var clock = new FakeTimeProvider();
using var stub = new StubHttp { TimeProvider = clock };
stub.Add(Route.Get("/users/{id}"), RetryReply.After(TimeSpan.FromSeconds(5)));
stub.Add(Route.Get("/users/{id}"), Reply.With(new User(7, "octocat")));

using var client = new HttpClient(stub.CreateRetryHandler())
{
    BaseAddress = new Uri("https://api.test"),
};
var api = RestService.ForGenerated<IUserApi>(client);
var pending = api.GetUser(7);
clock.Advance(TimeSpan.FromSeconds(5));
var user = await pending;
await stub.VerifyAllCalledAsync();
```

`FakeTimeProvider` comes from `Microsoft.Extensions.TimeProvider.Testing`. Advance time only
after the relevant send has reached its wait. See `HttpRetryHandlerTests.Time.cs` for an
observable timer-registration pattern for multi-attempt tests.

## Composing other resilience features

The handler boundary also fits timeouts, concurrency limits and circuit breakers without
changing generated clients. Those strategies are not built into this retry handler.
Place per-attempt timeouts or concurrency limits inside retries. Place a total-call timeout
outside. A circuit breaker's position determines whether it counts individual attempts or
final logical failures. Partition rate limits and breakers by the relevant provider, tenant
or endpoint rather than pausing unrelated requests.

Both `Microsoft.Extensions.Resilience` and `Microsoft.Extensions.Http.Resilience` are built
on Polly. They remain optional integrations; this handler does not depend on either.
Do not stack two retry strategies without calculating their combined attempt count.
For example, three retries in each of two layers can cause sixteen network sends.

Hedging runs parallel attempts and needs stricter replay safeguards. A typed fallback
chooses replacement application data, so it belongs above the HTTP handler.

## Query names and redirect targets

Use `[Query][AliasAs("client_id")] string clientId` for a scalar query alias.
`[Query("client_id")]` sets a nesting delimiter, not the query name.

For a relative redirect `Location`, resolve it against the request URL with
`new Uri(requestUrl, location)`, then pass that absolute URI to an existing `[Url]` parameter
on a method with an empty path. The encoded query stays intact. A route placeholder treats
`?` and `=` as path data and escapes them. No new attribute or generator hook is needed.

## Provider references

- [HTTP Retry-After, RFC 9110 section 10.2.3](https://www.rfc-editor.org/rfc/rfc9110.html#section-10.2.3)
- [Azure Cosmos DB response headers](https://learn.microsoft.com/en-us/rest/api/cosmos-db/common-cosmosdb-rest-response-headers)
- [Azure OpenAI quota guidance](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/quota)
- [Microsoft Graph throttling](https://learn.microsoft.com/en-us/graph/throttling)
- [AWS SDK retry behavior](https://docs.aws.amazon.com/sdkref/latest/guide/feature-retry-behavior.html)
- [GitHub REST rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)
- [Slack Web API rate limits](https://docs.slack.dev/apis/web-api/rate-limits/)
- [Jira Cloud rate limiting](https://developer.atlassian.com/cloud/jira/platform/rate-limiting/)
- [.NET resilience libraries](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
