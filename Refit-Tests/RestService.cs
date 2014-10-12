using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;

using NUnit.Framework;
using Newtonsoft.Json;

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
    }
}
