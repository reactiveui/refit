using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Refit; // InterfaceStubGenerator looks for this

using RichardSzalay.MockHttp;

using Xunit;

namespace Refit.Tests
{
#pragma warning disable IDE1006 // Naming Styles
    public class RootObject
    {
        public string _id { get; set; }
        public string _rev { get; set; }
        public string name { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles

    public class BigObject
    {
        public byte[] BigData { get; set; }
    }

    [Headers("User-Agent: Refit Integration Tests")]
    public interface INpmJs
    {
        [Get("/congruence")]
        Task<RootObject> GetCongruence();
    }

    public interface IRequestBin
    {
        [Post("/1h3a5jm1")]
        Task Post();

        [Post("/foo")]
        Task PostRawStringDefault([Body] string str);

        [Post("/foo")]
        Task PostRawStringJson([Body(BodySerializationMethod.Serialized)] string str);

        [Post("/foo")]
        Task PostRawStringUrlEncoded([Body(BodySerializationMethod.UrlEncoded)] string str);

        [Post("/1h3a5jm1")]
        Task PostGeneric<T>(T param);

        [Post("/foo")]
        Task PostVoidReturnBodyBuffered<T>([Body(buffered: true)] T param);

        [Post("/foo")]
        Task<string> PostNonVoidReturnBodyBuffered<T>([Body(buffered: true)] T param);

        [Post("/big")]
        Task PostBig(BigObject big);

        [Get("/foo/{arguments}")]
        Task SomeApiThatUsesVariableNameFromCodeGen(string arguments);
    }

    public interface IApiBindPathToObject
    {
        [Get("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task GetFooBars(PathBoundObject request);

        [Get("/foos/{Requestparams.SomeProperty}/bar/{requestParams.SoMeProPerty2}")]
        Task GetFooBarsWithDifferentCasing(PathBoundObject requestParams);

        [Get("/foos/{id}/{request.someProperty}/bar/{request.someProperty2}")]
        Task GetBarsByFoo(string id, PathBoundObject request);

        [Get("/foos/{someProperty}/bar/{request.someProperty2}")]
        Task GetFooBars(PathBoundObject request, string someProperty);

        [Get("/foos/{request.someProperty}/bar")]
        Task GetBarsByFoo(PathBoundObject request);

        [Get("/foo")]
        Task GetBarsWithCustomQueryFormat(PathBoundObjectWithQueryFormat request);

        [Get("/foos/{request.someProperty}/bar/{request.someProperty3}")]
        Task GetFooBarsDerived(PathBoundDerivedObject request);

        [Get("/foos/{request.values}")]
        Task GetFoos(PathBoundList request);

        [Get("/foos2/{values}")]
        Task GetFoos2(List<int> values);

        [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task PostFooBar(PathBoundObject request, [Body]object someObject);

        [Get("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task GetFooBars(PathBoundObjectWithQuery request);

        [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task<HttpResponseMessage> PostFooBar(PathBoundObject request, [Query] ModelObject someQueryParams);

        [Multipart]
        [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task<HttpResponseMessage> PostFooBarStreamPart(PathBoundObject request, [Query] ModelObject someQueryParams, StreamPart stream);

        [Multipart]
        [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task<HttpResponseMessage> PostFooBarStreamPart(PathBoundObject request, StreamPart stream);

        [Multipart]
        [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
        Task<HttpResponseMessage> PostFooBarStreamPart(PathBoundObjectWithQuery request, StreamPart stream);
    }

    public class PathBoundList
    {
        public List<int> Values { get; set; }
    }

    public class PathBoundDerivedObject : PathBoundObject
    {
        public string SomeProperty3 { get; set; }

    }

    public class PathBoundObject
    {
        public int SomeProperty { get; set; }

        public string SomeProperty2 { get; set; }

    }

    public class PathBoundObjectWithQuery
    {
        public int SomeProperty { get; set; }

        public string SomeProperty2 { get; set; }

        [Query]
        public string SomeQuery { get; set; }

    }

    public class PathBoundObjectWithQueryFormat
    {

        [Query(Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'")]
        public DateTime SomeQueryWithFormat { get; set; }
    }

    public interface INoRefitHereBuddy
    {
        Task Post();
    }

    public interface IAmHalfRefit
    {
        [Post("/anything")]
        Task Post();

        Task Get();
    }

    public interface IRefitInterfaceWithStaticMethod
    {
        [Get("")]
        Task Get();

#if NETCOREAPP3_1_OR_GREATER
        public static IRefitInterfaceWithStaticMethod Create()
        {
            // This is a C# 8 factory method

            return RestService.For<IRefitInterfaceWithStaticMethod>("http://foo/");
        }
#endif
    }

    public class ErrorResponse
    {
        public string[] Errors { get; set; }
    }

    public interface IHttpBinApi<TResponse, in TParam, in THeader>
        where TResponse : class
        where THeader : struct
    {
        [Get("")]
        Task<TResponse> Get(TParam param, [Header("X-Refit")] THeader header);

        [Get("/get?hardcoded=true")]
        Task<TResponse> GetQuery([Query("_")]TParam param);

        [Post("/post?hardcoded=true")]
        Task<TResponse> PostQuery([Query("_")]TParam param);

        [Get("")]
        Task<TResponse> GetQueryWithIncludeParameterName([Query(".", "search")]TParam param);

        [Get("/get?hardcoded=true")]
        Task<TValue> GetQuery1<TValue>([Query("_")]TParam param);


    }

    public interface IBrokenWebApi
    {
        [Post("/what-spec")]
        Task<bool> PostAValue([Body] string derp);
    }

    public interface IHttpContentApi
    {
        [Post("/blah")]
        Task<HttpContent> PostFileUpload([Body] HttpContent content);

        [Post("/blah")]
        Task<ApiResponse<HttpContent>> PostFileUploadWithMetadata([Body] HttpContent content);
    }

    public interface IStreamApi
    {
        [Post("/{filename}")]
        Task<Stream> GetRemoteFile(string filename);

        [Post("/{filename}")]
        Task<ApiResponse<Stream>> GetRemoteFileWithMetadata(string filename);
    }

    public interface IApiWithDecimal
    {
        [Get("/withDecimal")]
        Task<string> GetWithDecimal(decimal value);
    }

    public interface IBodylessApi
    {
        [Post("/nobody")]
        [Headers("Content-Type: application/x-www-form-urlencoded; charset=UTF-8")]
        Task Post();

        [Get("/nobody")]
        [Headers("Content-Type: application/x-www-form-urlencoded; charset=UTF-8")]
        Task Get();

        [Head("/nobody")]
        [Headers("Content-Type: application/x-www-form-urlencoded; charset=UTF-8")]
        Task Head();
    }

    public interface ITrimTrailingForwardSlashApi
    {
        HttpClient Client { get; }

        [Get("/someendpoint")]
        Task Get();
    }

    public interface IValidApi
    {
        [Get("/someendpoint")]
        Task Get();
    }

    public class HttpBinGet
    {
        public Dictionary<string, object> Args { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Origin { get; set; }
        public string Url { get; set; }
    }

    public class RestServiceIntegrationTests
    {
#if NETCOREAPP3_1_OR_GREATER
        [Fact]
        public void CanCreateInstanceUsingStaticMethod()
        {
            var instance = IRefitInterfaceWithStaticMethod.Create();


            Assert.NotNull(instance);
        }
#endif


        [Fact]
        public async Task CanAddContentHeadersToPostWithoutBody()
        {
            var mockHttp = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://foo/nobody")
                // The content length header is set automatically by the HttpContent instance,
                // so checking the header as a string doesn't work
                .With(r => r.Content?.Headers.ContentLength == 0)
                // But we added content type ourselves, so this should work
                .WithHeaders("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")
                .WithContent("")
                .Respond("application/json", "Ok");

            var fixture = RestService.For<IBodylessApi>("http://foo", settings);

            await fixture.Post();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithNoParametersTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/someendpoint")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<ITrimTrailingForwardSlashApi>("http://foo", settings);

            await fixture.Get();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task BaseAddressFromHttpClientMatchesTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/someendpoint")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");


            var client = new HttpClient(mockHttp)
            {
                BaseAddress = new Uri("http://foo")
            };

            var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

            await fixture.Get();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task BaseAddressWithTrailingSlashFromHttpClientMatchesTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/someendpoint")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");


            var client = new HttpClient(mockHttp)
            {
                BaseAddress = new Uri("http://foo/")
            };

            var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

            await fixture.Get();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task BaseAddressWithTrailingSlashCalledBeforeFromHttpClientMatchesTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/someendpoint")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");


            var client = new HttpClient(mockHttp)
            {
                BaseAddress = new Uri("http://foo/")
            };

            await client.GetAsync("/firstRequest"); ;

            var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

            await fixture.Get();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithNoParametersTestTrailingSlashInBase()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/someendpoint")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<ITrimTrailingForwardSlashApi>("http://foo/", settings);

            await fixture.Get();
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObject()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/1/bar/barNone")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetFooBars(new PathBoundObject()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectDifferentCasing()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/1/bar/barNone")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetFooBarsWithDifferentCasing(new PathBoundObject()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectAndParameter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/myId/22/bar/bart")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetBarsByFoo("myId", new PathBoundObject()
            {
                SomeProperty = 22,
                SomeProperty2 = "bart"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectAndParameterParameterPrecedence()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/chooseMe/bar/barNone")
                    .WithExactQueryString(new[] { new KeyValuePair<string, string>("SomeProperty", "1") })
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetFooBars(new PathBoundObject()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone"
            }, "chooseMe");
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundDerivedObject()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/1/bar/test")
                    .WithExactQueryString(new[] { new KeyValuePair<string, string>("SomeProperty2", "barNone") })
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetFooBarsDerived(new PathBoundDerivedObject()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone",
                SomeProperty3 = "test"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectAndQueryParameter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/22/bar")
                    .WithExactQueryString(new[] { new KeyValuePair<string, string>("SomeProperty2", "bart") })
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetBarsByFoo(new PathBoundObject()
            {
                SomeProperty = 22,
                SomeProperty2 = "bart"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostFooBarPathBoundObject()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "http://foo/foos/22/bar/bart")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.PostFooBar(new PathBoundObject()
            {
                SomeProperty = 22,
                SomeProperty2 = "bart"
            }, new { });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PathBoundObjectsRespectFormatter()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/22%2C23")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                UrlParameterFormatter = new TestEnumerableUrlParameterFormatter()
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetFoos(new PathBoundList()
            {
                Values = new List<int>() { 22, 23 }
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectAndQuery()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foos/1/bar/barNone")
                    .WithExactQueryString("SomeQuery=test")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetFooBars(new PathBoundObjectWithQuery()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone",
                SomeQuery = "test"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectAndQueryWithFormat()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Get, "http://foo/foo")
                    .WithExactQueryString("SomeQueryWithFormat=2020-03-05T13:55:00Z")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.GetBarsWithCustomQueryFormat(new PathBoundObjectWithQueryFormat
            {
                SomeQueryWithFormat = new DateTime(2020, 03, 05, 13, 55, 00)
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithPathBoundObjectAndQueryObject()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "http://foo/foos/1/bar/barNone")
                    .WithExactQueryString("Property1=test&Property2=test2")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            await fixture.PostFooBar(new PathBoundObject()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone"
            }, new ModelObject()
            {
                Property1 = "test",
                Property2 = "test2"
            });
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostFooBarPathMultipart()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "http://foo/foos/22/bar/bar")
                    .WithExactQueryString("")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            await fixture.PostFooBarStreamPart(new PathBoundObject()
            {
                SomeProperty = 22,
                SomeProperty2 = "bar"
            }, new StreamPart(stream, "Test.pdf", "application/pdf"));
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostFooBarPathQueryMultipart()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "http://foo/foos/22/bar/bar")
                    .WithExactQueryString("SomeQuery=test")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            await fixture.PostFooBarStreamPart(new PathBoundObjectWithQuery()
            {
                SomeProperty = 22,
                SomeProperty2 = "bar",
                SomeQuery = "test"
            }, new StreamPart(stream, "Test.pdf", "application/pdf"));
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostFooBarPathQueryObjectMultipart()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, "http://foo/foos/22/bar/bar")
                    .WithExactQueryString("Property1=test&Property2=test2")
                    .Respond("application/json", "Ok");

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };
            var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

            using var stream = GetTestFileStream("Test Files/Test.pdf");
            await fixture.PostFooBarStreamPart(new PathBoundObject
            {
                SomeProperty = 22,
                SomeProperty2 = "bar"
            }, new ModelObject()
            {
                Property1 = "test",
                Property2 = "test2"
            }, new StreamPart(stream, "Test.pdf", "application/pdf"));
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task DoesntAddAutoAddContentToGetRequest()
        {
            var mockHttp = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "http://foo/nobody")
                // We can't add HttpContent to a GET request,
                // because HttpClient doesn't allow it and it will
                // blow up at runtime
                .With(r => r.Content == null)
                .Respond("application/json", "Ok");

            var fixture = RestService.For<IBodylessApi>("http://foo", settings);

            await fixture.Get();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task DoesntAddAutoAddContentToHeadRequest()
        {
            var mockHttp = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Head, "http://foo/nobody")
                // We can't add HttpContent to a HEAD request,
                // because HttpClient doesn't allow it and it will
                // blow up at runtime
                .With(r => r.Content == null)
                .Respond("application/json", "Ok");

            var fixture = RestService.For<IBodylessApi>("http://foo", settings);

            await fixture.Head();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task GetWithDecimal()
        {
            var mockHttp = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "http://foo/withDecimal")
                    .WithExactQueryString(new[] { new KeyValuePair<string, string>("value", "3.456") })
                    .Respond("application/json", "Ok");

            var fixture = RestService.For<IApiWithDecimal>("http://foo", settings);

            const decimal val = 3.456M;


            var result = await fixture.GetWithDecimal(val);

            mockHttp.VerifyNoOutstandingExpectation();
        }


        [Fact]
        public async Task HitTheGitHubUserApiAsApiResponse()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            var responseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ 'login':'octocat', 'avatar_url':'http://foo/bar' }", System.Text.Encoding.UTF8, "application/json"),
            };
            responseMessage.Headers.Add("Cookie", "Value");

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat").Respond(req => responseMessage);

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetUserWithMetadata("octocat");

            Assert.True(result.Headers.Any());
            Assert.True(result.IsSuccessStatusCode);
            Assert.NotNull(result.ReasonPhrase);
            Assert.NotNull(result.RequestMessage);
            Assert.False(result.StatusCode == default);
            Assert.NotNull(result.Version);
            Assert.Equal("octocat", result.Content.Login);
            Assert.False(string.IsNullOrEmpty(result.Content.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheNonExistentApiAsApiResponse()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
                    .Respond(HttpStatusCode.NotFound);

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            using var result = await fixture.NothingToSeeHereWithMetadata();
            Assert.False(result.IsSuccessStatusCode);
            Assert.NotNull(result.ReasonPhrase);
            Assert.NotNull(result.RequestMessage);
            Assert.True(result.StatusCode == HttpStatusCode.NotFound);
            Assert.NotNull(result.Version);
            Assert.Null(result.Content);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheNonExistentApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
                    .Respond(HttpStatusCode.NotFound);

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            try
            {
                var result = await fixture.NothingToSeeHere();
            }
            catch (Exception ex)
            {
                Assert.IsType<ApiException>(ex);
            }

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservableApiResponse()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            var responseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ 'login':'octocat', 'avatar_url':'http://foo/bar' }", System.Text.Encoding.UTF8, "application/json"),
            };
            responseMessage.Headers.Add("Cookie", "Value");

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat").Respond(req => responseMessage);

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetUserObservableWithMetadata("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            Assert.True(result.Headers.Any());
            Assert.True(result.IsSuccessStatusCode);
            Assert.NotNull(result.ReasonPhrase);
            Assert.NotNull(result.RequestMessage);
            Assert.False(result.StatusCode == default);
            Assert.NotNull(result.Version);
            Assert.Equal("octocat", result.Content.Login);
            Assert.False(string.IsNullOrEmpty(result.Content.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                    .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetUser("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(string.IsNullOrEmpty(result.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitWithCamelCaseParameter()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                   .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetUserCamelCase("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(string.IsNullOrEmpty(result.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubOrgMembersApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
                  .Respond("application/json", "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetOrgMembers("github");

            Assert.True(result.Count > 0);
            Assert.Contains(result, member => member.Type == "User");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubOrgMembersApiInParallel()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
                  .Respond("application/json", "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");
            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
                  .Respond("application/json", "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var task1 = fixture.GetOrgMembers("github");
            var task2 = fixture.GetOrgMembers("github");

            await Task.WhenAll(task1, task2);

            Assert.True(task1.Result.Count > 0);
            Assert.Contains(task1.Result, member => member.Type == "User");

            Assert.True(task2.Result.Count > 0);
            Assert.Contains(task2.Result, member => member.Type == "User");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task RequestCanceledBeforeResponseRead()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            var cts = new CancellationTokenSource();

            mockHttp.When(HttpMethod.Get, "https://api.github.com/orgs/github/members")
                    .Respond(req =>
                    {
                        // Cancel the request
                        cts.Cancel();

                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]", Encoding.UTF8, "application/json")
                        };
                    });


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);



            await Assert.ThrowsAsync<TaskCanceledException>(async () => await fixture.GetOrgMembers("github", cts.Token));
        }


        [Fact]
        public async Task HitTheGitHubUserSearchApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/search/users")
                    .WithQueryString("q", "tom repos:>42 followers:>1000")
                    .Respond("application/json", "{ 'total_count': 1, 'items': [{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]}");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

            Assert.True(result.TotalCount > 0);
            Assert.Contains(result.Items, member => member.Type == "User");
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservable()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                    .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);


            var result = await fixture.GetUserObservable("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            Assert.Equal("octocat", result.Login);
            Assert.False(string.IsNullOrEmpty(result.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservableAndSubscribeAfterTheFact()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.When(HttpMethod.Get, "https://api.github.com/users/octocat")
                    .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var obs = fixture.GetUserObservable("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            // NB: We're gonna await twice, so that the 2nd await is definitely
            // after the result has completed.
            await obs;
            var result2 = await obs;
            Assert.Equal("octocat", result2.Login);
            Assert.False(string.IsNullOrEmpty(result2.AvatarUrl));
        }

        [Fact]
        public async Task TwoSubscriptionsResultInTwoRequests()
        {
            var input = new TestHttpMessageHandler
            {

                // we need to use a factory here to ensure each request gets its own httpcontent instance
                ContentFactory = () => new StringContent("test")
            };

            var client = new HttpClient(input) { BaseAddress = new Uri("http://foo") };
            var fixture = RestService.For<IGitHubApi>(client);

            Assert.Equal(0, input.MessagesSent);

            var obs = fixture.GetIndexObservable()
                .Timeout(TimeSpan.FromSeconds(10));

            var result1 = await obs;
            Assert.Equal(1, input.MessagesSent);

            var result2 = await obs;
            Assert.Equal(2, input.MessagesSent);

            // NB: TestHttpMessageHandler returns what we tell it to ('test' by default)
            Assert.Contains("test", result1);
            Assert.Contains("test", result2);
        }

        [Fact]
        public async Task ShouldRetHttpResponseMessage()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.When(HttpMethod.Get, "https://api.github.com/")
                    .Respond(HttpStatusCode.OK);


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
            var result = await fixture.GetIndex();

            Assert.NotNull(result);
            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact]
        public async Task ShouldRetHttpResponseMessageWithNestedInterface()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.When(HttpMethod.Get, "https://api.github.com/")
                    .Respond(HttpStatusCode.OK);


            var fixture = RestService.For<TestNested.INestedGitHubApi>("https://api.github.com", settings);
            var result = await fixture.GetIndex();

            Assert.NotNull(result);
            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact]
        public async Task HitTheNpmJs()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://registry.npmjs.org/congruence")
                    .Respond("application/json", "{ \"_id\":\"congruence\", \"_rev\":\"rev\" , \"name\":\"name\"}");



            var fixture = RestService.For<INpmJs>("https://registry.npmjs.org", settings);
            var result = await fixture.GetCongruence();

            Assert.Equal("congruence", result._id);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostToRequestBin()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1")
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);


            await fixture.Post();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostStringDefaultToRequestBin()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/foo")
                    .WithContent("raw string")
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);


            await fixture.PostRawStringDefault("raw string");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostStringJsonToRequestBin()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/foo")
                    .WithContent("\"json string\"")
                    .WithHeaders("Content-Type", "application/json; charset=utf-8")
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);


            await fixture.PostRawStringJson("json string");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostStringUrlToRequestBin()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/foo")
                    .WithContent("url%26string")
                    .WithHeaders("Content-Type", "application/x-www-form-urlencoded; charset=utf-8")
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);


            await fixture.PostRawStringUrlEncoded("url&string");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostToRequestBinWithGenerics()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1")
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);


            await fixture.PostGeneric(5);

            mockHttp.VerifyNoOutstandingExpectation();

            mockHttp.ResetExpectations();

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1")
                    .Respond(HttpStatusCode.OK);

            await fixture.PostGeneric("4");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostWithVoidReturnBufferedBodyExpectContentLengthHeader()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var postBody = new Dictionary<string, string>
            {
                { "some", "body" },
                { "once", "told me" }
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/foo")
                .With(request => request.Content?.Headers.ContentLength > 0)
                .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

            await fixture.PostVoidReturnBodyBuffered(postBody);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostWithNonVoidReturnBufferedBodyExpectContentLengthHeader()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var postBody = new Dictionary<string, string>
            {
                { "some", "body" },
                { "once", "told me" }
            };
            const string expectedResponse = "some response";

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/foo")
                .With(request => request.Content?.Headers.ContentLength > 0)
                .Respond("text/plain", expectedResponse);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

            var result = await fixture.PostNonVoidReturnBodyBuffered(postBody);

            mockHttp.VerifyNoOutstandingExpectation();

            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task UseMethodWithArgumentsParameter()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

            mockHttp.Expect(HttpMethod.Get, "http://httpbin.org/foo/something")
                    .Respond(HttpStatusCode.OK);

            await fixture.SomeApiThatUsesVariableNameFromCodeGen("something");

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CanGetDataOutOfErrorResponses()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.When(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
                    .Respond(HttpStatusCode.NotFound, "application/json", "{'message': 'Not Found', 'documentation_url': 'http://foo/bar'}");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
            try
            {
                await fixture.NothingToSeeHere();
                Assert.True(false);
            }
            catch (ApiException exception)
            {
                Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
                var content = await exception.GetContentAsAsync<Dictionary<string, string>>();

                Assert.Equal("Not Found", content["message"]);
                Assert.NotNull(content["documentation_url"]);
            }
        }

        [Fact]
        public async Task CanSerializeBigData()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new SystemTextJsonContentSerializer()
            };

            var bigObject = new BigObject
            {
                BigData = Enumerable.Range(0, 800000).Select(x => (byte)(x % 256)).ToArray()
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/big")
                    .With(m =>
                    {
                        async Task<bool> T()
                        {
                            using var s = await m.Content.ReadAsStreamAsync();
                            var it = await System.Text.Json.JsonSerializer.DeserializeAsync<BigObject>(s, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                            return it.BigData.SequenceEqual(bigObject.BigData);
                        }

                        return T().Result;
                    })
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

            await fixture.PostBig(bigObject);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ErrorsFromApiReturnErrorContent()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Post, "https://api.github.com/users")
                    .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);


            var result = await Assert.ThrowsAsync<ApiException>(async () => await fixture.CreateUser(new User { Name = "foo" }));


            var errors = await result.GetContentAsAsync<ErrorResponse>();

            Assert.Contains("error1", errors.Errors);
            Assert.Contains("message", errors.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }


        [Fact]
        public async Task ErrorsFromApiReturnErrorContentWhenApiResponse()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() })
            };

            mockHttp.Expect(HttpMethod.Post, "https://api.github.com/users")
                    .Respond(HttpStatusCode.BadRequest, "application/json", "{ 'errors': [ 'error1', 'message' ]}");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);


            using var response = await fixture.CreateUserWithMetadata(new User { Name = "foo" });
            Assert.False(response.IsSuccessStatusCode);
            Assert.NotNull(response.Error);

            var errors = await response.Error.GetContentAsAsync<ErrorResponse>();

            Assert.Contains("error1", errors.Errors);
            Assert.Contains("message", errors.Errors);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public void NonRefitInterfacesThrowMeaningfulExceptions()
        {
            try
            {
                RestService.For<INoRefitHereBuddy>("http://example.com");
            }
            catch (InvalidOperationException exception)
            {
                Assert.StartsWith("INoRefitHereBuddy", exception.Message);
            }
        }

        [Fact]
        public async Task NonRefitMethodsThrowMeaningfulExceptions()
        {
            try
            {
                var fixture = RestService.For<IAmHalfRefit>("http://example.com");
                await fixture.Get();
            }
            catch (NotImplementedException exception)
            {
                Assert.Contains("no Refit HTTP method attribute", exception.Message);
            }
        }

        [Fact]
        public async Task GenericsWork()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "http://httpbin.org/get")
                    .WithHeaders("X-Refit", "99")
                    .WithQueryString("param", "foo")
                    .Respond("application/json", "{\"url\": \"http://httpbin.org/get?param=foo\", \"args\": {\"param\": \"foo\"}, \"headers\":{\"X-Refit\":\"99\"}}");



            var fixture = RestService.For<IHttpBinApi<HttpBinGet, string, int>>("http://httpbin.org/get", settings);

            var result = await fixture.Get("foo", 99);

            Assert.Equal("http://httpbin.org/get?param=foo", result.Url);
            Assert.Equal("foo", result.Args["param"]);
            Assert.Equal("99", result.Headers["X-Refit"]);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ValueTypesArentValidButTheyWorkAnyway()
        {
            var handler = new TestHttpMessageHandler("true");

            var fixture = RestService.For<IBrokenWebApi>(new HttpClient(handler) { BaseAddress = new Uri("http://nowhere.com") });

            var result = await fixture.PostAValue("Does this work?");

            Assert.True(result);
        }

        [Fact]
        public async void MissingBaseUrlThrowsArgumentException()
        {
            var client = new HttpClient(); // No BaseUrl specified

            var fixture = RestService.For<IGitHubApi>(client);

            // We should get an InvalidOperationException if we call a method without a base address set
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await fixture.GetUser(null));
        }

        [Fact]
        public async Task SimpleDynamicQueryparametersTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .WithHeaders("X-Refit", "99")
                .Respond("application/json", "{\"url\": \"https://httpbin.org/get?FirstName=John&LastName=Rambo\", \"args\": {\"FirstName\": \"John\", \"lName\": \"Rambo\"}}");

            var myParams = new MySimpleQueryParams
            {
                FirstName = "John",
                LastName = "Rambo"
            };

            var fixture = RestService.For<IHttpBinApi<HttpBinGet, MySimpleQueryParams, int>>("https://httpbin.org/get", settings);

            var resp = await fixture.Get(myParams, 99);

            Assert.Equal("John", resp.Args["FirstName"]);
            Assert.Equal("Rambo", resp.Args["lName"]);
        }

        [Fact]
        public async Task ComplexDynamicQueryparametersTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .Respond("application/json", "{\"url\": \"https://httpbin.org/get?hardcoded=true&FirstName=John&LastName=Rambo&Addr_Zip=9999&Addr_Street=HomeStreet 99&MetaData_Age=99&MetaData_Initials=JR&MetaData_Birthday=10%2F31%2F1918 4%3A21%3A16 PM&Other=12345&Other=10%2F31%2F2017 4%3A21%3A17 PM&Other=696e8653-6671-4484-a65f-9485af95fd3a\", \"args\": { \"Addr_Street\": \"HomeStreet 99\", \"Addr_Zip\": \"9999\", \"FirstName\": \"John\", \"LastName\": \"Rambo\", \"MetaData_Age\": \"99\", \"MetaData_Birthday\": \"10/31/1981 4:32:59 PM\", \"MetaData_Initials\": \"JR\", \"Other\": [\"12345\",\"10/31/2017 4:32:59 PM\",\"60282dd2-f79a-4400-be01-bcb0e86e7bc6\"], \"hardcoded\": \"true\"}}");

            var myParams = new MyComplexQueryParams
            {
                FirstName = "John",
                LastName = "Rambo"
            };
            myParams.Address.Postcode = 9999;
            myParams.Address.Street = "HomeStreet 99";

            myParams.MetaData.Add("Age", 99);
            myParams.MetaData.Add("Initials", "JR");
            myParams.MetaData.Add("Birthday", new DateTime(1981, 10, 31, 16, 24, 59));

            myParams.Other.Add(12345);
            myParams.Other.Add(new DateTime(2017, 10, 31, 16, 24, 59));
            myParams.Other.Add(new Guid("60282dd2-f79a-4400-be01-bcb0e86e7bc6"));


            var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>("https://httpbin.org", settings);

            var resp = await fixture.GetQuery(myParams);

            Assert.Equal("John", resp.Args["FirstName"]);
            Assert.Equal("Rambo", resp.Args["LastName"]);
            Assert.Equal("9999", resp.Args["Addr_Zip"]);
        }

