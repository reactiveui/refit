// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for the authenticated HTTP client handler behavior.</summary>
public class AuthenticatedClientHandlerTests
{
    /// <summary>Plain text content type used by the stubbed responses.</summary>
    private const string PlainTextContentType = "text/plain";

    /// <summary>Bearer token value returned by the authorization getter.</summary>
    private const string TokenValue = "tokenValue";

    /// <summary>Base address for the stubbed API.</summary>
    private const string BaseUrl = "http://api";

    /// <summary>Fully qualified URL of the authenticated endpoint.</summary>
    private const string AuthUrl = BaseUrl + "/auth";

    /// <summary>Name of the HTTP authorization header.</summary>
    private const string AuthorizationHeader = "Authorization";

    /// <summary>Authorization header value carrying the bearer token.</summary>
    private const string BearerTokenValue = "Bearer " + TokenValue;

    /// <summary>Authorization header value carrying the refreshed bearer token.</summary>
    private const string BearerTokenValue2 = "Bearer tokenValue2";

    /// <summary>Name of the HTTP user agent header.</summary>
    private const string UserAgentHeader = "User-Agent";

    /// <summary>Header value identifying Refit as the caller.</summary>
    private const string RefitValue = "Refit";

    /// <summary>Name of the forwarded-for header.</summary>
    private const string ForwardedForHeader = "X-Forwarded-For";

    /// <summary>Refit service contract exercising the various authentication scenarios.</summary>
    public interface IMyAuthenticatedService
    {
        /// <summary>Gets an unauthenticated resource.</summary>
        /// <returns>The response body.</returns>
        [Get("/unauth")]
        Task<string> GetUnauthenticated();

        /// <summary>Gets a resource that requires bearer authentication.</summary>
        /// <returns>The response body.</returns>
        [Get("/auth")]
        [Headers("Authorization: Bearer")]
        Task<string> GetAuthenticated();

        /// <summary>Gets a resource using a bearer token supplied as a method parameter.</summary>
        /// <param name="token">The bearer token.</param>
        /// <returns>The response body.</returns>
        [Get("/auth")]
        Task<string> GetAuthenticatedWithTokenInMethod([Authorize("Bearer")] string token);

        /// <summary>Gets a resource using both an authorize attribute and a header collection.</summary>
        /// <param name="token">The bearer token.</param>
        /// <param name="headers">Additional headers to send.</param>
        /// <returns>The response body.</returns>
        [Get("/auth")]
        Task<string> GetAuthenticatedWithAuthorizeAttributeAndHeaderCollection(
            [Authorize("Bearer")] string token,
            [HeaderCollection] IDictionary<string, string> headers);

        /// <summary>Gets a resource using a token supplied through a header collection.</summary>
        /// <param name="headers">Headers to send, including the authorization header.</param>
        /// <returns>The response body.</returns>
        [Get("/auth")]
        Task<string> GetAuthenticatedWithTokenInHeaderCollection(
            [HeaderCollection] IDictionary<string, string> headers);

        /// <summary>Posts request data using a token supplied through a header collection.</summary>
        /// <param name="id">The resource identifier.</param>
        /// <param name="content">The request payload.</param>
        /// <param name="headers">Headers to send, including the authorization header.</param>
        /// <returns>The response body.</returns>
        [Post("/auth/{id}")]
        Task<string> PostAuthenticatedWithTokenInHeaderCollection(
            int id,
            SomeRequestData content,
            [HeaderCollection] IDictionary<string, string> headers);
    }

    /// <summary>Service that inherits authentication headers from a base contract.</summary>
    public interface IInheritedAuthenticatedServiceWithHeaders : IAuthenticatedServiceWithHeaders
    {
        /// <summary>Gets a resource declared on the inheriting contract.</summary>
        /// <returns>The response body.</returns>
        [Get("/get-inherited-thing")]
        Task<string> GetInheritedThing();
    }

