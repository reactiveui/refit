using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;

namespace Refit.Tests
{
    public class User
    {
        public string login { get; set; }
        public int id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public string blog { get; set; }
        public string location { get; set; }
        public string email { get; set; }
        public bool hireable { get; set; }
        public string bio { get; set; }
        public int public_repos { get; set; }
        public int followers { get; set; }
        public int following { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public int public_gists { get; set; }
    }

    public interface IGitHubApi
    {
        [Get("/users/{username}")]
        Task<User> GetUser(string userName);

        [Get("/")]
        Task<HttpResponseMessage> GetIndex();
    }

    [TestFixture]
    public class RestServiceIntegrationTests
    {
        [Test]
        public void HitTheGitHubUserAPI()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            var result = fixture.GetUser("octocat");

            result.Wait();
            Assert.AreEqual("octocat", result.Result.login);
        }

        [Test]
        public void ShouldRetHttpResponseMessage() 
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            var result = fixture.GetIndex();

            result.Wait();
            Assert.IsNotNull(result.Result);
            Assert.IsTrue(result.Result.IsSuccessStatusCode);
        }

        [Test]
        public void PostToRequestBin()
        {
            var fixture = RestService.For<IRequestBin>("http://requestb.in/");
            var result = fixture.Post();

            result.Wait();
        }

        interface IRequestBin
        {
            [Post("/1h3a5jm1")]
            Task Post();
        }
    }
}

