using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Newtonsoft.Json;
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

    public class HttpBinGet
    {
        public Dictionary<string, string> Args { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Origin { get; set; }
        public string Url { get; set; }
    }

    [TestFixture]
    public class RestServiceIntegrationTests
    {
        [Test]
        public async Task HitTheGitHubUserApi()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetUser("octocat");

            Assert.AreEqual("octocat", result.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Test]
        public async Task HitWithCamelCaseParameter()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings =
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetUserCamelCase("octocat");

            Assert.AreEqual("octocat", result.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Test]
        public async Task HitTheGitHubOrgMembersApi()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetOrgMembers("github");

            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(member => member.Type == "User"));
        }

        [Test]
        public async Task HitTheGitHubUserSearchApi()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

            Assert.IsTrue(result.TotalCount > 0);
            Assert.IsTrue(result.Items.Any(member => member.Type == "User"));
        }

        [Test]
        public async Task HitTheGitHubUserApiAsObservable()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = 
                () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

            var result = await fixture.GetUserObservable("octocat")
                .Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual("octocat", result.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Test]
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
            Assert.AreEqual("octocat", result2.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result2.AvatarUrl));
        }

        [Test]
        public async Task HitTheGitHubUserApiWithSettingsObj()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var fixture = RestService.For<IGitHubApi>(
                "https://api.github.com",
                new RefitSettings{
                    JsonSerializerSettings = new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() }
                });


            var result = await fixture.GetUser("octocat");

            Assert.AreEqual("octocat", result.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Test]
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

            Assert.AreEqual("octocat", result.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Test]
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

            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(member => member.Type == "User"));
        }

        [Test]
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

            Assert.IsTrue(result.TotalCount > 0);
            Assert.IsTrue(result.Items.Any(member => member.Type == "User"));
        }

        [Test]
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

            Assert.AreEqual("octocat", result.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result.AvatarUrl));
        }

        [Test]
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
            Assert.AreEqual("octocat", result2.Login);
            Assert.IsFalse(String.IsNullOrEmpty(result2.AvatarUrl));
        }


        [Test]
        public async Task TwoSubscriptionsResultInTwoRequests()
        {
            var input = new TestHttpMessageHandler();
            var client = new HttpClient(input) { BaseAddress = new Uri("http://foo") };
            var fixture = RestService.For<IGitHubApi>(client);

            Assert.AreEqual(0, input.MessagesSent);

            var obs = fixture.GetIndexObservable()
                .Timeout(TimeSpan.FromSeconds(10));

            await obs;
            Assert.AreEqual(1, input.MessagesSent);

            var result = await obs;
            Assert.AreEqual(2, input.MessagesSent);

            // NB: TestHttpMessageHandler returns what we tell it to ('test' by default)
            Assert.IsTrue(result.Contains("test"));
        }

        [Test]
        public async Task ShouldRetHttpResponseMessage()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            var result = await fixture.GetIndex();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccessStatusCode);
        }

        [Test]
        public async Task HitTheNpmJs()
        {
            var fixture = RestService.For<INpmJs>("https://registry.npmjs.org");
            var result = await fixture.GetCongruence();

            Assert.AreEqual("congruence", result._id);
        }

        [Test]
        public async Task PostToRequestBin()
        {
            var fixture = RestService.For<IRequestBin>("http://httpbin.org/");
            
            try {
                await fixture.Post();
            } catch (ApiException ex) { 
                // we should be good but maybe a 404 occurred
            }
        }

        [Test]
        public async Task CanGetDataOutOfErrorResponses() 
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            try {
                await fixture.NothingToSeeHere();
                Assert.Fail();
            } catch (ApiException exception) {
                Assert.AreEqual(HttpStatusCode.NotFound, exception.StatusCode);
                var content = exception.GetContentAs<Dictionary<string, string>>();

                Assert.AreEqual("Not Found", content["message"]);
                Assert.IsNotNull(content["documentation_url"]);
            }
        }

        [Test]
        public void NonRefitInterfacesThrowMeaningfulExceptions() 
        {
            try {
                RestService.For<INoRefitHereBuddy>("http://example.com");
            } catch (InvalidOperationException exception) {
                StringAssert.StartsWith("INoRefitHereBuddy", exception.Message);
            }
        }

        [Test]
        public async Task NonRefitMethodsThrowMeaningfulExceptions() 
        {
            try {
                var fixture = RestService.For<IAmHalfRefit>("http://example.com");
                await fixture.Get();
            } catch (NotImplementedException exception) {
                StringAssert.Contains("no Refit HTTP method attribute", exception.Message);
            }
        }

        [Test]
        public async Task GenericsWork() 
        {
            var fixture = RestService.For<IHttpBinApi<HttpBinGet, string, int>>("http://httpbin.org/get");

            var result = await fixture.Get("foo", 99);

            Assert.AreEqual("http://httpbin.org/get?param=foo", result.Url);
            Assert.AreEqual("foo", result.Args["param"]);
            Assert.AreEqual("99", result.Headers["X-Refit"]);
        }

        [Test]
        public async Task ValueTypesArentValidButTheyWorkAnyway()
        {
            var handler = new TestHttpMessageHandler("true");

            var fixture = RestService.For<IBrokenWebApi>(new HttpClient(handler) { BaseAddress = new Uri("http://nowhere.com") });

            var result = await fixture.PostAValue("Does this work?");

            Assert.AreEqual(true, result);
        }
    }
}
