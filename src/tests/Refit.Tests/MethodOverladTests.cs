// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests that Refit dispatches correctly across overloaded interface methods.</summary>
public class MethodOverladTests
{
    /// <summary>The base address of the overloaded httpbin service.</summary>
    private const string BaseUrl = "https://httpbin.org/";

    /// <summary>The httpbin GET endpoint URL exercised by the generic overloads.</summary>
    private const string GetUrl = "https://httpbin.org/get";

    /// <summary>The query parameter name exercised by the generic overloads.</summary>
    private const string ParamKey = "param";

    /// <summary>The HTTP status code requested from the stub to exercise the Forbidden response path.</summary>
    private const int ForbiddenStatusCode = 403;

    /// <summary>Verifies that non-generic <c>Get</c> overloads resolve to the correct request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BasicMethodOverloadTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(BaseUrl),
                new StubResponse { Status = HttpStatusCode.OK, Text = "OK", ContentType = "text/html" }
            },
            {
                Route.Get("https://httpbin.org/status/403"),
                Reply.Status(HttpStatusCode.Forbidden)
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedMethods>(BaseUrl);
        var plainText = await fixture.Get();

        var resp = await fixture.Get(ForbiddenStatusCode);

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    /// <summary>Verifies the parameterless generic <c>Get</c> overload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest1()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(BaseUrl),
                new StubResponse { Status = HttpStatusCode.OK, Text = "OK", ContentType = "text/html" }
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);
        var plainText = await fixture.Get();

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
    }

    /// <summary>Verifies the generic <c>Get</c> overload that targets the status endpoint.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest2()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://httpbin.org/status/403"),
                Reply.Status(HttpStatusCode.Forbidden)
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);

        var resp = await fixture.Get(ForbiddenStatusCode);

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    /// <summary>Verifies the single-type-argument generic <c>Get</c> overload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest3()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GetUrl, Query = [("someVal", "201")] },
                Reply.Json("some-T-value")
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);

        const int someValue = 201;
        var result = await fixture.Get<string>(someValue);

        await Assert.That(result).IsEqualTo("some-T-value");
    }

    /// <summary>Verifies the generic <c>Get</c> overload that sends a parameter and header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest4()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GetUrl, Query = [(ParamKey, "foo")], Headers = [("X-Refit", "99")] },
                Reply.Json("{\"url\": \"https://httpbin.org/get\", \"args\": {\"param\": \"foo\"}}")
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);

        const int headerValue = 99;
        var result = await fixture.Get("foo", headerValue);

        await Assert.That(result.Args![ParamKey]).IsEqualTo("foo");
    }

    /// <summary>Verifies the generic <c>Get</c> overload with header and parameter types swapped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest5()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GetUrl, Query = [(ParamKey, "99")], Headers = [("X-Refit", "foo")] },
                Reply.Json("{\"url\": \"https://httpbin.org/get\", \"args\": {\"param\": \"99\"}}")
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);

        const int paramValue = 99;
        var result = await fixture.Get(paramValue, "foo");

        await Assert.That(result.Args![ParamKey]).IsEqualTo("99");
    }

    /// <summary>Verifies the two-type-argument generic <c>Get</c> overload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest6()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = GetUrl, Query = [("input", "99")] },
                Reply.Json("generic-output")
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);

        const int inputValue = 99;
        var result = await fixture.Get<string, int>(inputValue);

        await Assert.That(result).IsEqualTo("generic-output");
    }

    /// <summary>Verifies the two-input generic <c>Get</c> overload with inferable type arguments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest7()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Get,
                    Template = GetUrl,
                    Query = [.. new Dictionary<string, string> { { "input1", "str" }, { "input2", "3" } }.Select(static kv => (kv.Key, kv.Value))],
                },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(BaseUrl);

        const int secondInput = 3;
        await fixture.Get<string, int>("str", secondInput);

        await handler.VerifyAllCalledAsync();
    }
}