        [Fact]
        public async Task ComplexPostDynamicQueryparametersTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "https://httpbin.org/post")
                .Respond("application/json", "{\"url\": \"https://httpbin.org/post?hardcoded=true&FirstName=John&LastName=Rambo&Addr_Zip=9999&Addr_Street=HomeStreet 99&MetaData_Age=99&MetaData_Initials=JR&MetaData_Birthday=10%2F31%2F1918 4%3A21%3A16 PM&Other=12345&Other=10%2F31%2F2017 4%3A21%3A17 PM&Other=696e8653-6671-4484-a65f-9485af95fd3a\", \"args\": { \"Addr_Street\": \"HomeStreet 99\", \"Addr_Zip\": \"9999\", \"FirstName\": \"John\", \"LastName\": \"Rambo\", \"MetaData_Age\": \"99\", \"MetaData_Birthday\": \"10/31/1981 4:32:59 PM\", \"MetaData_Initials\": \"JR\", \"Other\": [\"12345\",\"10/31/2017 4:32:59 PM\",\"60282dd2-f79a-4400-be01-bcb0e86e7bc6\"], \"hardcoded\": \"true\"}}");

            var myParams = new MyComplexQueryParams
            {
                FirstName = "John",
                LastName = "Rambo"
            };
            myParams.Address.Postcode = 9999;
            myParams.Address.Street = "HomeStreet 99";

