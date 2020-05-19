using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests
{
    public class ErrorHandlerTests
    {
        [Fact]
        public async Task ApiResponseErrorWithDefaultHandler()
        {
            var mockHttp = new MockHttpMessageHandler();
            var mockErrorHandler = new MockDefaultErrorHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }),
                ErrorHandler = mockErrorHandler
            };

            mockHttp.Expect(HttpMethod.Post, "https://api.github.com/users")
                .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);


            using var response = await fixture.CreateUserWithMetadata(new User { Name = "foo" });
            Assert.False(response.IsSuccessStatusCode);
            Assert.NotNull(response.Error);
            Assert.True(mockErrorHandler.HandleErrorAsyncCalled);

            var apiException = (ApiException)response.Error;
            var errors = await apiException.GetContentAsAsync<ErrorResponse>();

            Assert.Contains("error1", errors.Errors);
            Assert.Contains("message", errors.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CancellableTaskErrorWithDefaultHandler()
        {
            var mockHttp = new MockHttpMessageHandler();
            var mockErrorHandler = new MockDefaultErrorHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }),
                ErrorHandler = mockErrorHandler
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            ApiException apiException = null;
            try
            {
                await fixture.GetUser("octocat");
            }
            catch (ApiException exception)
            {
                apiException = exception;
            }
            Assert.NotNull(apiException);
            Assert.True(mockErrorHandler.HandleErrorAsyncCalled);
            var errors = await apiException.GetContentAsAsync<ErrorResponse>();

            Assert.Contains("error1", errors.Errors);
            Assert.Contains("message", errors.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }


        [Fact]
        public async Task ObservableErrorWithDefaultHandler()
        {
            var mockHttp = new MockHttpMessageHandler();
            var mockErrorHandler = new MockDefaultErrorHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }),
                ErrorHandler = mockErrorHandler
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            ApiException apiException = null;
            try
            {
                var user = await fixture.GetUserObservable("octocat")
                    .Timeout(TimeSpan.FromSeconds(10));
                Assert.Null(user);
            }
            catch (ApiException exception)
            {
                apiException = exception;
            }
            Assert.NotNull(apiException);
            Assert.True(mockErrorHandler.HandleErrorAsyncCalled);
            var errors = await apiException.GetContentAsAsync<ErrorResponse>();

            Assert.Contains("error1", errors.Errors);
            Assert.Contains("message", errors.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task VoidTaskErrorWithDefaultHandler()
        {
            var mockHttp = new MockHttpMessageHandler();
            var mockErrorHandler = new MockDefaultErrorHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }),
                ErrorHandler = mockErrorHandler
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
                .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");


            var fixture = RestService.For<TestNested.INestedGitHubApi>("https://api.github.com", settings);

            ApiException apiException = null;
            try
            {
                await fixture.NothingToSeeHere();
            }
            catch (ApiException exception)
            {
                apiException = exception;
            }
            Assert.NotNull(apiException);
            Assert.True(mockErrorHandler.HandleErrorAsyncCalled);
            var errors = await apiException.GetContentAsAsync<ErrorResponse>();

            Assert.Contains("error1", errors.Errors);
            Assert.Contains("message", errors.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CancellableTaskOverrideErrorHandler()
        {
            var mockHttp = new MockHttpMessageHandler();
            var mockErrorHandler = new MockSampleErrorHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }),
                ErrorHandler = mockErrorHandler
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            MockSampleErrorHandler.SampleException sampleException = null;
            try
            {
                await fixture.GetUser("octocat");
            }
            catch (MockSampleErrorHandler.SampleException exception)
            {
                sampleException = exception;
            }
            Assert.NotNull(sampleException);
            Assert.True(mockErrorHandler.HandleErrorAsyncCalled);

            Assert.Contains("error1", sampleException.Errors);
            Assert.Contains("message", sampleException.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        public class MockDefaultErrorHandler : DefaultErrorHandler
        {
            public bool HandleErrorAsyncCalled { get; set; }
            public override Task<Exception> HandleErrorAsync(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response,
                RefitSettings refitSettings = null)
            {
                HandleErrorAsyncCalled = true;
                return base.HandleErrorAsync(message, httpMethod, response, refitSettings);
            }
        }


        public class MockSampleErrorHandler : DefaultErrorHandler
        {
            public bool HandleErrorAsyncCalled { get; set; }
            public override async Task<Exception> HandleErrorAsync(HttpRequestMessage message, HttpMethod httpMethod, HttpResponseMessage response,
                RefitSettings refitSettings = null)
            {
                var content = await response.Content.ReadAsStringAsync();
                var errors = JsonConvert.DeserializeObject<SampleErrorModel>(content);
                HandleErrorAsyncCalled = true;
                return new SampleException(errors.Errors);
            }

            public class SampleException: Exception
            {
                public SampleException(List<string> errors)
                {
                    Errors = errors;
                }

                public readonly List<string> Errors;
            }
            public class SampleErrorModel
            {
                [JsonProperty("errors")]
                public List<string> Errors { get; set; }
            }
        }
    }
}
