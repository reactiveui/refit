// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.IO;
using System.Net;

namespace Refit.Testing.Tests;

/// <summary>Additional coverage for the route/reply factories, matcher edge cases, typed capture, and <see cref="StubApiResponse{T}"/>.</summary>
public sealed class StubHttpCoverageTests
{
    /// <summary>The base address used by the coverage tests.</summary>
    private const string BaseUrl = "https://api.test";

    /// <summary>The number of routes configured by the enumeration test.</summary>
    private const int RouteCount = 2;

    /// <summary>An index beyond the recorded requests, used to exercise range validation.</summary>
    private const int OutOfRangeIndex = 5;

    /// <summary>Verifies each verb-specific <see cref="Route"/> factory matches its method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RouteFactoriesMatchTheirMethods()
    {
        var handler = new StubHttp
        {
            { Route.Put("/put"), Reply.Status(HttpStatusCode.OK) },
            { Route.Delete("/delete"), Reply.Status(HttpStatusCode.OK) },
            { Route.Patch("/patch"), Reply.Status(HttpStatusCode.OK) },
            { Route.Head("/head"), Reply.Status(HttpStatusCode.OK) },
            { Route.For(HttpMethod.Options, "/options"), Reply.Status(HttpStatusCode.OK) },
        };

        await Assert.That((await SendAsync(handler, HttpMethod.Put, BaseUrl + "/put")).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That((await SendAsync(handler, HttpMethod.Delete, BaseUrl + "/delete")).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That((await SendAsync(handler, HttpMethod.Patch, BaseUrl + "/patch")).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That((await SendAsync(handler, HttpMethod.Head, BaseUrl + "/head")).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That((await SendAsync(handler, HttpMethod.Options, BaseUrl + "/options")).StatusCode).IsEqualTo(HttpStatusCode.OK);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies <see cref="Reply.Json(string, HttpStatusCode)"/> carries both body and status.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReplyJsonWithStatusCarriesBoth()
    {
        var handler = new StubHttp { { Route.Get("/j"), Reply.Json("{\"ok\":true}", HttpStatusCode.Created) } };

        var response = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/j");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
    }

    /// <summary>Verifies <see cref="Reply.Text(string, string)"/> uses the supplied content type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReplyTextWithContentTypeUsesIt()
    {
        var handler = new StubHttp { { Route.Get("/t"), Reply.Text("<b>hi</b>", "text/html") } };

        var response = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/t");

        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/html");
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("<b>hi</b>");
    }

    /// <summary>Verifies <see cref="Reply.Content(HttpContent)"/> uses the supplied content verbatim.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReplyContentUsesSuppliedContent()
    {
        var handler = new StubHttp { { Route.Get("/c"), Reply.Content(new StringContent("raw-bytes")) } };

        var response = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/c");

        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("raw-bytes");
    }

    /// <summary>Verifies the request body can be read by index and that an out-of-range index throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestBodyByIndexRoundTripsAndValidatesRange()
    {
        var handler = new StubHttp { { Route.Post("/users"), Reply.With(new User(1, "created")) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        _ = await api.CreateUser(new NewUser("bob"));

        var sent = await handler.RequestBodyAsync<NewUser>(0);
        await Assert.That(sent!.Login).IsEqualTo("bob");
        await Assert.That(() => handler.RequestBodyAsync<NewUser>(OutOfRangeIndex)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies <see cref="StubHttp.LastRequestBodyAsync{T}"/> throws when no request has been received.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LastRequestBodyThrowsWhenNoRequests()
    {
        var handler = new StubHttp();

        await Assert.That(handler.LastRequestBodyAsync<string>).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a captured body of a bodyless request deserializes to the default value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LastRequestBodyIsDefaultForBodylessRequest()
    {
        var handler = new StubHttp { { Route.Get("/none"), Reply.Status(HttpStatusCode.OK) } };

        _ = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/none");

        await Assert.That(await handler.LastRequestBodyAsync<NewUser>()).IsNull();
    }

    /// <summary>Verifies the handler enumerates its configured routes through both enumerator interfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HandlerEnumeratesRoutes()
    {
        var handler = new StubHttp
        {
            { Route.Get("/a"), Reply.Status(HttpStatusCode.OK) },
            { Route.Post("/b"), Reply.Status(HttpStatusCode.OK) },
        };

        var generic = handler.ToList();

        var nonGeneric = 0;
        var enumerator = ((IEnumerable)handler).GetEnumerator();
        while (enumerator.MoveNext())
        {
            nonGeneric++;
        }

        await Assert.That(generic.Count).IsEqualTo(RouteCount);
        await Assert.That(nonGeneric).IsEqualTo(RouteCount);
    }

    /// <summary>Verifies a <c>{placeholder}</c> segment does not match an empty path segment.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TemplatePlaceholderRejectsEmptySegment()
    {
        var handler = new StubHttp { { Route.Get("/a/{id}"), Reply.Status(HttpStatusCode.OK) } };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/a/"))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a template with a different number of segments does not match.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TemplateRejectsSegmentCountMismatch()
    {
        var handler = new StubHttp { { Route.Get("/a/{id}"), Reply.Status(HttpStatusCode.OK) } };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/a/1/extra"))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a literal segment mismatch does not match.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TemplateRejectsLiteralMismatch()
    {
        var handler = new StubHttp { { Route.Get("/a/{id}"), Reply.Status(HttpStatusCode.OK) } };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/b/1"))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies an empty query pair (from <c>&amp;&amp;</c>) is ignored during query matching.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EmptyQueryPairIsIgnored()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = "/q", Query = [("a", "1")] }, Reply.Status(HttpStatusCode.OK) } };

        var response = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/q?a=1&&b=2");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies the exact query-pair matcher rejects a request with a different value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExactQueryParamsRejectWrongValue()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = "/q", ExactQueryParams = [("a", "1")] }, Reply.Status(HttpStatusCode.OK) } };

        await Assert.That(async () => _ = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/q?a=2"))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies the error accessors and disposal on <see cref="StubApiResponse{T}"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StubApiResponseErrorAccessorsAndDispose()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
        var error = new ApiRequestException("failed", request, HttpMethod.Get, new RefitSettings());
        var response = new StubApiResponse<string> { Error = error };

        var hasRequestError = response.HasRequestError(out var requestError);
        var hasResponseError = response.HasResponseError(out _);

        await Assert.That(hasRequestError).IsTrue();
        await Assert.That(requestError).IsSameReferenceAs(error);
        await Assert.That(hasResponseError).IsFalse();

        response.Dispose();
    }

    /// <summary>Verifies the source-generated client factory constructs an instance.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateGeneratedClientConstructsInstance()
    {
        var handler = new StubHttp();

        var api = handler.CreateGeneratedClient<IUserApi>(BaseUrl);

        await Assert.That(api).IsNotNull();
    }

    /// <summary>Verifies a body matcher treats a request with no content as an empty body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyMatcherTreatsMissingContentAsEmpty()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Post, Template = "/empty", Body = string.Empty }, Reply.Status(HttpStatusCode.OK) } };

        var response = await SendAsync(handler, HttpMethod.Post, BaseUrl + "/empty");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies a header matcher finds a header carried on the request content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeaderMatcherMatchesContentHeaders()
    {
        var content = new StringContent("x");
        content.Headers.Add("Content-Language", "en");
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Post, Template = "/h", Headers = [("Content-Language", "en")] }, Reply.Status(HttpStatusCode.OK) } };

        var response = await SendAsync(handler, HttpMethod.Post, BaseUrl + "/h", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies a query token with no <c>=</c> is parsed as a key with an empty value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryTokenWithoutEqualsParsesEmptyValue()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = "/q", Query = [("flag", string.Empty)] }, Reply.Status(HttpStatusCode.OK) } };

        var response = await SendAsync(handler, HttpMethod.Get, BaseUrl + "/q?flag&a=1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies a captured body with no content type is deserialized via the default media type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturesBodyWithoutContentType()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Post, Template = "/raw" }, Reply.Status(HttpStatusCode.OK) } };

        _ = await SendAsync(handler, HttpMethod.Post, BaseUrl + "/raw", new ByteArrayContent("{\"Login\":\"z\"}"u8.ToArray()));

        var sent = await handler.RequestBodyAsync<NewUser>(0);
        await Assert.That(sent!.Login).IsEqualTo("z");
    }

    /// <summary>Verifies a request with no URI matches no route (exercised via <see cref="HttpMessageInvoker"/>, which allows it).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullRequestUriMatchesNoRoute()
    {
        var handler = new StubHttp { { Route.Get("/x"), Reply.Status(HttpStatusCode.OK) } };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, (Uri?)null);

        await Assert.That(async () => _ = await invoker.SendAsync(request, default)).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a query matcher treats a null request URI as an empty query (wildcard template, no URI).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullUriEvaluatesQueryAsEmpty()
    {
        var handler = new StubHttp { { new RouteMatcher { Template = "*", Query = [("a", "1")] }, Reply.Status(HttpStatusCode.OK) } };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, (Uri?)null);

        await Assert.That(async () => _ = await invoker.SendAsync(request, default)).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies a body that cannot be buffered is skipped for capture without failing the request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BufferSkipsUnbufferableBody()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Post, Template = "/u" }, Reply.Status(HttpStatusCode.OK) } };

        var response = await SendAsync(handler, HttpMethod.Post, BaseUrl + "/u", new ThrowingContent());

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await handler.LastRequestBodyAsync<NewUser>()).IsNull();
    }

    /// <summary>Sends a request through the handler using a real <see cref="HttpClient"/>.</summary>
    /// <param name="handler">The handler under test.</param>
    /// <param name="method">The request method.</param>
    /// <param name="url">The request URL.</param>
    /// <param name="content">The optional request content.</param>
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

    /// <summary>An <see cref="HttpContent"/> that fails when read, to exercise best-effort body capture.</summary>
    private sealed class ThrowingContent : HttpContent
    {
        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new IOException("This content cannot be buffered.");

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