    /// <summary>Service whose inherited route contains CRLF characters used to verify smuggling protection.</summary>
    public interface IInheritedAuthenticatedServiceWithHeadersCrlf : IAuthenticatedServiceWithHeaders
    {
        /// <summary>Gets a resource whose route contains injected CRLF characters.</summary>
        /// <returns>The response body.</returns>
        [Get("/get-inherited-thing\r\n\r\nGET /smuggled")]
        Task<string> GetInheritedThing();
    }

    /// <summary>Base service contract that declares a bearer authorization header.</summary>
    [Headers("Authorization: Bearer")]
    public interface IAuthenticatedServiceWithHeaders
    {
        /// <summary>Gets a resource declared on the base contract.</summary>
        /// <returns>The response body.</returns>
        [Get("/get-base-thing")]
        Task<string> GetThingFromBase();
    }

    /// <summary>Verifies the default inner handler is an <see cref="HttpClientHandler"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task DefaultHandlerIsHttpClientHandler()
    {
        var handler = new AuthenticatedHttpClientHandler(static (_, _) => new ValueTask<string>(string.Empty));

        await Assert.That(handler.InnerHandler).IsTypeOf<HttpClientHandler>();
    }

    /// <summary>Verifies the inner handler is null when an explicit null inner handler is supplied.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task DefaultHandlerIsNull()
    {
        var handler = new AuthenticatedHttpClientHandler(null, static (_, _) => new ValueTask<string>(string.Empty));

        await Assert.That(handler.InnerHandler).IsNull();
    }

    /// <summary>Verifies the constructor that takes an inner handler stores it when provided.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ExplicitInnerHandlerIsAssigned()
    {
        using var innerHandler = new TestHttpMessageHandler();
        var handler = new AuthenticatedHttpClientHandler(innerHandler, static (_, _) => new ValueTask<string>(string.Empty));

        await Assert.That(handler.InnerHandler).IsSameReferenceAs(innerHandler);
    }

    /// <summary>Verifies a null token getter throws an <see cref="ArgumentNullException"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NullTokenGetterThrows() =>
        await Assert
            .That(static () => new AuthenticatedHttpClientHandler(
                (Func<HttpRequestMessage, CancellationToken, ValueTask<string>>)null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies unauthenticated calls do not send an authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerIgnoresUnAuth()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://api/unauth", Where = static msg => msg.Headers.Authorization is null },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl, new RefitSettings
        {
            AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>(TokenValue)
        });

        var result = await fixture.GetUnauthenticated();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authenticated calls send the bearer authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerUsesAuth()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl, new RefitSettings
        {
            AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>(TokenValue)
        });

