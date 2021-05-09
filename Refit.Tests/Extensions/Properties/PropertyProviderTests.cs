using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Tests.Extensions.Exceptions;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests.Extensions.Properties
{
    public class PropertyProviderTests
    {
        public interface IMyService
        {
            [Get("/get-with-result")]
            Task<ApiResponse<string>> GetWithResult();

            [Put("/put-without-result")]
            Task PutWithoutResult();
        }

        [Fact]
        public async Task GivenNullPropertyProvider_WhenInvokeRefit_Succeeds()
        {
            var message = "result";
            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = null!
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, new StringContent(message));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result = await fixture.GetWithResult();

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(message, result.Content);
        }
    }
}
