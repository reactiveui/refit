// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies that a <c>[Url]</c> parameter dispatches a request to an arbitrary absolute URL, bypassing the client base
/// address, and that the source generator produces exactly the same request URI as the reflection request builder.
/// </summary>
public class AbsoluteUrlDispatchTests
{
    /// <summary>The base address of the client under test, which a <c>[Url]</c> request ignores.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>The absolute URL, on a different host, dispatched by the <c>[Url]</c> parameter.</summary>
    private const string AbsoluteUrl = "https://cdn.example.com/files/data.bin";

    /// <summary>The serialized JSON body shared by the POST fixtures.</summary>
    private const string SerializedBody = "{\"name\":\"x\"}";

    /// <summary>A relative value rejected by both builders because <c>[Url]</c> requires an absolute URI.</summary>
    private const string RelativePath = "relative/path";

    /// <summary>Verifies a string <c>[Url]</c> value is dispatched verbatim, ignoring the base address, in both builders.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StringUrlDispatchesToAbsoluteUrlIgnoringBaseAddress()
    {
        var generated = await SendGeneratedAsync(static api => api.GetAbsolute(AbsoluteUrl));
        var reflected = await ReflectAsync(nameof(IUrlDispatchApi.GetAbsolute), AbsoluteUrl);

        await Assert.That(generated.RequestMessage!.RequestUri!.AbsoluteUri).IsEqualTo(AbsoluteUrl);
        await Assert.That(reflected.RequestUri!.AbsoluteUri).IsEqualTo(AbsoluteUrl);
    }

    /// <summary>Verifies a <c>[Query]</c> parameter is appended to the absolute URL in both builders.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParametersAppendToAbsoluteUrl()
    {
        const string expected = AbsoluteUrl + "?token=abc";

        var generated = await SendGeneratedAsync(static api => api.GetAbsoluteWithQuery(AbsoluteUrl, "abc"));
        var reflected = await ReflectAsync(nameof(IUrlDispatchApi.GetAbsoluteWithQuery), AbsoluteUrl, "abc");

        await Assert.That(generated.RequestMessage!.RequestUri!.AbsoluteUri).IsEqualTo(expected);
        await Assert.That(reflected.RequestUri!.AbsoluteUri).IsEqualTo(expected);
    }

    /// <summary>Verifies a <c>[Url]</c> value that already carries a query string appends further parameters with <c>&amp;</c>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task QueryParametersAppendToAbsoluteUrlThatAlreadyHasAQuery()
    {
        const string urlWithQuery = AbsoluteUrl + "?existing=1";
        const string expected = urlWithQuery + "&token=abc";

        var generated = await SendGeneratedAsync(static api => api.GetAbsoluteWithQuery(urlWithQuery, "abc"));
        var reflected = await ReflectAsync(nameof(IUrlDispatchApi.GetAbsoluteWithQuery), urlWithQuery, "abc");

        await Assert.That(generated.RequestMessage!.RequestUri!.AbsoluteUri).IsEqualTo(expected);
        await Assert.That(reflected.RequestUri!.AbsoluteUri).IsEqualTo(expected);
    }

    /// <summary>Verifies a <see cref="Uri"/> <c>[Url]</c> value is dispatched as its absolute URI in both builders.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UriUrlDispatchesToAbsoluteUrl()
    {
        var uri = new Uri(AbsoluteUrl);

        var generated = await SendGeneratedAsync(api => api.GetAbsoluteFromUri(uri));
        var reflected = await ReflectAsync(nameof(IUrlDispatchApi.GetAbsoluteFromUri), uri);

        await Assert.That(generated.RequestMessage!.RequestUri!.AbsoluteUri).IsEqualTo(AbsoluteUrl);
        await Assert.That(reflected.RequestUri!.AbsoluteUri).IsEqualTo(AbsoluteUrl);
    }

