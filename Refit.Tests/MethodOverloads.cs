using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit;
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

            var fixture = RestService.For<IUseOverloadedMethods>("https://httpbin.org/");
            var plainText = await fixture.Get();

            var resp = await fixture.Get(200);

            Assert.True(!String.IsNullOrWhiteSpace(plainText));
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
 
        }

        [Fact]
        public async Task GenericMethodOverloadTest()
        {
            var fixture = RestService.For<IUseOverloadedGenericMethods<HttpBinGet, string, int>>("http://httpbin.org");
            var plainText = await fixture.Get();
            var resp = await fixture.Get(200);
            var result = await fixture.Get("foo", 99);
            var result2 = await fixture.Get(99, "foo");

            Assert.True(!String.IsNullOrWhiteSpace(plainText));
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            Assert.Equal("foo", result.Args["param"]);
            Assert.Equal("99", result.Headers["X-Refit"]);

            Assert.Equal("foo", result2.Headers["X-Refit"]);
            Assert.Equal("99", result2.Args["param"]);

        }
    }
}

