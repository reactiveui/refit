﻿using System;
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
        }

        public interface IInheritedAuthenticatedServiceWithHeaders : IAuthenticatedServiceWithHeaders
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
            var handler = new AuthenticatedHttpClientHandler(((_, _) => Task.FromResult(string.Empty)));

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


        [Fact]
        public async void AuthenticatedHandlerWithTokenInParameterUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                HttpMessageHandlerFactory = () => handler
            };

            handler.Expect(HttpMethod.Get, "http://api/auth")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IMyAuthenticatedService>("http://api", settings);

            var result = await fixture.GetAuthenticatedWithTokenInMethod("tokenValue");

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }

        [Fact]
        public async void AuthentictedMethodFromBaseClassWithHeadersAttributeUsesAuth()
        {
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings()
            {
                AuthorizationHeaderValueGetter = () => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler.Expect(HttpMethod.Get, "http://api/get-base-thing")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeaders>("http://api", settings);

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
                AuthorizationHeaderValueGetter = () => Task.FromResult("tokenValue"),
                HttpMessageHandlerFactory = () => handler
            };

            handler.Expect(HttpMethod.Get, "http://api/get-inherited-thing")
                .WithHeaders("Authorization", "Bearer tokenValue")
                .Respond("text/plain", "Ok");

            var fixture = RestService.For<IInheritedAuthenticatedServiceWithHeaders>("http://api", settings);

            var result = await fixture.GetInheritedThing();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal("Ok", result);
        }
    }
}
