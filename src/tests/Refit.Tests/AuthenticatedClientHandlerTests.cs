// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests for the authenticated HTTP client handler behavior.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class AuthenticatedClientHandlerTests
{
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
        var handler = new AuthenticatedHttpClientHandler((_, _) => Task.FromResult(string.Empty));

        await Assert.That(handler.InnerHandler).IsTypeOf<HttpClientHandler>();
    }

    /// <summary>Verifies the inner handler is null when an explicit null inner handler is supplied.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task DefaultHandlerIsNull()
    {
        var handler = new AuthenticatedHttpClientHandler(null, (_, _) => Task.FromResult(string.Empty));

        await Assert.That(handler.InnerHandler).IsNull();
    }

    /// <summary>Verifies a null token getter throws an <see cref="ArgumentNullException"/>.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task NullTokenGetterThrows() =>
        await Assert
            .That(() => new AuthenticatedHttpClientHandler(
                (Func<HttpRequestMessage, CancellationToken, Task<string>>)null!))
            .ThrowsExactly<ArgumentNullException>();

    /// <summary>Verifies unauthenticated calls do not send an authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerIgnoresUnAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("tokenValue"),
            HttpMessageHandlerFactory = () => handler
        };

        handler
            .Expect(HttpMethod.Get, "http://api/unauth")
            .With(msg => msg.Headers.Authorization is null)
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.GetUnauthenticated();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authenticated calls send the bearer authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("tokenValue"),
            HttpMessageHandlerFactory = () => handler
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.GetAuthenticated();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authentication works when the token is supplied as a method parameter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithTokenInParameterUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.GetAuthenticatedWithTokenInMethod("tokenValue");

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authentication works when the token is supplied through a header collection.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithTokenInHeaderCollectionUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var headers = new Dictionary<string, string>
        {
            { "User-Agent", "Refit" },
            { "Authorization", "Bearer tokenValue" }
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders(headers)
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.GetAuthenticatedWithTokenInHeaderCollection(headers);

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies authentication works with both an authorize attribute and a header collection.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithAuthorizeAttributeAndHeaderCollectionUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var expectedHeaders = new Dictionary<string, string>
        {
            { "Authorization", "Bearer tokenValue" },
            { "User-Agent", "Refit" },
            { "X-Forwarded-For", "Refit" }
        };

        var headerCollectionHeaders = new Dictionary<string, string>
        {
            { "User-Agent", "Refit" },
            { "X-Forwarded-For", "Refit" }
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders(expectedHeaders)
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.GetAuthenticatedWithAuthorizeAttributeAndHeaderCollection(
            "tokenValue",
            headerCollectionHeaders);

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies an explicit authorization header in the collection overrides the attribute token.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerWithDuplicatedAuthorizationHeaderUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var expectedHeaders = new Dictionary<string, string>
        {
            { "Authorization", "Bearer tokenValue2" },
            { "User-Agent", "Refit" },
            { "X-Forwarded-For", "Refit" }
        };

        var headerCollectionHeaders = new Dictionary<string, string>
        {
            { "Authorization", "Bearer tokenValue2" },
            { "User-Agent", "Refit" },
            { "X-Forwarded-For", "Refit" }
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders(expectedHeaders)
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.GetAuthenticatedWithAuthorizeAttributeAndHeaderCollection(
            "tokenValue",
            headerCollectionHeaders);

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies posting with a token supplied through a header collection sends the authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthenticatedHandlerPostTokenInHeaderCollectionUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        const int id = 1;
        var someRequestData = new SomeRequestData { ReadablePropertyName = 1 };

        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer tokenValue2" },
            { "ThingId", id.ToString() }
        };

        handler
            .Expect(HttpMethod.Post, $"http://api/auth/{id}")
            .WithHeaders(headers)
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

        var result = await fixture.PostAuthenticatedWithTokenInHeaderCollection(
            id,
            someRequestData,
            headers);

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies a method inherited from a base contract with a headers attribute sends the authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthentictedMethodFromBaseClassWithHeadersAttributeUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("tokenValue"),
            HttpMessageHandlerFactory = () => handler
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-base-thing")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeaders>(
            "http://api",
            settings);

        var result = await fixture.GetThingFromBase();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies a method declared on an inheriting contract with a headers attribute sends the authorization header.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthentictedMethodFromInheritedClassWithHeadersAttributeUsesAuth()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("tokenValue"),
            HttpMessageHandlerFactory = () => handler
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-inherited-thing")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeaders>(
            "http://api",
            settings);

        var result = await fixture.GetInheritedThing();

        handler.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies a route containing CRLF characters is rejected to prevent header smuggling.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthentictedMethodFromInheritedClassWithHeadersAttributeUsesAuth_WithCRLFCheck()
    {
        var handler = new MockHttpMessageHandler();
        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("tokenValue"),
            HttpMessageHandlerFactory = () => handler,
        };

        handler
            .Expect(HttpMethod.Get, "http://api/get-inherited-thing")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        await Assert.That(async () =>
        {
            var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeadersCrlf>(
                "http://api",
                settings);

            await fixture.GetInheritedThing();
        }).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies the authorization header value getter is used when supplying an explicit HTTP client.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterIsUsedWhenSupplyingHttpClient()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new("http://api") };

        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("tokenValue")
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>(httpClient, settings);

        var result = await fixture.GetAuthenticated();

        handler.VerifyNoOutstandingExpectation();
        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies an awaited authorization header value getter is honored when supplying an explicit HTTP client.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterCanAwaitWhenSupplyingHttpClient()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new("http://api") };

        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = async (_, _) =>
            {
                await Task.Yield();
                return "tokenValue";
            }
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders("Authorization", "Bearer tokenValue")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>(httpClient, settings);

        var result = await fixture.GetAuthenticated();

        handler.VerifyNoOutstandingExpectation();
        await Assert.That(result).IsEqualTo("Ok");
    }

    /// <summary>Verifies an explicit token parameter is not overridden by the authorization header value getter.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AuthorizationHeaderValueGetterDoesNotOverrideExplicitTokenWhenSupplyingHttpClient()
    {
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new("http://api") };

        var settings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("token-from-getter")
        };

        handler
            .Expect(HttpMethod.Get, "http://api/auth")
            .WithHeaders("Authorization", "Bearer token-from-parameter")
            .Respond("text/plain", "Ok");

        var fixture = RestService.For<IMyAuthenticatedService>(httpClient, settings);

        var result = await fixture.GetAuthenticatedWithTokenInMethod("token-from-parameter");

        handler.VerifyNoOutstandingExpectation();
        await Assert.That(result).IsEqualTo("Ok");
    }
}
