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
        }


        [Fact]
        public void DefaultHandlerIsHttpClientHandler()
        {
            var handler = new AuthenticatedHttpClientHandler((() => Task.FromResult(string.Empty)));

            Assert.IsType<HttpClientHandler>(handler.InnerHandler);
        }

        [Fact]
        public void DefaultHandlerIsHttpClientHandlerWithParam()
        {
            var handler = new AuthenticatedParameterizedHttpClientHandler(((request) => Task.FromResult(string.Empty)));

            Assert.IsType<HttpClientHandler>(handler.InnerHandler);
        }

        [Fact]
        public void NullTokenGetterThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthenticatedHttpClientHandler(null));
        }

        [Fact]
        public void NullTokenGetterThrowsWithParam()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthenticatedParameterizedHttpClientHandler(null));
        }

        [Fact]
        public async void AuthenticatedHandlerIgnoresUnAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = () => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler.Expect(HttpMethod.Get, "http://api/unauth")
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
                AuthorizationHeaderValueGetter = () => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler.Expect(HttpMethod.Get, "http://api/auth")
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
                AuthorizationHeaderValueWithParamGetter = (request) => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler.Expect(HttpMethod.Get, "http://api/auth")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

            var result = await fixture.GetAuthenticated();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }
    }
}
