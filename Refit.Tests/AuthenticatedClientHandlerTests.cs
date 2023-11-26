using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

using Refit; // for the code gen
using Xunit;

namespace Refit.Tests
{
    public class AuthenticatedClientHandlerTests
    {
        public interface IMyAuthenticatedService
        {
            [Get("/unauth")]
            Task<string> GetUnauthenticated();

            [Get("/auth")]
            [Headers("Authorization: Bearer")]
            Task<string> GetAuthenticated();

            [Get("/auth")]
            Task<string> GetAuthenticatedWithTokenInMethod([Authorize("Bearer")] string token);

            [Get("/auth")]
            Task<string> GetAuthenticatedWithAuthorizeAttributeAndHeaderCollection(
                [Authorize("Bearer")] string token,
                [HeaderCollection] IDictionary<string, string> headers
            );

            [Get("/auth")]
            Task<string> GetAuthenticatedWithTokenInHeaderCollection(
                [HeaderCollection] IDictionary<string, string> headers
            );

            [Post("/auth/{id}")]
            Task<string> PostAuthenticatedWithTokenInHeaderCollection(
                int id,
                SomeRequestData content,
                [HeaderCollection] IDictionary<string, string> headers
            );
        }

        public interface IInheritedAuthenticatedServiceWithHeaders
            : IAuthenticatedServiceWithHeaders
        {
            [Get("/get-inherited-thing")]
            Task<string> GetInheritedThing();
        }

        [Headers("Authorization: Bearer")]
        public interface IAuthenticatedServiceWithHeaders
        {
            [Get("/get-base-thing")]
            Task<string> GetThingFromBase();
        }

        [Fact]
        public void DefaultHandlerIsHttpClientHandler()
        {
            var handler = new AuthenticatedHttpClientHandler(
                ((_, _) => Task.FromResult(string.Empty))
            );

            Assert.IsType<HttpClientHandler>(handler.InnerHandler);
        }

        [Fact]
        public void NullTokenGetterThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthenticatedHttpClientHandler(null));
        }

        [Fact]
        public async void AuthenticatedHandlerIgnoresUnAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (_, __) => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler
                .Expect(HttpMethod.Get, "http://api/unauth")
                .With(msg => msg.Headers.Authorization == null)
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

            var result = await fixture.GetUnauthenticated();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (_, __) => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler
                .Expect(HttpMethod.Get, "http://api/auth")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

            var result = await fixture.GetAuthenticated();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerWithParamUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (request, _) => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler
                .Expect(HttpMethod.Get, "http://api/auth")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

            var result = await fixture.GetAuthenticated();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerWithTokenInParameterUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings() { HttpMessageHandlerFactory = () => handler };

            handler
                .Expect(HttpMethod.Get, "http://api/auth")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

            var result = await fixture.GetAuthenticatedWithTokenInMethod("tokenValue");

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerWithTokenInHeaderCollectionUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings() { HttpMessageHandlerFactory = () => handler };

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

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerWithAuthorizeAttributeAndHeaderCollectionUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings() { HttpMessageHandlerFactory = () => handler };

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
                headerCollectionHeaders
            );

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerWithDuplicatedAuthorizationHeaderUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings() { HttpMessageHandlerFactory = () => handler };

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
                headerCollectionHeaders
            );

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthenticatedHandlerPostTokenInHeaderCollectionUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings() { HttpMessageHandlerFactory = () => handler };

            var id = 1;
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
                headers
            );

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthentictedMethodFromBaseClassWithHeadersAttributeUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (_, __) => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler
                .Expect(HttpMethod.Get, "http://api/get-base-thing")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeaders>(
                "http://api",
                settings
            );

            var result = await fixture.GetThingFromBase();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthentictedMethodFromInheritedClassWithHeadersAttributeUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = (_, __) => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler
                .Expect(HttpMethod.Get, "http://api/get-inherited-thing")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeaders>(
                "http://api",
                settings
            );

            var result = await fixture.GetInheritedThing();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }
    }
}