        var result = await fixture.GetAuthenticated();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authentication works when the token is supplied as a method parameter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithTokenInParameterUsesAuth()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl);

        var result = await fixture.GetAuthenticatedWithTokenInMethod(TokenValue);

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authentication works when the token is supplied through a header collection.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithTokenInHeaderCollectionUsesAuth()
    {
        var headers = new Dictionary<string, string>
        {
            { UserAgentHeader, RefitValue },
            { AuthorizationHeader, BearerTokenValue }
        };

        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [.. headers.Select(static kv => (kv.Key, kv.Value))] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl);

        var result = await fixture.GetAuthenticatedWithTokenInHeaderCollection(headers);

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authentication works with both an authorize attribute and a header collection.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithAuthorizeAttributeAndHeaderCollectionUsesAuth()
    {
        var expectedHeaders = new Dictionary<string, string>
        {
            { AuthorizationHeader, BearerTokenValue },
            { UserAgentHeader, RefitValue },
            { ForwardedForHeader, RefitValue }
        };

        var headerCollectionHeaders = new Dictionary<string, string>
        {
            { UserAgentHeader, RefitValue },
            { ForwardedForHeader, RefitValue }
        };

        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [.. expectedHeaders.Select(static kv => (kv.Key, kv.Value))] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl);

        var result = await fixture.GetAuthenticatedWithAuthorizeAttributeAndHeaderCollection(
            TokenValue,
            headerCollectionHeaders);

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies an explicit authorization header in the collection overrides the attribute token.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithDuplicatedAuthorizationHeaderUsesAuth()
    {
        var expectedHeaders = new Dictionary<string, string>
        {
            { AuthorizationHeader, BearerTokenValue2 },
            { UserAgentHeader, RefitValue },
            { ForwardedForHeader, RefitValue }
        };

        var headerCollectionHeaders = new Dictionary<string, string>
        {
            { AuthorizationHeader, BearerTokenValue2 },
            { UserAgentHeader, RefitValue },
            { ForwardedForHeader, RefitValue }
        };

        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [.. expectedHeaders.Select(static kv => (kv.Key, kv.Value))] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl);

        var result = await fixture.GetAuthenticatedWithAuthorizeAttributeAndHeaderCollection(
            TokenValue,
            headerCollectionHeaders);

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies posting with a token supplied through a header collection sends the authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerPostTokenInHeaderCollectionUsesAuth()
    {
        const int id = 1;
        var someRequestData = new SomeRequestData { ReadablePropertyName = 1 };

        var headers = new Dictionary<string, string>
        {
            { AuthorizationHeader, BearerTokenValue2 },
            { "ThingId", id.ToString() }
        };

        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = $"http://api/auth/{id}", Headers = [.. headers.Select(static kv => (kv.Key, kv.Value))] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IMyAuthenticatedService>(BaseUrl);

        var result = await fixture.PostAuthenticatedWithTokenInHeaderCollection(
            id,
            someRequestData,
            headers);

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies a method inherited from a base contract with a headers attribute sends the authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthentictedMethodFromBaseClassWithHeadersAttributeUsesAuth()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://api/get-base-thing", Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IInheritedAuthenticatedServiceWithHeaders>(BaseUrl, new RefitSettings
        {
            AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>(TokenValue)
        });

        var result = await fixture.GetThingFromBase();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies a method declared on an inheriting contract with a headers attribute sends the authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthentictedMethodFromInheritedClassWithHeadersAttributeUsesAuth()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://api/get-inherited-thing", Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var fixture = handler.CreateClient<IInheritedAuthenticatedServiceWithHeaders>(BaseUrl, new RefitSettings
        {
            AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>(TokenValue)
        });

        var result = await fixture.GetInheritedThing();

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies a route containing CRLF characters is rejected to prevent header smuggling.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthentictedMethodFromInheritedClassWithHeadersAttributeUsesAuth_WithCRLFCheck()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://api/get-inherited-thing", Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        await Assert.That(async () =>
        {
            var fixture = handler.CreateClient<IInheritedAuthenticatedServiceWithHeadersCrlf>(BaseUrl, new RefitSettings
            {
                AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>(TokenValue)
            });

            await fixture.GetInheritedThing();
        }).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies the authorization header value getter is used when supplying an explicit HTTP client.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterIsUsedWhenSupplyingHttpClient()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new(BaseUrl) };

        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>(TokenValue)
        };

        var fixture = RestService.For<IMyAuthenticatedService>(httpClient, settings);

        var result = await fixture.GetAuthenticated();

        await handler.VerifyAllCalledAsync();
        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies an awaited authorization header value getter is honored when supplying an explicit HTTP client.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterCanAwaitWhenSupplyingHttpClient()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [(AuthorizationHeader, BearerTokenValue)] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new(BaseUrl) };

        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = static async (_, _) =>
            {
                await Task.Yield();
                return TokenValue;
            }
        };

        var fixture = RestService.For<IMyAuthenticatedService>(httpClient, settings);

        var result = await fixture.GetAuthenticated();

        await handler.VerifyAllCalledAsync();
        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies an explicit token parameter is not overridden by the authorization header value getter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterDoesNotOverrideExplicitTokenWhenSupplyingHttpClient()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = AuthUrl, Headers = [(AuthorizationHeader, "Bearer token-from-parameter")] },
                Reply.Text("Ok", PlainTextContentType)
            },
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new(BaseUrl) };

        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = static (_, _) => new ValueTask<string>("token-from-getter")
        };

        var fixture = RestService.For<IMyAuthenticatedService>(httpClient, settings);

        var result = await fixture.GetAuthenticatedWithTokenInMethod("token-from-parameter");

        await handler.VerifyAllCalledAsync();
        await Assert.That(result).IsEqualTo("Ok");
    }
}
