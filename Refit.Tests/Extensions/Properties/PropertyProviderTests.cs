using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Refit.Extensions.Properties;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests.Extensions.Properties
{
    public class PropertyProviderTests
    {
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
        private class RetryAttribute : Attribute
        {
            public int Times { get; }

            public RetryAttribute(int times) => Times = times;

            protected bool Equals(RetryAttribute other)
            {
                return base.Equals(other) && Times == other.Times;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((RetryAttribute) obj);
            }
        }

        public class MyDummyObject
        {
            protected bool Equals(MyDummyObject other)
            {
                return SomeValue == other.SomeValue && AnotherValue == other.AnotherValue;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MyDummyObject)obj);
            }

            public string SomeValue { get; set; }
            public int AnotherValue { get; set; }
        }

        private const int RetryTargetInterface = 0;
        private const int RetryPostMutlipart = 1;
        private const int RetryPutHeaders = 2;
        private const int RetryGetApiResponse = 3;
        private const int RetryDeleteQueryUriFormat = 4;

        private const string UrlGetWithResult = "get-with-result";
        private const string UrlPostMultipart = "post-multipart";
        private const string UrlPutHeaders = "put-headers";
        private const string UrlGetApiResponse = "get-api-response";
        private const string UrlDeleteQueryUriFormat = "delete-query-uri-format";
        private const string UrlHeadInterface = "head-interface";

        private const string ParamPropertyKey = "somePropertyKey";
        private const string ParamPropertyValue = "somePropertyValue";
        /*
         * This is a sample of how an end user might use a property provider
         * to feed metadata into a delegating handler in order to control certain behaviors.
         * Polly's integration with HttpClient via HttpClientFactory allows you to set behaviors
         * that apply across the board to an entire HttpClient, but sometimes that isn't granular enough.
         * This feature provides the possibility of more fine-grained control.
         * You could for example still define policies with Polly and store them in a policy registry
         * and then leverage the CustomAttributePropertyProvider in conjunction with an attribute and
         * DelegatingHandler that allows you to select which Polly policy to apply!
         */
        [Retry(RetryTargetInterface)]
        public interface IMyService
        {
            [Get("/" + UrlGetWithResult)]
            Task<MyDummyObject> GetWithResult([Property(ParamPropertyKey)] string someProperty);

            [Retry(RetryPostMutlipart)]
            [Multipart]
            [Post("/" + UrlPostMultipart)]
            Task<ApiResponse<MyDummyObject>> PostMultipart(string multipartValue, [Property(ParamPropertyKey)] string someProperty);

            [Retry(RetryPutHeaders)]
            [Headers("User-Agent: AAA")]
            [Put("/" + UrlPutHeaders)]
            Task<ApiResponse<MyDummyObject>> PutHeaders([Property(ParamPropertyKey)] string someProperty);

            [Retry(RetryGetApiResponse)]
            [Get("/" + UrlGetApiResponse)]
            Task<ApiResponse<MyDummyObject>> GetApiResponse([Property(ParamPropertyKey)] string someProperty);

            [Retry(RetryDeleteQueryUriFormat)]
            [QueryUriFormat(UriFormat.Unescaped)]
            [Delete("/" + UrlDeleteQueryUriFormat)]
            Task<ApiResponse<MyDummyObject>> DeleteQueryUriFormat([Property(ParamPropertyKey)] string someProperty);

            [Head("/" + UrlHeadInterface)]
            Task<ApiResponse<MyDummyObject>> HeadInterface([Property(ParamPropertyKey)] string someProperty);
        }

        public static IEnumerable<object[]> CustomAttributePropertyProviderTestCases()
        {
            Func<IMyService, Task<ApiResponse<MyDummyObject>>> refitMethod;
            Func<MethodInfo, Type, IDictionary<string, object>> propertyProvider = PropertyProviderFactory.CustomAttributePropertyProvider;
            IDictionary<string, object> expectedProperties;

            refitMethod = refitClient => refitClient.GetApiResponse(ParamPropertyValue);
            expectedProperties = new Dictionary<string, object>
            {
                {nameof(RetryAttribute), new RetryAttribute(RetryGetApiResponse)},
                {ParamPropertyKey, ParamPropertyValue}
            };
            yield return new object[]
            {
                HttpMethod.Get, UrlGetApiResponse, refitMethod, propertyProvider, expectedProperties
            };

            refitMethod = refitClient => refitClient.PostMultipart("multipart", ParamPropertyValue);
            expectedProperties = new Dictionary<string, object>
            {
                {nameof(RetryAttribute), new RetryAttribute(RetryPostMutlipart)},
                {ParamPropertyKey, ParamPropertyValue}
            };
            yield return new object[]
            {
                HttpMethod.Post, UrlPostMultipart, refitMethod, propertyProvider, expectedProperties
            };

            refitMethod = refitClient => refitClient.PutHeaders(ParamPropertyValue);
            expectedProperties = new Dictionary<string, object>
            {
                {nameof(RetryAttribute), new RetryAttribute(RetryPutHeaders)},
                {ParamPropertyKey, ParamPropertyValue}
            };
            yield return new object[]
            {
                HttpMethod.Put, UrlPutHeaders, refitMethod, propertyProvider, expectedProperties
            };

            refitMethod = refitClient => refitClient.DeleteQueryUriFormat(ParamPropertyValue);
            expectedProperties = new Dictionary<string, object>
            {
                {nameof(RetryAttribute), new RetryAttribute(RetryDeleteQueryUriFormat)},
                {ParamPropertyKey, ParamPropertyValue}
            };
            yield return new object[]
            {
                HttpMethod.Delete, UrlDeleteQueryUriFormat, refitMethod, propertyProvider, expectedProperties
            };

            refitMethod = refitClient => refitClient.HeadInterface(ParamPropertyValue);
            expectedProperties = new Dictionary<string, object>
            {
                {nameof(RetryAttribute), new RetryAttribute(RetryTargetInterface)},
                {ParamPropertyKey, ParamPropertyValue}
            };
            yield return new object[]
            {
                HttpMethod.Head, UrlHeadInterface, refitMethod, propertyProvider, expectedProperties
            };
        }

        [Theory]
        [MemberData(nameof(CustomAttributePropertyProviderTestCases))]
        public async Task GivenCustomAttributePropertyProvider_WhenInvokeRefitReturningTaskApiResponseT_CustomAttributesPopulatedIntoProperties(
            HttpMethod httpMethod,
            string url,
            Func<IMyService, Task<ApiResponse<MyDummyObject>>> refitMethod,
            Func<MethodInfo, Type, IDictionary<string, object>> propertyProvider,
            IDictionary<string, object> expectedProperties)
        {
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = propertyProvider
            };
            handler.Expect(httpMethod, $"http://api/{url}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            var fixture = RestService.For<IMyService>("http://api", settings);

            var result = await refitMethod(fixture);

            handler.VerifyNoOutstandingExpectation();
            Assert.Equal(dummyObject, result.Content);
#if NET5_0_OR_GREATER
            Assert.Equal(expectedProperties.Count, result.RequestMessage.Options.Count());

            foreach (var expectedProperty in expectedProperties)
            {
                Assert.True(result.RequestMessage.Options.TryGetValue(new HttpRequestOptionsKey<object?>(expectedProperty.Key), out var actualPropertyValue));
                Assert.Equal(expectedProperty.Value, actualPropertyValue);
            }
#else
            Assert.Equal(expectedProperties.Count, result.RequestMessage.Properties.Count);

            foreach (var expectedProperty in expectedProperties)
            {
                var actualPropertyValue = result.RequestMessage.Properties[expectedProperty.Key];
                Assert.NotNull(actualPropertyValue);
                Assert.Equal(expectedProperty.Value, actualPropertyValue);
            }
#endif
        }

        [Fact]
        public async Task GivenNullPropertyProvider_WhenInvokeRefit_Succeeds()
        {
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler, PropertyProviderFactory = null!
            };

            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetApiResponse}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result2 = await fixture.GetApiResponse(ParamPropertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result2.Content);

            Assert.Equal(ParamPropertyValue, result2.RequestMessage?.Properties[ParamPropertyKey]);
            Assert.Equal(1, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenMethodInfoPropertyProvider_WhenInvokeRefit_MethodInfoPopulatedIntoProperties()
        {
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = PropertyProviderFactory.MethodInfoPropertyProvider
            };

            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetWithResult}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetApiResponse}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponse(propertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result1);
            Assert.Equal(dummyObject, result2.Content);
            Assert.IsAssignableFrom<MethodInfo>(
                result2.RequestMessage?.Properties[HttpRequestMessageOptions.MethodInfo]);
            Assert.Equal(2, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenPropertyProviderReturnsNull_WhenInvokeRefit_Success()
        {
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = PropertyProviderFactory.NullPropertyProvider
            };

            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetApiResponse}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result2 = await fixture.GetApiResponse(ParamPropertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(ParamPropertyValue, result2.RequestMessage?.Properties[ParamPropertyKey]);
            Assert.Equal(1, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenPropertyProviderOverwritesAProperty_WhenInvokeRefit_LastWriteWins()
        {
            var overwrittenPropertyValue = "aDifferentPropertyValue";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = (methodInfo, targetType) => new Dictionary<string, object>
                {
                    {ParamPropertyKey, overwrittenPropertyValue}
                }
            };

            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetApiResponse}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result2 = await fixture.GetApiResponse(ParamPropertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(overwrittenPropertyValue, result2.RequestMessage?.Properties[ParamPropertyKey]);
            Assert.Equal(1, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenPropertyProviderThrowsException_WhenInvokeRefit_PopulatesExceptionToProperties()
        {
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};
            var tantrum = new Exception("I'm very unhappy about this...kaboom!");

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = (methodInfo, targetType) => throw tantrum
            };

            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetApiResponse}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result2 = await fixture.GetApiResponse(ParamPropertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result2.Content);

            Assert.Equal(2, result2.RequestMessage?.Properties.Count);
            Assert.Equal(ParamPropertyValue, result2.RequestMessage?.Properties[ParamPropertyKey]);
            Assert.Equal(tantrum, result2.RequestMessage?.Properties[HttpRequestMessageOptions.PropertyProviderException]);
        }

        [Fact]
        public async Task GivenCustomAttributePropertyProvider_WhenInvokeRefitReturningTaskT_Success()
        {
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = PropertyProviderFactory.CustomAttributePropertyProvider
            };

            handler.Expect(HttpMethod.Get, $"http://api/{UrlGetWithResult}")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(ParamPropertyValue);

            handler.VerifyNoOutstandingExpectation();
            Assert.Equal(dummyObject, result1);
        }
    }
}
