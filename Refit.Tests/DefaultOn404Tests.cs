using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests
{
    public class DefaultOn404Tests
    {
        private DefaultOn404Api fixture;

        public DefaultOn404Tests()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.When(HttpMethod.Get, "https://404/object").Respond(HttpStatusCode.NotFound, null, string.Empty);
            mockHttp.When(HttpMethod.Get, "https://404/int").Respond(HttpStatusCode.NotFound, null, string.Empty);

            fixture = RestService.For<DefaultOn404Api>("https://404", settings);
        }

        [Fact]
        public async Task ReferenceTypesReturnsNull()
        {
            var result = await fixture.GetObjectAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task ValueTypesReturnsDefaultValue()
        {
            var result = await fixture.GetInt32Async();

            Assert.Equal(default(int), result);
        }
    }
}