    /// <summary>Verifies a POST to a <see cref="Uri"/> <c>[Url]</c> still binds the remaining parameter as the body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AbsoluteUrlOnPostCarriesImplicitBody()
    {
        var uri = new Uri(AbsoluteUrl);
        var body = new BodyPayload { Name = "x" };

        var generated = await SendGeneratedAsync(api => api.PostAbsolute(uri, body));
        var reflected = await ReflectAsync(nameof(IUrlDispatchApi.PostAbsolute), uri, body);

        await Assert.That(generated.RequestMessage!.RequestUri!.AbsoluteUri).IsEqualTo(AbsoluteUrl);
        await Assert.That(generated.SendContent).IsEqualTo(SerializedBody);
        await Assert.That(reflected.RequestUri!.AbsoluteUri).IsEqualTo(AbsoluteUrl);
    }

    /// <summary>Verifies a relative string <c>[Url]</c> value throws an <see cref="ArgumentException"/> in both builders.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RelativeStringUrlThrowsArgumentException()
    {
        await Assert
            .That(static () => (Task)SendGeneratedAsync(static api => api.GetAbsolute(RelativePath)))
            .Throws<ArgumentException>();

        var factory = new RequestBuilderImplementation<IUrlDispatchApi>()
            .BuildRequestFactoryForMethod(nameof(IUrlDispatchApi.GetAbsolute));
        await Assert.That(() => (Task)factory([RelativePath])).Throws<ArgumentException>();
    }

    /// <summary>Verifies a relative <see cref="Uri"/> <c>[Url]</c> value throws an <see cref="ArgumentException"/> in both builders.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RelativeUriUrlThrowsArgumentException()
    {
        var relativeUri = new Uri(RelativePath, UriKind.Relative);

        await Assert
            .That(() => (Task)SendGeneratedAsync(api => api.GetAbsoluteFromUri(relativeUri)))
            .Throws<ArgumentException>();

        var factory = new RequestBuilderImplementation<IUrlDispatchApi>()
            .BuildRequestFactoryForMethod(nameof(IUrlDispatchApi.GetAbsoluteFromUri));
        await Assert.That(() => (Task)factory([relativeUri])).Throws<ArgumentException>();
    }

    /// <summary>Verifies the reflection builder rejects a <c>[Url]</c> method that also declares a path template.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UrlMethodWithPathTemplateThrows() =>
        await Assert
            .That(static () => new RequestBuilderImplementation<IUrlWithTemplateApi>())
            .Throws<ArgumentException>();

    /// <summary>Verifies the reflection builder rejects a method with more than one <c>[Url]</c> parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleUrlParametersThrow() =>
        await Assert
            .That(static () => new RequestBuilderImplementation<IMultipleUrlApi>())
            .Throws<ArgumentException>();

    /// <summary>Verifies the reflection builder rejects a <c>[Url]</c> parameter that is neither a string nor a <see cref="Uri"/>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NonStringOrUriUrlParameterThrows() =>
        await Assert
            .That(static () => new RequestBuilderImplementation<IUrlWrongTypeApi>())
            .Throws<ArgumentException>();

    /// <summary>Sends one request through the source-generated client and returns the handler that observed it.</summary>
    /// <param name="call">The interface method to invoke on the generated client.</param>
    /// <returns>The handler that captured the request.</returns>
    private static async Task<TestHttpMessageHandler> SendGeneratedAsync(Func<IUrlDispatchApi, Task<string>> call)
    {
        var handler = new TestHttpMessageHandler();
        using var client = new HttpClient(handler) { BaseAddress = new(BaseAddress) };
        var api = RestService.ForGenerated<IUrlDispatchApi>(client, new RefitSettings());

        _ = await call(api);

        return handler;
    }

    /// <summary>Builds a request through the reflection request builder and returns the produced request message.</summary>
    /// <param name="method">The interface method to build a request for.</param>
    /// <param name="args">The argument values for the call.</param>
    /// <returns>The produced request message.</returns>
    private static Task<HttpRequestMessage> ReflectAsync(string method, params object[] args) =>
        new RequestBuilderImplementation<IUrlDispatchApi>()
            .BuildRequestFactoryForMethod(method)(args);
}
