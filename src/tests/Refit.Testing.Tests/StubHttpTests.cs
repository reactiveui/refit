// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>Unit tests for the <see cref="StubHttp"/> declarative test handler.</summary>
public sealed class StubHttpTests
{
    /// <summary>A reusable request URL used across tests.</summary>
    private const string ThingUrl = "https://api/thing";

    /// <summary>A reusable request URL used by query-matching tests.</summary>
    private const string QueryUrl = "https://api/q";

    /// <summary>A reusable request URL used by body-matching tests.</summary>
    private const string BodyUrl = "https://api/b";

    /// <summary>A reusable request URL used by ordering tests.</summary>
    private const string SeqUrl = "https://api/seq";

    /// <summary>Verifies a method/URL match returns the configured JSON and satisfies verification.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MatchesMethodAndUrlAndReturnsJson()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ThingUrl),
                Reply.Json("{\"ok\":true}")
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, ThingUrl);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("{\"ok\":true}");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies the wrong method does not match and the request throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WrongMethodDoesNotMatch()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ThingUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Post, ThingUrl))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies <see cref="StubHttp.VerifyAllCalled"/> throws when an expectation is unmet.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task VerifyAllCalledThrowsForUnmetExpectation()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://api/never"),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(handler.VerifyAllCalled).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a null method matches any method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullMethodMatchesAnyMethod()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "https://api/any", Reusable = true },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var get = await SendAsync(handler, HttpMethod.Get, "https://api/any");
        var post = await SendAsync(handler, HttpMethod.Post, "https://api/any");

        await Assert.That(get.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(post.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies the wildcard URL matches any request path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WildcardUrlMatchesAnything()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Text("hit")
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, "https://anywhere/at/all");

        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("hit");
    }

    /// <summary>Verifies the URL is matched ignoring the query string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlMatchesIgnoringQuery()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ThingUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, ThingUrl + "?a=1&b=2");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies a partial query matcher requires each pair but allows extras.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PartialQueryMatchesSubset()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = QueryUrl, Query = [("a", "1")] },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, QueryUrl + "?a=1&b=2");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a partial query matcher fails when the pair is absent.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PartialQueryFailsWhenMissing()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = QueryUrl, Query = [("a", "1")] },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Get, QueryUrl + "?a=2"))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies the exact-query string matcher compares the raw, encoded query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExactQueryStringMatchesRawEncoded()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = QueryUrl, ExactQuery = "key%2C=value%2C" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, QueryUrl + "?key%2C=value%2C");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies the exact-query string matcher rejects extra parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExactQueryStringRejectsExtras()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = QueryUrl, ExactQuery = "a=1" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Get, QueryUrl + "?a=1&b=2"))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies the decoded exact-query pair matcher compares complete, decoded pairs.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExactQueryParamsMatchDecodedPairs()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = QueryUrl, ExactQueryParams = [("enums", "k0,k1")] },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, QueryUrl + "?enums=k0%2Ck1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies an empty exact-query matches a request with no query.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyExactQueryMatchesNoQuery()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = QueryUrl, ExactQuery = string.Empty },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, QueryUrl);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies a header matcher requires the header value on the request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeaderMatcherRequiresHeader()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Headers = [("X-Refit", "99")], Reusable = true },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        using var client = new HttpClient(handler);
        using var withHeader = new HttpRequestMessage(HttpMethod.Get, "https://api/h");
        withHeader.Headers.Add("X-Refit", "99");
        var ok = await client.SendAsync(withHeader);

        await Assert.That(ok.StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var without = new HttpRequestMessage(HttpMethod.Get, "https://api/h");
        await Assert.That(async () => _ = await client.SendAsync(without)).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies an exact body matcher compares the request content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyMatcherComparesContent()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = BodyUrl, Body = "raw string" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var response = await SendAsync(handler, HttpMethod.Post, BodyUrl, new StringContent("raw string"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an exact body matcher fails on a mismatched body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyMatcherFailsOnMismatch()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = BodyUrl, Body = "expected" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Post, BodyUrl, new StringContent("actual")))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a form-data matcher checks for decoded form pairs in the body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormDataMatcherChecksPairs()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = "https://api/f", FormData = [("user_name", "bob"), ("pwd", "secret")] },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var form = new FormUrlEncodedContent(
            [new("user_name", "bob"), new("pwd", "secret"), new("extra", "x")]);
        var response = await SendAsync(handler, HttpMethod.Post, "https://api/f", form);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies the <see cref="RouteMatcher.Where"/> predicate gates matching.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WherePredicateGatesMatch()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Where = request => request.Headers.Contains("X-Flag"), Reusable = true },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        using var client = new HttpClient(handler);
        using var flagged = new HttpRequestMessage(HttpMethod.Get, "https://api/w");
        flagged.Headers.Add("X-Flag", "1");

        await Assert.That((await client.SendAsync(flagged)).StatusCode).IsEqualTo(HttpStatusCode.OK);

        using var plain = new HttpRequestMessage(HttpMethod.Get, "https://api/w");
        await Assert.That(async () => _ = await client.SendAsync(plain)).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies non-reusable exchanges are one-shot and matched in declared order.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonReusableExchangesAreOneShotInOrder()
    {
        var handler = new StubHttp
        {
            {
                Route.Any(SeqUrl),
                Reply.Text("first")
            },
            {
                Route.Any(SeqUrl),
                Reply.Text("second")
            },
        };

        var first = await SendAsync(handler, HttpMethod.Get, SeqUrl);
        var second = await SendAsync(handler, HttpMethod.Get, SeqUrl);

        await Assert.That(await first.Content.ReadAsStringAsync()).IsEqualTo("first");
        await Assert.That(await second.Content.ReadAsStringAsync()).IsEqualTo("second");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a reusable exchange matches repeatedly and is not required by verification.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReusableExchangeMatchesRepeatedly()
    {
        const int callCount = 2;
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Text("again")
            },
        };

        for (var i = 0; i < callCount; i++)
        {
            _ = await SendAsync(handler, HttpMethod.Get, "https://api/" + i);
        }

        await handler.VerifyAllCalledAsync();
        await Assert.That(handler.Requests.Count).IsEqualTo(callCount);
    }

    /// <summary>Verifies the custom responder produces the full response and captures the request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RespondWithProducesCustomResponse()
    {
        var handler = new StubHttp
        {
            {
                Route.Any("*"),
                Reply.From(static request => new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent(request.RequestUri!.AbsolutePath), })
            },
        };

        var response = await SendAsync(handler, HttpMethod.Get, "https://api/echo");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("/echo");
    }

    /// <summary>Verifies received requests are recorded in order for inspection.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestsAreRecorded()
    {
        var handler = new StubHttp
        {
            {
                Route.Any("https://api/a"),
                Reply.Status(HttpStatusCode.OK)
            },
            {
                Route.Any(BodyUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        _ = await SendAsync(handler, HttpMethod.Get, "https://api/a");
        _ = await SendAsync(handler, HttpMethod.Post, BodyUrl);

        await Assert.That(handler.Requests[0].RequestUri?.AbsolutePath).IsEqualTo("/a");
        await Assert.That(handler.Requests[1].Method).IsEqualTo(HttpMethod.Post);
    }

    /// <summary>Verifies <see cref="StubHttp.ToSettings()"/> points the settings handler factory at the stub.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ToSettingsWiresHandlerFactory()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ThingUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var settings = handler.ToSettings();

        await Assert.That(settings.HttpMessageHandlerFactory).IsNotNull();
        await Assert.That(settings.HttpMessageHandlerFactory!()).IsSameReferenceAs(handler);
    }

    /// <summary>Verifies <see cref="StubHttp.ToSettings(RefitSettings)"/> preserves other settings.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ToSettingsPreservesBaseSettings()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(ThingUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };
        var baseSettings = new RefitSettings { Buffered = true };

        var settings = handler.ToSettings(baseSettings);

        await Assert.That(settings).IsSameReferenceAs(baseSettings);
        await Assert.That(settings.Buffered).IsTrue();
        await Assert.That(settings.HttpMessageHandlerFactory!()).IsSameReferenceAs(handler);
    }

    /// <summary>Verifies an async responder can await the request body before producing a response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RespondWithAsyncAwaitsRequestBody()
    {
        var handler = new StubHttp
        {
            {
                Route.Post("*"),
                Reply.From(static async request =>
                {
                    var body = await request.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body.ToUpperInvariant())
                    };
                })
            },
        };

        var response = await SendAsync(handler, HttpMethod.Post, "https://api/echo", new StringContent("hello"));

        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("HELLO");
    }

    /// <summary>Verifies the async verify waits for a fire-and-forget request that arrives later.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task VerifyAllCalledAsyncWaitsForPendingRequest()
    {
        const int landingDelayMs = 30;
        const int waitSeconds = 5;
        var handler = new StubHttp
        {
            {
                Route.Any("https://api/bg"),
                Reply.Status(HttpStatusCode.OK)
            },
        };
        using var client = new HttpClient(handler);

        // Fire-and-forget: the request only lands after a delay, so a synchronous verify would fail.
        _ = Task.Run(async () =>
        {
            await Task.Delay(landingDelayMs);
            _ = await client.GetAsync(new Uri("https://api/bg"));
        });

        await handler.VerifyAllCalledAsync(TimeSpan.FromSeconds(waitSeconds));

        await Assert.That(handler.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies the async verify faults when the expected request never arrives within the timeout.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task VerifyAllCalledAsyncThrowsOnTimeout()
    {
        const int timeoutMs = 50;
        var handler = new StubHttp
        {
            {
                Route.Any("https://api/never"),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(async () => await handler.VerifyAllCalledAsync(TimeSpan.FromMilliseconds(timeoutMs)))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Sends a request through a handler and returns the response.</summary>
    /// <param name="handler">The handler under test.</param>
    /// <param name="method">The request method.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="content">Optional request content.</param>
    /// <returns>The response produced by the handler.</returns>
    private static async Task<HttpResponseMessage> SendAsync(
        StubHttp handler,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(method, url) { Content = content };
        return await client.SendAsync(request);
    }
}
