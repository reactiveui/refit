using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using NUnit.Framework;

namespace Refit.Tests
{
    [Headers("User-Agent: RefitTestClient", "Api-Version: 1")]
    public interface IRestMethodInfoTests
    {
        [Get("@)!@_!($_!@($\\\\|||::::")]
        Task<string> GarbagePath();

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffMissingParameters();

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParam(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithQueryParam(int id, string search);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithAlias([AliasAs("id")] int anId);

        [Get("/foo/bar/{width}x{height}")]
        Task<string> FetchAnImage(int width, int height);

        [Get("/foo/bar/{id}")]
        IObservable<string> FetchSomeStuffWithBody([AliasAs("id")] int anId, [Body] Dictionary<int, string> theData);

        [Post("/foo/bar/{id}")]
        IObservable<string> PostSomeUrlEncodedStuff([AliasAs("id")] int anId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> theData);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: 2 ")]
        Task<string> FetchSomeStuffWithHardcodedHeaders(int id);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);
        
        [Post("/foo/{id}")]
        Task<bool> OhYeahValueTypes(int id, [Body] int whatever);

        [Post("/foo/{id}")]
        Task VoidPost(int id);

        [Post("/foo/{id}")]
        string AsyncOnlyBuddy(int id);
    }

    [TestFixture]
    public class RestMethodInfoTests
    {
        [Test]
        public void GarbagePathsShouldThrow()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "GarbagePath"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void MissingParametersShouldBlowUp()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffMissingParameters"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void ParameterMappingSmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuff"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void ParameterMappingWithQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithQueryParam"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual("search", fixture.QueryParameterMap[1]);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void ParameterMappingWithHardcodedQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedQueryParam"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void AliasMappingShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithAlias"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void MultipleParametersPerSegmentShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchAnImage"));
            Assert.AreEqual("width", fixture.ParameterMap[0]);
            Assert.AreEqual("height", fixture.ParameterMap[1]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);
        }

        [Test]
        public void FindTheBodyParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithBody"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.IsNotNull(fixture.BodyParameterInfo);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.AreEqual(1, fixture.BodyParameterInfo.Item2);
        }

        [Test]
        public void AllowUrlEncodedContent()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PostSomeUrlEncodedStuff"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.IsNotNull(fixture.BodyParameterInfo);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.AreEqual(BodySerializationMethod.UrlEncoded, fixture.BodyParameterInfo.Item1);
        }

        [Test]
        public void HardcodedHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedHeaders"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);

            Assert.IsTrue(fixture.Headers.ContainsKey("Api-Version"), "Headers include Api-Version header");
            Assert.AreEqual("2", fixture.Headers["Api-Version"]);
            Assert.IsTrue(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.AreEqual(2, fixture.Headers.Count);
        }

        [Test]
        public void DynamicHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithDynamicHeader"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.IsNull(fixture.BodyParameterInfo);

            Assert.AreEqual("Authorization", fixture.HeaderParameterMap[1]);
            Assert.IsTrue(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.AreEqual(2, fixture.Headers.Count);
        }

        [Test]
        public void ValueTypesDontBlowUp()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "OhYeahValueTypes"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);
            Assert.AreEqual(0, fixture.QueryParameterMap.Count);
            Assert.AreEqual(BodySerializationMethod.Json, fixture.BodyParameterInfo.Item1);
            Assert.AreEqual(1, fixture.BodyParameterInfo.Item2);

            Assert.AreEqual(typeof(bool), fixture.SerializedReturnType);
        }

        [Test]
        public void ReturningTaskShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "VoidPost"));
            Assert.AreEqual("id", fixture.ParameterMap[0]);

            Assert.AreEqual(typeof(Task), fixture.ReturnType);
            Assert.AreEqual(typeof(void), fixture.SerializedReturnType);
        }

        [Test]
        public void SyncMethodsShouldThrow()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "AsyncOnlyBuddy"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.IsFalse(shouldDie);
        }
    }

    [Headers("User-Agent: RefitTestClient", "Api-Version: 1")]
    public interface IDummyHttpApi
    {
        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuff(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedQueryParameter(int id);

        [Get("/foo/bar/{id}?baz=bamf")]
        Task<string> FetchSomeStuffWithHardcodedAndOtherQueryParameters(int id, [AliasAs("search_for")] string searchQuery);

        [Get("/{id}/{width}x{height}/foo")]
        Task<string> FetchSomethingWithMultipleParametersPerSegment(int id, int width, int height);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: 2")]
        Task<string> FetchSomeStuffWithHardcodedHeader(int id);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version")]
        Task<string> FetchSomeStuffWithNullHardcodedHeader(int id);

        [Get("/foo/bar/{id}")]
        [Headers("Api-Version: ")]
        Task<string> FetchSomeStuffWithEmptyHardcodedHeader(int id);

        [Get("/foo/bar/{id}")]
        [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithCustomHeader(int id, [Header("X-Emoji")] string custom);

        [Get("/string")]
        Task<string> FetchSomeStuffWithoutFullPath();

        [Get("/void")]
        Task FetchSomeStuffWithVoid();

        [Post("/foo/bar/{id}")]
        Task<string> PostSomeUrlEncodedStuff(int id, [Body(BodySerializationMethod.UrlEncoded)] object content);

        [Post("/foo/bar/{id}")]
        Task<string> PostSomeAliasedUrlEncodedStuff(int id,[Body(BodySerializationMethod.UrlEncoded)] SomeRequestData content);

        string SomeOtherMethod();

        [Put("/foo/bar/{id}")]
        Task PutSomeContentWithAuthorization(int id, [Body] object content, [Header("Authorization")] string authorization);

        [Put("/foo/bar/{id}")]
        Task<string> PutSomeStuffWithDynamicContentType(int id, [Body] string content, [Header("Content-Type")] string contentType);

        [Post("/foo/bar/{id}")]
        Task<bool> PostAValueType(int id, [Body] Guid? content);
    }

    public class SomeRequestData
    {
        [AliasAs("rpn")]
        public int ReadablePropertyName { get; set; }
    }

    public class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage RequestMessage { get; private set; }
        public int MessagesSent { get; set; }
        public string Content { get; set; }

        public TestHttpMessageHandler(string content = "test")
        {
            Content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMessage = request;
            MessagesSent++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Content) });
        }
    }

    public class TestUrlParameterFormatter : IUrlParameterFormatter
    {
        readonly string constantParameterOutput;

        public TestUrlParameterFormatter(string constantOutput)
        {
            constantParameterOutput = constantOutput;
        }

        public string Format(object value, ParameterInfo parameterInfo)
        {
            return constantParameterOutput;
        }
    }

    [TestFixture]
    public class RequestBuilderTests
    {
        [Test]
        public void MethodsThatDontHaveAnHttpMethodShouldFail()
        {
            var failureMethods = new[] { 
                "SomeOtherMethod",
                "weofjwoeijfwe",
                null,
            };

            var successMethods = new[] {
                "FetchSomeStuff",
            };

            foreach (var v in failureMethods) {
                bool shouldDie = true;

                try {
                    var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception) {
                    shouldDie = false;
                }
                Assert.IsFalse(shouldDie);
            }

            foreach (var v in successMethods) {
                bool shouldDie = false;

                try {
                    var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception ex) {
                    shouldDie = true;
                }

                Assert.IsFalse(shouldDie);
            }
        }

        [Test]
        public void HardcodedQueryParamShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedQueryParameter");
            var output = factory(new object[] { 6 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.AreEqual("/foo/bar/6?baz=bamf", uri.PathAndQuery);
        }

        [Test]
        public void ParameterizedQueryParamsShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedAndOtherQueryParameters");
            var output = factory(new object[] { 6, "foo" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.AreEqual("/foo/bar/6?baz=bamf&search_for=foo", uri.PathAndQuery);
        }

        [Test]
        public void MultipleParametersInTheSameSegmentAreGeneratedProperly()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomethingWithMultipleParametersPerSegment");
            var output = factory(new object[] { 6, 1024, 768 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.AreEqual("/6/1024x768/foo", uri.PathAndQuery);
        }

        [Test]
        public void HardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.IsTrue(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.IsTrue(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.AreEqual("2", output.Headers.GetValues("Api-Version").Single());
        }

        [Test]
        public void EmptyHardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithEmptyHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.IsTrue(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.IsTrue(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.AreEqual("", output.Headers.GetValues("Api-Version").Single());
        }
        [Test]
        public void NullHardcodedHeadersShouldNotBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithNullHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.IsTrue(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.AreEqual("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.IsFalse(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
        }

        [Test]
        public void DynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.IsNotNull(output.Headers.Authorization, "Headers include Authorization header");
            Assert.AreEqual("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }

        [Test]
        public void CustomDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, ":joy_cat:" });

            Assert.IsTrue(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.AreEqual(":joy_cat:", output.Headers.GetValues("X-Emoji").First());
        }

        [Test]
        public void EmptyDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, "" });

            Assert.IsTrue(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.AreEqual("", output.Headers.GetValues("X-Emoji").First());
        }

        [Test]
        public void NullDynamicHeaderShouldNotBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, null });

            Assert.IsNull(output.Headers.Authorization, "Headers include Authorization header");
        }

        [Test]
        public void HttpClientShouldPrefixedAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithoutFullPath");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/foo/bar") }, new object[0]);
            task.Wait();

            Assert.AreEqual("http://api/foo/bar/string", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Test]
        public void HttpClientForVoidMethodShouldPrefixedAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithVoid");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/foo/bar") }, new object[0]);
            task.Wait();

            Assert.AreEqual("http://api/foo/bar/void", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Test]
        public void HttpClientShouldNotPrefixEmptyAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuff");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { 42 });
            task.Wait();

            Assert.AreEqual("http://api/foo/bar/42", testHttpMessageHandler.RequestMessage.RequestUri.ToString());            
        }

        [Test]
        public void DontBlowUpWithDynamicAuthorizationHeaderAndContent() 
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PutSomeContentWithAuthorization");
            var output = factory(new object[] { 7, new { Octocat = "Dunetocat" }, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.IsNotNull(output.Headers.Authorization, "Headers include Authorization header");
            Assert.AreEqual("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }

        [Test]
        public void SuchFlexibleContentTypeWow()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PutSomeStuffWithDynamicContentType");
            var output = factory(new object[] { 7, "such \"refit\" is \"amaze\" wow", "text/dson" });

            Assert.IsNotNull(output.Content, "Request has content");
            Assert.IsNotNull(output.Content.Headers.ContentType, "Headers include Content-Type header");
            Assert.AreEqual("text/dson", output.Content.Headers.ContentType.MediaType, "Content-Type header has the expected value");
        }

        [Test]
        public async Task BodyContentGetsUrlEncoded() 
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PostSomeUrlEncodedStuff");
            var output = factory(
                new object[] {
                    6, 
                    new {
                        Foo = "Something", 
                        Bar = 100, 
                        Baz = default(string)
                    }
                });

            string content = await output.Content.ReadAsStringAsync();

            Assert.AreEqual("Foo=Something&Bar=100&Baz=", content);
        }

        [Test]
        public async Task FormFieldGetsAliased()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PostSomeAliasedUrlEncodedStuff");
            var output = factory(
                new object[] {
                    6, 
                    new SomeRequestData {
                        ReadablePropertyName = 99
                    }
                });

            string content = await output.Content.ReadAsStringAsync();

            Assert.AreEqual("rpn=99", content);
        }

        [Test]
        public async Task CustomParmeterFormatter()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter") };
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi), settings);

            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
            var output = factory(new object[] { 5 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.AreEqual("/foo/bar/custom-parameter", uri.PathAndQuery);
        }

        [Test]
        public async Task ICanPostAValueTypeIfIWantYoureNotTheBossOfMe()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PostAValueType");
            var guid = Guid.NewGuid();
            var expected = string.Format("\"{0}\"", guid);
            var output = factory(new object[] { 7, guid });

            var content = await output.Content.ReadAsStringAsync();
            
            Assert.AreEqual(expected, content);
        }
    }
}
