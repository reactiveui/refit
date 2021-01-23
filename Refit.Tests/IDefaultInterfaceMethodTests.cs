using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using RichardSzalay.MockHttp;

using Xunit;

namespace Refit.Tests
{
    public interface IHaveDims
    {
        [Get("")]
        internal Task<string> GetInternal();

        // DIMs require C# 8.0 which requires .NET Core 3.x or .NET Standard 2.1
#if NETCOREAPP3_1_OR_GREATER
        private Task<string> GetPrivate()
        {
            return GetInternal();
        }

        Task<string> GetDim()
        {
            return GetPrivate();
        }

        static string GetStatic()
        {
            return nameof(IHaveDims);
        }
#endif
    }

    // DIMs require C# 8.0 which requires .NET Core 3.x or .NET Standard 2.1
#if NETCOREAPP3_1_OR_GREATER
    public class DefaultInterfaceMethodTests
    {
        [Fact]
        public async Task InternalInterfaceMemberTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            var fixture = RestService.For<IHaveDims>("https://httpbin.org/", settings);
            var plainText = await fixture.GetInternal();

            Assert.True(!string.IsNullOrWhiteSpace(plainText));
        }

        [Fact]
        public async Task DimTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            var fixture = RestService.For<IHaveDims>("https://httpbin.org/", settings);
            var plainText = await fixture.GetDim();

            Assert.True(!string.IsNullOrWhiteSpace(plainText));
        }

        [Fact]
        public async Task InternalDimTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/")
                .Respond(HttpStatusCode.OK, "text/html", "OK");

            var fixture = RestService.For<IHaveDims>("https://httpbin.org/", settings);
            var plainText = await fixture.GetInternal();

            Assert.True(!string.IsNullOrWhiteSpace(plainText));
        }

        [Fact]
        public void StaticInterfaceMethodTest()
        {
            var plainText = IHaveDims.GetStatic();

            Assert.True(!string.IsNullOrWhiteSpace(plainText));
        }
    }
#endif
}
