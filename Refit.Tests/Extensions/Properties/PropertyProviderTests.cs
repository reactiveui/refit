using System;
using System.Collections.Generic;
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
        [AttributeUsage(AttributeTargets.Method)]
        private class RetryAttribute : Attribute
        {
            public int Times { get; }

            public RetryAttribute(int times) => Times = times;
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

        public interface IMyService
        {
            [Retry(3)]
            [Get("/get-api-response-with-result")]
            Task<ApiResponse<MyDummyObject>> GetApiResponseWithResult([Property] string someProperty);

            [Get("/get-with-result")]
            Task<MyDummyObject> GetWithResult([Property] string someProperty);
        }

        [Fact]
        public async Task GivenNullPropertyProvider_WhenInvokeRefit_Succeeds()
        {
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler, PropertyProviderFactory = null!
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, "http://api/get-api-response-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponseWithResult(propertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result1);
            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(propertyValue, result2.RequestMessage?.Properties["someProperty"]);
            Assert.Equal(1, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenMethodInfoPropertyProvider_WhenInvokeRefit_MethodInfoPopulatedIntoProperties()
        {
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};
            var methodInfoKey = "methodInfo";

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = PropertyProviderFactory.MethodInfoPropertyProvider
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, "http://api/get-api-response-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponseWithResult(propertyValue);

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
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};
            var methodInfoKey = "methodInfo";

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = PropertyProviderFactory.NullPropertyProvider
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, "http://api/get-api-response-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponseWithResult(propertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result1);
            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(propertyValue, result2.RequestMessage?.Properties["someProperty"]);
            Assert.Equal(1, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenPropertyProviderOverwritesAProperty_WhenInvokeRefit_LastWriteWins()
        {
            var propertyValue = "somePropertyValue";
            var overwrittenPropertyValue = "aDifferentPropertyValue";
            var existingPropertyKey = "someProperty";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = (methodInfo, targetType) => new Dictionary<string, object>
                {
                    {existingPropertyKey, overwrittenPropertyValue}
                }
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, "http://api/get-api-response-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponseWithResult(propertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result1);
            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(overwrittenPropertyValue, result2.RequestMessage?.Properties[existingPropertyKey]);
            Assert.Equal(1, result2.RequestMessage?.Properties.Count);
        }

        [Fact]
        public async Task GivenPropertyProviderThrowsException_WhenInvokeRefit_PopulatesExceptionToProperties()
        {
            var propertyValue = "somePropertyValue";
            var existingPropertyKey = "someProperty";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};
            var tantrum = new Exception("I'm very unhappy about this...kaboom!");

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = (methodInfo, targetType) => throw tantrum
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, "http://api/get-api-response-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponseWithResult(propertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result1);
            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(2, result2.RequestMessage?.Properties.Count);
            Assert.Equal(propertyValue, result2.RequestMessage?.Properties[existingPropertyKey]);
            Assert.Equal(tantrum,
                result2.RequestMessage?.Properties[HttpRequestMessageOptions.PropertyProviderException]);
        }

        [Fact]
        public async Task GivenCustomAttributePropertyProvider_WhenInvokeRefit_CustomAttributesPopulatedIntoProperties()
        {
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject {SomeValue = "AValue", AnotherValue = 1};
            var methodInfoKey = "methodInfo";

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = PropertyProviderFactory.CustomAttributePropertyProvider
            };

            handler.Expect(HttpMethod.Get, "http://api/get-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));
            handler.Expect(HttpMethod.Get, "http://api/get-api-response-with-result")
                .Respond(HttpStatusCode.OK, settings.ContentSerializer.ToHttpContent(dummyObject));

            var fixture = RestService.For<IMyService>("http://api", settings);

            var result1 = await fixture.GetWithResult(propertyValue);
            var result2 = await fixture.GetApiResponseWithResult(propertyValue);

            handler.VerifyNoOutstandingExpectation();

            Assert.Equal(dummyObject, result1);
            Assert.Equal(dummyObject, result2.Content);
            Assert.Equal(2, result2.RequestMessage?.Properties.Count);
#if NET5_0_OR_GREATER
            RetryAttribute retryAttribute = default;
            Assert.True(result2.RequestMessage.Options.TryGetValue(
                new HttpRequestOptionsKey<RetryAttribute>(nameof(RetryAttribute)), out retryAttribute));
            Assert.Equal(3, retryAttribute.Times);
#endif
        }
    }
}
