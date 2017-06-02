using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
using Refit; // InterfaceStubGenerator looks for this
using RichardSzalay.MockHttp;


namespace Refit.Tests
{
    public class RootObject
    {
        public string _id { get; set; }
        public string _rev { get; set; }
        public string name { get; set; } 
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

    public interface IHttpBinApi<TResponse, in TParam, in THeader>
        where TResponse : class
        where THeader : struct
    {
        [Get("")]
        Task<TResponse> Get(TParam param, [Header("X-Refit")] THeader header);
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
    }

    public class HttpBinGet
    {
        public Dictionary<string, string> Args { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Origin { get; set; }
        public string Url { get; set; }
    }

    public class RestServiceIntegrationTests
    {
        [Fact]
        public async Task HitTheGitHubUserApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                    .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");
      

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetUser("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitWithCamelCaseParameter()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                   .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.GetUserCamelCase("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubOrgMembersApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/orgs/github/members")
                  .Respond("application/json", "[{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
     
            var result = await fixture.GetOrgMembers("github");

            Assert.True(result.Count > 0);
            Assert.True(result.Any(member => member.Type == "User"));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserSearchApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/search/users")
                    .WithQueryString("q", "tom repos:>42 followers:>1000")
                    .Respond("application/json", "{ 'total_count': 1, 'items': [{ 'login':'octocat', 'avatar_url':'http://foo/bar', 'type':'User'}]}");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);

            var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

            Assert.True(result.TotalCount > 0);
            Assert.True(result.Items.Any(member => member.Type == "User"));
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservable()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.Expect(HttpMethod.Get, "https://api.github.com/users/octocat")
                    .Respond("application/json", "{ 'login':'octocat', 'avatar_url':'http://foo/bar' }");

            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);


            var result = await fixture.GetUserObservable("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservableAndSubscribeAfterTheFact()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
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
            Assert.False(String.IsNullOrEmpty(result2.AvatarUrl));
        }
        
        [Fact]
        public async Task TwoSubscriptionsResultInTwoRequests()
        {
            var input = new TestHttpMessageHandler();

            // we need to use a factory here to ensure each request gets its own httpcontent instance
            input.ContentFactory = () => new StringContent("test");

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
            Assert.True(result1.Contains("test"));
            Assert.True(result2.Contains("test"));
        }

        [Fact]
        public async Task ShouldRetHttpResponseMessage()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.When(HttpMethod.Get, "https://api.github.com/")
                    .Respond(HttpStatusCode.OK);


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
            var result = await fixture.GetIndex();

            Assert.NotNull(result);
            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact]
        public async Task HitTheNpmJs()
        {
             var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "https://registry.npmjs.org/congruence")
                    .Respond("application/json", "{ '_id':'congruence', '_rev':'rev' , 'name':'name'}");
      


            var fixture = RestService.For<INpmJs>("https://registry.npmjs.org", settings);
            var result = await fixture.GetCongruence();

            Assert.Equal("congruence", result._id);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task PostToRequestBin()
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1")
                    .Respond(HttpStatusCode.OK);

            var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);
            
            try {
                await fixture.Post();
            } catch (ApiException ex) { 
                // we should be good but maybe a 404 occurred
            }

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CanGetDataOutOfErrorResponses() 
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp,
                JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
            };

            mockHttp.When(HttpMethod.Get, "https://api.github.com/give-me-some-404-action")
                    .Respond(HttpStatusCode.NotFound, "application/json", "{'message': 'Not Found', 'documentation_url': 'http://foo/bar'}");


            var fixture = RestService.For<IGitHubApi>("https://api.github.com", settings);
            try {
                await fixture.NothingToSeeHere();
                Assert.True(false);
            } catch (ApiException exception) {
                Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
                var content = exception.GetContentAs<Dictionary<string, string>>();

                Assert.Equal("Not Found", content["message"]);
                Assert.NotNull(content["documentation_url"]);
            }
        }

        [Fact]
        public void NonRefitInterfacesThrowMeaningfulExceptions() 
        {
            try {
                RestService.For<INoRefitHereBuddy>("http://example.com");
            } catch (InvalidOperationException exception) {
                Assert.StartsWith("INoRefitHereBuddy", exception.Message);
            }
        }

        [Fact]
        public async Task NonRefitMethodsThrowMeaningfulExceptions() 
        {
            try {
                var fixture = RestService.For<IAmHalfRefit>("http://example.com");
                await fixture.Get();
            } catch (NotImplementedException exception) {
                Assert.Contains("no Refit HTTP method attribute", exception.Message);
            }
        }

        [Fact]
        public async Task GenericsWork() 
        {
            var mockHttp = new MockHttpMessageHandler();

            var settings = new RefitSettings {
                HttpMessageHandlerFactory = () => mockHttp
            };

            mockHttp.Expect(HttpMethod.Get, "http://httpbin.org/get")
                    .WithHeaders("X-Refit", "99")
                    .WithQueryString("param", "foo")
                    .Respond("application/json", "{'url': 'http://httpbin.org/get?param=foo', 'args': {'param': 'foo'}, 'headers':{'X-Refit':'99'}}");



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

            Assert.Equal(true, result);
        }
    }
}
