// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests that Refit dispatches correctly across overloaded interface methods.</summary>
public class MethodOverladTests
{
    /// <summary>Verifies that non-generic <c>Get</c> overloads resolve to the correct request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BasicMethodOverloadTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/")
            .Respond(HttpStatusCode.OK, "text/html", "OK");

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/status/403")
            .Respond(HttpStatusCode.Forbidden);

        var fixture = RestService.For<IUseOverloadedMethods>("https://httpbin.org/", settings);
        var plainText = await fixture.Get();

        var resp = await fixture.Get(403);

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    /// <summary>Verifies the parameterless generic <c>Get</c> overload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest1()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/")
            .Respond(HttpStatusCode.OK, "text/html", "OK");

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);
        var plainText = await fixture.Get();

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
    }

    /// <summary>Verifies the generic <c>Get</c> overload that targets the status endpoint.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest2()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/status/403")
            .Respond(HttpStatusCode.Forbidden);

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);

        var resp = await fixture.Get(403);

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    /// <summary>Verifies the single-type-argument generic <c>Get</c> overload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest3()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithQueryString("someVal", "201")
            .Respond("application/json", "some-T-value");

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);

        var result = await fixture.Get<string>(201);

        await Assert.That(result).IsEqualTo("some-T-value");
    }

    /// <summary>Verifies the generic <c>Get</c> overload that sends a parameter and header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest4()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithHeaders("X-Refit", "99")
            .WithQueryString("param", "foo")
            .Respond(
                "application/json",
                "{\"url\": \"https://httpbin.org/get\", \"args\": {\"param\": \"foo\"}}");

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);

        var result = await fixture.Get("foo", 99);

        await Assert.That(result.Args!["param"]).IsEqualTo("foo");
    }

    /// <summary>Verifies the generic <c>Get</c> overload with header and parameter types swapped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest5()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithHeaders("X-Refit", "foo")
            .WithQueryString("param", "99")
            .Respond(
                "application/json",
                "{\"url\": \"https://httpbin.org/get\", \"args\": {\"param\": \"99\"}}");

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);

        var result = await fixture.Get(99, "foo");

        await Assert.That(result.Args!["param"]).IsEqualTo("99");
    }

    /// <summary>Verifies the two-type-argument generic <c>Get</c> overload.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest6()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithQueryString("input", "99")
            .Respond("application/json", "generic-output");

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);

        var result = await fixture.Get<string, int>(99);

        await Assert.That(result).IsEqualTo("generic-output");
    }

    /// <summary>Verifies the two-input generic <c>Get</c> overload with inferable type arguments.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericMethodOverloadTest7()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithQueryString(
                new Dictionary<string, string> { { "input1", "str" }, { "input2", "3" } })
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>(
            "https://httpbin.org/",
            settings);

        await fixture.Get<string, int>("str", 3);

        mockHttp.VerifyNoOutstandingExpectation();
    }
}
