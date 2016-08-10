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
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetUser("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Fact]
        public async Task HitWithCamelCaseParameter()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetUserCamelCase("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Fact]
        public async Task HitTheGitHubOrgMembersApi()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetOrgMembers("github");

            Assert.True(result.Count > 0);
            Assert.True(result.Any(member => member.Type == "User"));
        }

        [Fact]
        public async Task HitTheGitHubUserSearchApi()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

            Assert.True(result.TotalCount > 0);
            Assert.True(result.Items.Any(member => member.Type == "User"));
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservable()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetUserObservable("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservableAndSubscribeAfterTheFact()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

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
        public async Task HitTheGitHubUserApiWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings{
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });


            var result = await fixture.GetUser("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Fact]
        public async Task HitWithCamelCaseParameterWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings
                {
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });


            var result = await fixture.GetUserCamelCase("octocat");

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Fact]
        public async Task HitTheGitHubOrgMembersApiWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings
                {
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });


            var result = await fixture.GetOrgMembers("github");

            Assert.True(result.Count > 0);
            Assert.True(result.Any(member => member.Type == "User"));
        }

        [Fact]
        public async Task HitTheGitHubUserSearchApiWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings
                {
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });

            var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

            Assert.True(result.TotalCount > 0);
            Assert.True(result.Items.Any(member => member.Type == "User"));
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservableWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings
                {
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });


            var result = await fixture.GetUserObservable("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            Assert.Equal("octocat", result.Login);
            Assert.False(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Fact]
        public async Task HitTheGitHubUserApiAsObservableAndSubscribeAfterTheFactWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings
                {
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });


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
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            var result = await fixture.GetIndex();

            Assert.NotNull(result);
            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact]
        public async Task HitTheNpmJs()
        {
            var fixture = RestService.For<INpmJs>("https://registry.npmjs.org");
            var result = await fixture.GetCongruence();

            Assert.Equal("congruence", result._id);
        }

        [Fact]
        public async Task PostToRequestBin()
        {
            var fixture = RestService.For<IRequestBin>("http://httpbin.org/");
            
            try {
                await fixture.Post();
            } catch (ApiException ex) { 
                // we should be good but maybe a 404 occurred
            }
        }

        [Fact]
        public async Task CanGetDataOutOfErrorResponses() 
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
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
            var fixture = RestService.For<IHttpBinApi<HttpBinGet, string, int>>("http://httpbin.org/get");

            var result = await fixture.Get("foo", 99);

            Assert.Equal("http://httpbin.org/get?param=foo", result.Url);
            Assert.Equal("foo", result.Args["param"]);
            Assert.Equal("99", result.Headers["X-Refit"]);
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
