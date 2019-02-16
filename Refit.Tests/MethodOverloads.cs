using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests
{
    public interface IUseOverloadedMethods
    {
        [Get("")]
        Task<string> Get();

        [Get("/status/{httpstatuscode}")]
        Task<HttpResponseMessage> Get(int httpstatuscode);
    }

    public interface IUseOverloadedGenericMethods<TResponse, in TParam, in THeader>
        where TResponse : class
        where THeader : struct
    {
        [Get("")]
        Task<string> Get();

        [Get("/get")]
        Task<TResponse> Get(TParam param, [Header("X-Refit")] THeader header);

        [Get("/get")]
        Task<TResponse> Get(THeader param, [Header("X-Refit")] TParam header);

        [Get("/status/{httpstatuscode}")]
        Task<HttpResponseMessage> Get(int httpstatuscode);

        [Get("/get")]
        Task<TValue> Get<TValue>(int someVal);

        [Get("/get")]
        Task<TValue> Get<TValue, TInput>(TInput input);

        [Get("/get")]
        Task Get<TInput1, TInput2>(TInput1 input1, TInput2 input2);
    }


    public class MethodOverladTests
    {
        [Fact]
        public async Task BasicMethodOverloadTest()
        {

            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/status/403")
                .Respond(HttpStatusCode.Forbidden);

            var fixture = RestService.For<IUseOverloadedMethods>("https://httpbin.org/", settings);
            var plainText = await fixture.Get();

            var resp = await fixture.Get(403);

            Assert.True(!String.IsNullOrWhiteSpace(plainText));
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        }

        [Fact]
        public async Task GenericMethodOverloadTest1()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);
            var plainText = await fixture.Get();

            Assert.True(!string.IsNullOrWhiteSpace(plainText));
        }

        [Fact]
        public async Task GenericMethodOverloadTest2()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };


            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/status/403")
                .Respond(HttpStatusCode.Forbidden);


            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);

            var resp = await fixture.Get(403);

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task GenericMethodOverloadTest3()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                    .WithQueryString("someVal", "201")
                    .Respond("application/json", "some-T-value");


            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);

            var result = await fixture.Get<string>(201);

            Assert.Equal("some-T-value", result);
        }

        [Fact]
        public async Task GenericMethodOverloadTest4()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .WithHeaders("X-Refit", "99")
                .WithQueryString("param", "foo")
                .Respond("application/json", "{'url': 'https://httpbin.org/get', 'args': {'param': 'foo'}}");

            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);

            var result = await fixture.Get("foo", 99);

            Assert.Equal("foo", result.Args["param"]);
        }

        [Fact]
        public async Task GenericMethodOverloadTest5()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };


            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .WithHeaders("X-Refit", "foo")
                .WithQueryString("param", "99")
                .Respond("application/json", "{'url': 'https://httpbin.org/get', 'args': {'param': '99'}}");

            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);

            var result = await fixture.Get(99, "foo");

            Assert.Equal("99", result.Args["param"]);
        }

        [Fact]
        public async Task GenericMethodOverloadTest6()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                    .WithQueryString("input", "99")
                    .Respond("application/json", "generic-output");

            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);

            var result = await fixture.Get<string, int>(99);

            Assert.Equal("generic-output", result);
        }

        [Fact]
        public async Task GenericMethodOverloadTest7()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                    .WithQueryString(new Dictionary<string, string>()
                    {
                        { "input1", "str" },
                        { "input2", "3" }
                     })
                    .Respond("application/json", "Ok");


            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);

            await fixture.Get<string, int>("str", 3);

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}

