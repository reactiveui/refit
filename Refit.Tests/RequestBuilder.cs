using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

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
        Task<bool> PullStreamMethod(int id, [Body(buffered: true)] Dictionary<int, string> theData);

        [Post("/foo/{id}")]
        Task VoidPost(int id);

        [Post("/foo/{id}")]
        string AsyncOnlyBuddy(int id);

        [Patch("/foo/{id}")]
        IObservable<string> PatchSomething(int id, [Body] string someAttribute);


        [Post("/foo")]
        Task PostWithBodyDetected(Dictionary<int, string> theData);

        [Get("/foo")]
        Task GetWithBodyDetected(Dictionary<int, string> theData);

        [Put("/foo")]
        Task PutWithBodyDetected(Dictionary<int, string> theData);

        [Patch("/foo")]
        Task PatchWithBodyDetected(Dictionary<int, string> theData);

        [Post("/foo")]
        Task TooManyComplexTypes(Dictionary<int, string> theData, Dictionary<int, string> theData1);

        [Post("/foo")]
        Task ManyComplexTypes(Dictionary<int, string> theData, [Body] Dictionary<int, string> theData1);
    }
    
    public class RestMethodInfoTests
    {

        [Fact]
        public void TooManyComplexTypesThrows()
        {
            var input = typeof(IRestMethodInfoTests);

            Assert.Throws<ArgumentException>(() => {
                var fixture = new RestMethodInfo(
                    input, 
                    input.GetMethods().First(x => x.Name == "TooManyComplexTypes"));
            });

        }

        [Fact]
        public void ManyComplexTypes()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "ManyComplexTypes"));

            Assert.Equal(1, fixture.QueryParameterMap.Count);
            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);
        }

        [Fact]
        public void DefaultBodyParameterDetectedForPost()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PostWithBodyDetected"));

            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.NotNull(fixture.BodyParameterInfo);
        }

        [Fact]
        public void DefaultBodyParameterDetectedForPut()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PutWithBodyDetected"));

            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.NotNull(fixture.BodyParameterInfo);
        }

        [Fact]
        public void DefaultBodyParameterDetectedForPatch()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PatchWithBodyDetected"));

            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.NotNull(fixture.BodyParameterInfo);
        }

        [Fact]
        public void DefaultBodyParameterNotDetectedForGet()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "GetWithBodyDetected"));

            Assert.Equal(1, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void GarbagePathsShouldThrow()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "GarbagePath"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.False(shouldDie);
        }

        [Fact]
        public void MissingParametersShouldBlowUp()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffMissingParameters"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.False(shouldDie);
        }

        [Fact]
        public void ParameterMappingSmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuff"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void ParameterMappingWithQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithQueryParam"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal("search", fixture.QueryParameterMap[1]);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void ParameterMappingWithHardcodedQuerySmokeTest()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedQueryParam"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void AliasMappingShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithAlias"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void MultipleParametersPerSegmentShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchAnImage"));
            Assert.Equal("width", fixture.ParameterMap[0]);
            Assert.Equal("height", fixture.ParameterMap[1]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);
        }

        [Fact]
        public void FindTheBodyParameter()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithBody"));
            Assert.Equal("id", fixture.ParameterMap[0]);

            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);
        }

        [Fact]
        public void AllowUrlEncodedContent()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PostSomeUrlEncodedStuff"));
            Assert.Equal("id", fixture.ParameterMap[0]);

            Assert.NotNull(fixture.BodyParameterInfo);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Equal(BodySerializationMethod.UrlEncoded, fixture.BodyParameterInfo.Item1);
        }

        [Fact]
        public void HardcodedHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithHardcodedHeaders"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.True(fixture.Headers.ContainsKey("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("2", fixture.Headers["Api-Version"]);
            Assert.True(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.Equal(2, fixture.Headers.Count);
        }

        [Fact]
        public void DynamicHeadersShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "FetchSomeStuffWithDynamicHeader"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Null(fixture.BodyParameterInfo);

            Assert.Equal("Authorization", fixture.HeaderParameterMap[1]);
            Assert.True(fixture.Headers.ContainsKey("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", fixture.Headers["User-Agent"]);
            Assert.Equal(2, fixture.Headers.Count);
        }

        [Fact]
        public void ValueTypesDontBlowUp()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "OhYeahValueTypes"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Equal(0, fixture.QueryParameterMap.Count);
            Assert.Equal(BodySerializationMethod.Json, fixture.BodyParameterInfo.Item1);
            Assert.False(fixture.BodyParameterInfo.Item2);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);

            Assert.Equal(typeof(bool), fixture.SerializedReturnType);
        }

        [Fact]
        public void StreamMethodPullWorks()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PullStreamMethod"));
            Assert.Equal("id", fixture.ParameterMap[0]);
            Assert.Empty(fixture.QueryParameterMap);
            Assert.Equal(BodySerializationMethod.Json, fixture.BodyParameterInfo.Item1);
            Assert.True(fixture.BodyParameterInfo.Item2);
            Assert.Equal(1, fixture.BodyParameterInfo.Item3);

            Assert.Equal(typeof(bool), fixture.SerializedReturnType);
        }

        [Fact]
        public void ReturningTaskShouldWork()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "VoidPost"));
            Assert.Equal("id", fixture.ParameterMap[0]);

            Assert.Equal(typeof(Task), fixture.ReturnType);
            Assert.Equal(typeof(void), fixture.SerializedReturnType);
        }

        [Fact]
        public void SyncMethodsShouldThrow()
        {
            bool shouldDie = true;

            try {
                var input = typeof(IRestMethodInfoTests);
                var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "AsyncOnlyBuddy"));
            } catch (ArgumentException) {
                shouldDie = false;
            }

            Assert.False(shouldDie);
        }

        [Fact]
        public void UsingThePatchAttributeSetsTheCorrectMethod()
        {
            var input = typeof(IRestMethodInfoTests);
            var fixture = new RestMethodInfo(input, input.GetMethods().First(x => x.Name == "PatchSomething"));

            Assert.Equal("PATCH", fixture.HttpMethod.Method);
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

        [Post("/foo/bar/{id}")]
        [Headers("Content-Type: literally/anything")]
        Task<string> PostSomeStuffWithHardCodedContentTypeHeader(int id, [Body] string content);

        [Get("/foo/bar/{id}")]
        [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
        Task<string> FetchSomeStuffWithDynamicHeader(int id, [Header("Authorization")] string authorization);

        [Get("/foo/bar/{id}")]
        Task<string> FetchSomeStuffWithCustomHeader(int id, [Header("X-Emoji")] string custom);

        [Post("/foo/bar/{id}")]
        Task<string> PostSomeStuffWithCustomHeader(int id, [Body] object body, [Header("X-Emoji")] string emoji);

        [Get("/string")]
        Task<string> FetchSomeStuffWithoutFullPath();

        [Get("/void")]
        Task FetchSomeStuffWithVoid();

        [Get("/void/{id}/path")]
        Task FetchSomeStuffWithVoidAndQueryAlias(string id, [AliasAs("a")] string valueA, [AliasAs("b")] string valueB);

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

        [Patch("/foo/bar/{id}")]
        IObservable<string> PatchSomething(int id, [Body] string someAttribute);
    }

    interface ICancellableMethods
    {
        [Get("/foo")]
        Task GetWithCancellation(CancellationToken token = default (CancellationToken));
        [Get("/foo")]
        Task<string> GetWithCancellationAndReturn(CancellationToken token = default (CancellationToken));
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
        public HttpContent Content { get; set; }
        public Func<HttpContent> ContentFactory { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public string SendContent { get; set; }

        public TestHttpMessageHandler(string content = "test")
        {
            Content = new StringContent(content);
            ContentFactory = () => Content;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMessage = request;
            if (request.Content != null) {
                SendContent = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            CancellationToken = cancellationToken;
            MessagesSent++;

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = ContentFactory() };
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

    public class RequestBuilderTests
    {

        [Fact]
        public void MethodsShouldBeCancellableDefault()
        {
            var fixture = new RequestBuilderImplementation(typeof(ICancellableMethods));
            var factory = fixture.RunRequest("GetWithCancellation");
            var output = factory(new object[0]);

            var uri = new Uri(new Uri("http://api"), output.RequestMessage.RequestUri);
            Assert.Equal("/foo", uri.PathAndQuery);
            Assert.False(output.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void MethodsShouldBeCancellableWithToken()
        {
            var fixture = new RequestBuilderImplementation(typeof(ICancellableMethods));
            var factory = fixture.RunRequest("GetWithCancellation");

            var cts = new CancellationTokenSource();

            var output = factory(new object[]{cts.Token});

            var uri = new Uri(new Uri("http://api"), output.RequestMessage.RequestUri);
            Assert.Equal("/foo", uri.PathAndQuery);
            Assert.False(output.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void MethodsShouldBeCancellableWithTokenDoesCancel()
        {
            var fixture = new RequestBuilderImplementation(typeof(ICancellableMethods));
            var factory = fixture.RunRequest("GetWithCancellation");

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var output = factory(new object[] { cts.Token });
            Assert.True(output.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void HttpContentTest()
        {
            var fixture = new RequestBuilderImplementation(typeof(IHttpContentApi));
            var factory = fixture.BuildRestResultFuncForMethod("PostFileUpload");
            var testHttpMessageHandler = new TestHttpMessageHandler();
            var retContent = new StreamContent(new MemoryStream());
            testHttpMessageHandler.Content = retContent;

            var mpc = new MultipartContent("foosubtype");

            var task = (Task<HttpContent>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { mpc });
            task.Wait();

            Assert.Equal(testHttpMessageHandler.RequestMessage.Content, mpc);
            Assert.Equal(retContent, task.Result);
        }

        [Fact]
        public void StreamResponseTest()
        {
            var fixture = new RequestBuilderImplementation(typeof(IStreamApi));
            var factory = fixture.BuildRestResultFuncForMethod("GetRemoteFile");
            var testHttpMessageHandler = new TestHttpMessageHandler();
            var streamResponse = new MemoryStream();
            var reponseContent = "A remote file";
            testHttpMessageHandler.Content = new StreamContent(streamResponse);

            var writer = new StreamWriter(streamResponse);
            writer.Write(reponseContent);
            writer.Flush();
            streamResponse.Seek(0L, SeekOrigin.Begin);

            var task = (Task<Stream>)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { "test-file" });
            task.Wait();
            
            using (var reader = new StreamReader(task.Result))
                Assert.Equal(reponseContent, reader.ReadToEnd());
        }

        [Fact]
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
                Assert.False(shouldDie);
            }

            foreach (var v in successMethods) {
                bool shouldDie = false;

                try {
                    var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
                    fixture.BuildRequestFactoryForMethod(v);
                } catch (Exception) {
                    shouldDie = true;
                }

                Assert.False(shouldDie);
            }
        }

        [Fact]
        public void HardcodedQueryParamShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedQueryParameter");
            var output = factory(new object[] { 6 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/6?baz=bamf", uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrl()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedAndOtherQueryParameters");
            var output = factory(new object[] { 6, "foo" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/6?baz=bamf&search_for=foo", uri.PathAndQuery);
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrlAndValuesEncoded()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedAndOtherQueryParameters");
            var output = factory(new object[] { 6, "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
#if NETCOREAPP2_0
            Assert.Equal("/foo/bar/6?baz=bamf&search_for=push!%3Dpull", uri.PathAndQuery);
#else
            Assert.Equal("/foo/bar/6?baz=bamf&search_for=push!%3dpull", uri.PathAndQuery);
#endif
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQuery()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithVoidAndQueryAlias");
            var output = factory(new object[] { "6", "test@example.com", "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
#if NETCOREAPP2_0
            Assert.Equal("/void/6/path?a=test@example.com&b=push!%3Dpull", uri.PathAndQuery);
#else
            Assert.Equal("/void/6/path?a=test%40example.com&b=push!%3dpull", uri.PathAndQuery);
#endif
        }

        [Fact]
        public void QueryParamWithPathDelimiterShouldBeEncoded()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithVoidAndQueryAlias");
            var output = factory(new object[] { "6/6", "test@example.com", "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
#if NETCOREAPP2_0
            Assert.Equal("/void/6%2F6/path?a=test@example.com&b=push!%3Dpull", uri.PathAndQuery);
#else
            Assert.Equal("/void/6%2F6/path?a=test%40example.com&b=push!%3dpull", uri.PathAndQuery);
#endif
        }

        [Fact]
        public void ParameterizedQueryParamsShouldBeInUrlAndValuesEncodedWhenMixedReplacementAndQueryBadId()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithVoidAndQueryAlias");
            var output = factory(new object[] { "6", "test@example.com", "push!=pull" });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
#if NETCOREAPP2_0
            Assert.Equal("/void/6/path?a=test@example.com&b=push!%3Dpull", uri.PathAndQuery);
#else
            Assert.Equal("/void/6/path?a=test%40example.com&b=push!%3dpull", uri.PathAndQuery);
#endif
        }

        [Fact]
        public void MultipleParametersInTheSameSegmentAreGeneratedProperly()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomethingWithMultipleParametersPerSegment");
            var output = factory(new object[] { 6, 1024, 768 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/6/1024x768/foo", uri.PathAndQuery);
        }

        [Fact]
        public void HardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.True(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.True(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("2", output.Headers.GetValues("Api-Version").Single());
        }

        [Fact]
        public void EmptyHardcodedHeadersShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithEmptyHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.True(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.True(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.Equal("", output.Headers.GetValues("Api-Version").Single());
        }
        [Fact]
        public void NullHardcodedHeadersShouldNotBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithNullHardcodedHeader");
            var output = factory(new object[] { 6 });

            Assert.True(output.Headers.Contains("User-Agent"), "Headers include User-Agent header");
            Assert.Equal("RefitTestClient", output.Headers.UserAgent.ToString());
            Assert.False(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
        }

        [Fact]
        public void ContentHeadersCanBeHardcoded()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PostSomeStuffWithHardCodedContentTypeHeader");
            var output = factory(new object[] { 6, "stuff" });

            Assert.True(output.Content.Headers.Contains("Content-Type"), "Content headers include Content-Type header");
            Assert.Equal("literally/anything", output.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public void DynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.NotNull(output.Headers.Authorization);//, "Headers include Authorization header");
            Assert.Equal("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }

        [Fact]
        public void CustomDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, ":joy_cat:" });

            Assert.True(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.Equal(":joy_cat:", output.Headers.GetValues("X-Emoji").First());
        }

        [Fact]
        public void EmptyDynamicHeaderShouldBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, "" });

            Assert.True(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.Equal("", output.Headers.GetValues("X-Emoji").First());
        }

        [Fact]
        public void NullDynamicHeaderShouldNotBeInHeaders()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuffWithDynamicHeader");
            var output = factory(new object[] { 6, null });

            Assert.Null(output.Headers.Authorization);//, "Headers include Authorization header");
        }

        [Fact]
        public void AddCustomHeadersToRequestHeadersOnly()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PostSomeStuffWithCustomHeader");
            var output = factory(new object[] { 6, new { Foo = "bar" }, ":smile_cat:" });

            Assert.True(output.Headers.Contains("Api-Version"), "Headers include Api-Version header");
            Assert.True(output.Headers.Contains("X-Emoji"), "Headers include X-Emoji header");
            Assert.False(output.Content.Headers.Contains("Api-Version"), "Content headers include Api-Version header");
            Assert.False(output.Content.Headers.Contains("X-Emoji"), "Content headers include X-Emoji header");
        }

        [Fact]
        public void HttpClientShouldPrefixedAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithoutFullPath");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/foo/bar") }, new object[0]);
            task.Wait();

            Assert.Equal("http://api/foo/bar/string", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Fact]
        public void HttpClientForVoidMethodShouldPrefixedAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuffWithVoid");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/foo/bar") }, new object[0]);
            task.Wait();

            Assert.Equal("http://api/foo/bar/void", testHttpMessageHandler.RequestMessage.RequestUri.ToString());
        }

        [Fact]
        public void HttpClientShouldNotPrefixEmptyAbsolutePathToTheRequestUri()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRestResultFuncForMethod("FetchSomeStuff");
            var testHttpMessageHandler = new TestHttpMessageHandler();

            var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, new object[] { 42 });
            task.Wait();

            Assert.Equal("http://api/foo/bar/42", testHttpMessageHandler.RequestMessage.RequestUri.ToString());            
        }

        [Fact]
        public void DontBlowUpWithDynamicAuthorizationHeaderAndContent() 
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PutSomeContentWithAuthorization");
            var output = factory(new object[] { 7, new { Octocat = "Dunetocat" }, "Basic RnVjayB5ZWFoOmhlYWRlcnMh" });

            Assert.NotNull(output.Headers.Authorization);//, "Headers include Authorization header");
            Assert.Equal("RnVjayB5ZWFoOmhlYWRlcnMh", output.Headers.Authorization.Parameter);
        }

        [Fact]
        public void SuchFlexibleContentTypeWow()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.BuildRequestFactoryForMethod("PutSomeStuffWithDynamicContentType");
            var output = factory(new object[] { 7, "such \"refit\" is \"amaze\" wow", "text/dson" });

            Assert.NotNull(output.Content);//, "Request has content");
            Assert.NotNull(output.Content.Headers.ContentType);//, "Headers include Content-Type header");
            Assert.Equal("text/dson", output.Content.Headers.ContentType.MediaType);//, "Content-Type header has the expected value");
        }

        [Fact]
        public void BodyContentGetsUrlEncoded() 
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.RunRequest("PostSomeUrlEncodedStuff");
            var output = factory(
                new object[] {
                    6, 
                    new {
                        Foo = "Something", 
                        Bar = 100, 
                        Baz = default(string)
                    }
                });

            Assert.Equal("Foo=Something&Bar=100&Baz=", output.SendContent);
        }

        [Fact]
        public void FormFieldGetsAliased()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.RunRequest("PostSomeAliasedUrlEncodedStuff");
            var output = factory(
                new object[] {
                    6, 
                    new SomeRequestData {
                        ReadablePropertyName = 99
                    }
                });



            Assert.Equal("rpn=99", output.SendContent);
        }

        [Fact]
        public void CustomParmeterFormatter()
        {
            var settings = new RefitSettings { UrlParameterFormatter = new TestUrlParameterFormatter("custom-parameter") };
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi), settings);

            var factory = fixture.BuildRequestFactoryForMethod("FetchSomeStuff");
            var output = factory(new object[] { 5 });

            var uri = new Uri(new Uri("http://api"), output.RequestUri);
            Assert.Equal("/foo/bar/custom-parameter", uri.PathAndQuery);
        }

        [Fact]
        public void ICanPostAValueTypeIfIWantYoureNotTheBossOfMe()
        {
            var fixture = new RequestBuilderImplementation(typeof(IDummyHttpApi));
            var factory = fixture.RunRequest("PostAValueType", "true");
            var guid = Guid.NewGuid();
            var expected = string.Format("\"{0}\"", guid);
            var output = factory(new object[] { 7, guid });


            Assert.Equal(expected, output.SendContent);
        }
    }

    static class RequestBuilderTestExtensions
    {
        public static Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(this IRequestBuilder builder, string methodName)
        {
            var factory = builder.BuildRestResultFuncForMethod(methodName);
            var testHttpMessageHandler = new TestHttpMessageHandler();


            return paramList => {
               var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/")}, paramList);
               task.Wait();
               return testHttpMessageHandler.RequestMessage;
           };
        }

       
        public static Func<object[], TestHttpMessageHandler> RunRequest(this IRequestBuilder builder, string methodName, string returnContent = null)
        {
            var factory = builder.BuildRestResultFuncForMethod(methodName);
            var testHttpMessageHandler = new TestHttpMessageHandler();
            if (returnContent != null) {
                testHttpMessageHandler.Content = new StringContent(returnContent);
            }

            return paramList => {
                var task = (Task)factory(new HttpClient(testHttpMessageHandler) { BaseAddress = new Uri("http://api/") }, paramList);
                task.Wait();
                return testHttpMessageHandler;
            };
        }
    }
}