            myParams.MetaData.Add("Age", 99);
            myParams.MetaData.Add("Initials", "JR");
            myParams.MetaData.Add("Birthday", new DateTime(1981, 10, 31, 16, 24, 59));

            myParams.Other.Add(12345);
            myParams.Other.Add(new DateTime(2017, 10, 31, 16, 24, 59));
            myParams.Other.Add(new Guid("60282dd2-f79a-4400-be01-bcb0e86e7bc6"));


            var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>("https://httpbin.org", settings);

            var resp = await fixture.PostQuery(myParams);

            Assert.Equal("John", resp.Args["FirstName"]);
            Assert.Equal("Rambo", resp.Args["LastName"]);
            Assert.Equal("9999", resp.Args["Addr_Zip"]);
        }

        [Fact]
        public async Task GenericMethodTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            const string response = "4";
            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                    .Respond("application/json", response);

            var myParams = new Dictionary<string, object>
            {
                ["FirstName"] = "John",
                ["LastName"] = "Rambo",
                ["Address"] = new
                {
                    Zip = 9999,
                    Street = "HomeStreet 99"
                }
            };

            var fixture = RestService.For<IHttpBinApi<HttpBinGet, Dictionary<string, object>, int>>("https://httpbin.org", settings);

            // Use the generic to get it as an ApiResponse of string
            var resp = await fixture.GetQuery1<ApiResponse<string>>(myParams);
            Assert.Equal(response, resp.Content);

            mockHttp.VerifyNoOutstandingExpectation();

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                    .Respond("application/json", response);

            // Get as string
            var resp1 = await fixture.GetQuery1<string>(myParams);

            Assert.Equal(response, resp1);

            mockHttp.VerifyNoOutstandingExpectation();

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                    .Respond("application/json", response);


            var resp2 = await fixture.GetQuery1<int>(myParams);
            Assert.Equal(4, resp2);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task InheritedMethodTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var fixture = RestService.For<IAmInterfaceC>("https://httpbin.org", settings);

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get").Respond("application/json", nameof(IAmInterfaceA.Ping));
            var resp = await fixture.Ping();
            Assert.Equal(nameof(IAmInterfaceA.Ping), resp);
            mockHttp.VerifyNoOutstandingExpectation();

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .Respond("application/json", nameof(IAmInterfaceB.Pong));
            resp = await fixture.Pong();
            Assert.Equal(nameof(IAmInterfaceB.Pong), resp);
            mockHttp.VerifyNoOutstandingExpectation();

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .Respond("application/json", nameof(IAmInterfaceC.Pang));
            resp = await fixture.Pang();
            Assert.Equal(nameof(IAmInterfaceC.Pang), resp);
            mockHttp.VerifyNoOutstandingExpectation();
        }


        [Fact]
        public async Task InheritedInterfaceWithOnlyBaseMethodsTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var fixture = RestService.For<IContainAandB>("https://httpbin.org", settings);

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get").Respond("application/json", nameof(IAmInterfaceA.Ping));
            var resp = await fixture.Ping();
            Assert.Equal(nameof(IAmInterfaceA.Ping), resp);
            mockHttp.VerifyNoOutstandingExpectation();

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .Respond("application/json", nameof(IAmInterfaceB.Pong));
            resp = await fixture.Pong();
            Assert.Equal(nameof(IAmInterfaceB.Pong), resp);
            mockHttp.VerifyNoOutstandingExpectation();
        }


        [Fact]
        public async Task InheritedInterfaceWithoutRefitInBaseMethodsTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            var fixture = RestService.For<IImplementTheInterfaceAndUseRefit>("https://httpbin.org", settings);

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/doSomething")
                    .WithQueryString("parameter", "4")
                    .Respond("application/json", nameof(IImplementTheInterfaceAndUseRefit.DoSomething));

            await fixture.DoSomething(4);
            mockHttp.VerifyNoOutstandingExpectation();




            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/DoSomethingElse")
                .Respond("application/json", nameof(IImplementTheInterfaceAndUseRefit.DoSomethingElse));
            await fixture.DoSomethingElse();
            mockHttp.VerifyNoOutstandingExpectation();


            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/DoSomethingElse")
    .Respond("application/json", nameof(IImplementTheInterfaceAndUseRefit.DoSomethingElse));
            await ((IAmInterfaceEWithNoRefit<int>)fixture).DoSomethingElse();
            mockHttp.VerifyNoOutstandingExpectation();


            Assert.Throws<InvalidOperationException>(() => RestService.For<IAmInterfaceEWithNoRefit<int>>("https://httpbin.org"));
        }

        [Fact]
        public async Task DictionaryDynamicQueryparametersTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .Respond("application/json", "{\"url\": \"https://httpbin.org/get?hardcoded=true&FirstName=John&LastName=Rambo&Address_Zip=9999&Address_Street=HomeStreet 99\", \"args\": {\"Address_Street\": \"HomeStreet 99\",\"Address_Zip\": \"9999\",\"FirstName\": \"John\",\"LastName\": \"Rambo\",\"hardcoded\": \"true\"}}");

            var myParams = new Dictionary<string, object>
            {
                ["FirstName"] = "John",
                ["LastName"] = "Rambo",
                ["Address"] = new
                {
                    Zip = 9999,
                    Street = "HomeStreet 99"
                }
            };

            var fixture = RestService.For<IHttpBinApi<HttpBinGet, Dictionary<string, object>, int>>("https://httpbin.org", settings);

            var resp = await fixture.GetQuery(myParams);

            Assert.Equal("John", resp.Args["FirstName"]);
            Assert.Equal("Rambo", resp.Args["LastName"]);
            Assert.Equal("9999", resp.Args["Address_Zip"]);
        }

        [Fact]
        public async Task ComplexDynamicQueryparametersTestWithIncludeParameterName()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://httpbin.org/get")
                .Respond("application/json", "{\"url\": \"https://httpbin.org/get?search.FirstName=John&search.LastName=Rambo&search.Addr.Zip=9999&search.Addr.Street=HomeStreet 99\", \"args\": {\"search.Addr.Street\": \"HomeStreet 99\",\"search.Addr.Zip\": \"9999\",\"search.FirstName\": \"John\",\"search.LastName\": \"Rambo\"}}");

            var myParams = new MyComplexQueryParams
            {
                FirstName = "John",
                LastName = "Rambo"
            };
            myParams.Address.Postcode = 9999;
            myParams.Address.Street = "HomeStreet 99";

            var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>("https://httpbin.org/get", settings);

            var resp = await fixture.GetQueryWithIncludeParameterName(myParams);

            Assert.Equal("John", resp.Args["search.FirstName"]);
            Assert.Equal("Rambo", resp.Args["search.LastName"]);
            Assert.Equal("9999", resp.Args["search.Addr.Zip"]);
        }

        [Fact]
        public async Task ServiceOutsideNamespaceGetRequest()
        {
            var mockHttp = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "http://foo/")
                // We can't add HttpContent to a GET request,
                // because HttpClient doesn't allow it and it will
                // blow up at runtime
                .With(r => r.Content == null)
                .Respond("application/json", "Ok");

            var fixture = RestService.For<IServiceWithoutNamespace>("http://foo", settings);

            await fixture.GetRoot();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task ServiceOutsideNamespacePostRequest()
        {
            var mockHttp = new MockHttpMessageHandler();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://foo/")
                .Respond("application/json", "Ok");

            var fixture = RestService.For<IServiceWithoutNamespace>("http://foo", settings);

            await fixture.PostRoot();

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CanSerializeContentAsXml()
        {
            var mockHttp = new MockHttpMessageHandler();
            var contentSerializer = new XmlContentSerializer();
            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
                ContentSerializer = contentSerializer
            };

            mockHttp
                .Expect(HttpMethod.Post, "/users")
                .WithHeaders("Content-Type:application/xml; charset=utf-8")
                .Respond(req => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<User><Name>Created</Name></User>", Encoding.UTF8, "application/xml")
                });

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.CreateUser(new User()).ConfigureAwait(false);

            Assert.Equal("Created", result.Name);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public void ShouldTrimTrailingForwardSlashFromBaseUrl()
        {
            var expectedBaseAddress = "http://example.com/api";
            var inputBaseAddress = "http://example.com/api/";

            var fixture = RestService.For<ITrimTrailingForwardSlashApi>(inputBaseAddress);

            Assert.Equal(fixture.Client.BaseAddress.AbsoluteUri, expectedBaseAddress);
        }

        [Fact]
        public void ShouldThrowArgumentExceptionIfHostUrlIsNull()
        {
            try
            {
                RestService.For<IValidApi>(hostUrl: null);
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("hostUrl", ex.ParamName);
                return;
            }

            Assert.False(true, "Exception not thrown.");
        }

        [Fact]
        public void ShouldThrowArgumentExceptionIfHostUrlIsEmpty()
        {
            try
            {
                RestService.For<IValidApi>(hostUrl: "");
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("hostUrl", ex.ParamName);
                return;
            }

            Assert.False(true, "Exception not thrown.");
        }

        [Fact]
        public void ShouldThrowArgumentExceptionIfHostUrlIsWhitespace()
        {
            try
            {
                RestService.For<IValidApi>(hostUrl: " ");
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("hostUrl", ex.ParamName);
                return;
            }

            Assert.False(true, "Exception not thrown.");
        }

        [Fact]
        public void NonGenericCreate()
        {
            var expectedBaseAddress = "http://example.com/api";
            var inputBaseAddress = "http://example.com/api/";

            var fixture = RestService.For(typeof(ITrimTrailingForwardSlashApi), inputBaseAddress) as ITrimTrailingForwardSlashApi;

            Assert.Equal(fixture.Client.BaseAddress.AbsoluteUri, expectedBaseAddress);
        }

        [Fact]
        public async Task TypeCollisionTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings
            {
                HttpMessageHandlerFactory = () => mockHttp,
            };

            const string Url = "https://httpbin.org/get";

            mockHttp.Expect(HttpMethod.Get, Url)
                .Respond("application/json", "{ }");

            var fixtureA = RestService.For<ITypeCollisionApiA>(Url);

            var respA = await fixtureA.SomeARequest();

            var fixtureB = RestService.For<ITypeCollisionApiB>(Url);

            var respB = await fixtureB.SomeBRequest();

            Assert.IsType<CollisionA.SomeType>(respA);
            Assert.IsType<CollisionB.SomeType>(respB);
        }




        internal static Stream GetTestFileStream(string relativeFilePath)
        {
            const char namespaceSeparator = '.';

            // get calling assembly
            var assembly = Assembly.GetCallingAssembly();

            // compute resource name suffix
            var relativeName = "." + relativeFilePath
                .Replace('\\', namespaceSeparator)
                .Replace('/', namespaceSeparator)
                .Replace(' ', '_');

            // get resource stream
            var fullName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(relativeName, StringComparison.InvariantCulture));
            if (fullName == null)
            {
                throw new Exception($"Unable to find resource for path \"{relativeFilePath}\". Resource with name ending on \"{relativeName}\" was not found in assembly.");
            }

            var stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null)
            {
                throw new Exception($"Unable to find resource for path \"{relativeFilePath}\". Resource named \"{fullName}\" was not found in assembly.");
            }

            return stream;
        }
    }
}
