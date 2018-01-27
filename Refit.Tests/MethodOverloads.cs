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
        public async Task GenericMethodOverloadTest()
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

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .WithHeaders("X-Refit", "99")
                .WithQueryString("param","foo")
                .Respond("application/json", "{'url': 'https://httpbin.org/get', 'args': {'param': 'foo'}}");

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .WithHeaders("X-Refit", "foo")
                .WithQueryString("param", "99")
                .Respond("application/json", "{'url': 'https://httpbin.org/get', 'args': {'param': '99'}}");


            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("https://httpbin.org/", settings);
            var plainText = await fixture.Get();
            var resp = await fixture.Get(403);
            var result = await fixture.Get("foo", 99);
            var result2 = await fixture.Get(99, "foo");

            Assert.True(!String.IsNullOrWhiteSpace(plainText));
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

            Assert.Equal("foo", result.Args["param"]);
            Assert.Equal("99", result2.Args["param"]);

        }
    }
}

