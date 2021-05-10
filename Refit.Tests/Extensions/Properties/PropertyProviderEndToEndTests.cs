using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests.Extensions.Properties
{
    public class PropertyProviderEndToEndTests
    {
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
                return Equals((MyDummyObject) obj);
            }

            public string SomeValue { get; set; }
            public int AnotherValue { get; set; }
        }
        public interface IMyService
        {
            [Get("/get-api-response-with-result")]
            Task<ApiResponse<MyDummyObject>> GetApiResponseWithResult([Property] string someProperty);

            [Get("/get-with-result")]
            Task<MyDummyObject> GetWithResult([Property] string someProperty);
        }

        [Fact]
        public async Task GivenNullPropertyProvider_WhenInvokeRefit_Succeeds()
        {
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject
            {
                SomeValue = "AValue",
                AnotherValue = 1
            };

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = null!
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
        }

        [Fact]
        public async Task GivenMethodInfoPropertyProvider_WhenInvokeRefit_MethodInfoPopulatedIntoProperties()
        {
            var propertyValue = "somePropertyValue";
            var dummyObject = new MyDummyObject
            {
                SomeValue = "AValue",
                AnotherValue = 1
            };
            var methodInfoKey = "methodInfo";

            var handler = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => handler,
                PropertyProviderFactory = (methodInfo, targetType) => new Dictionary<string, object>
                {
                    {methodInfoKey, methodInfo}
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
            Assert.IsAssignableFrom<MethodInfo>(result2.RequestMessage?.Properties[methodInfoKey]);
        }
    }
}
