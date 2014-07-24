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
    public class User
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Blog { get; set; }
        public string Location { get; set; }
        public string Email { get; set; }
        public bool Hireable { get; set; }
        public string Bio { get; set; }
        public int PublicRepos { get; set; }
        public int Followers { get; set; }
        public int Following { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public int PublicGists { get; set; }
    }

	public class UserSearchResult
	{
		public int TotalCount { get; set; }
		public bool IncompleteResults { get; set; }
		public IList<User> Items { get; set; }
	}

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IGitHubApi
    {
        [Get("/users/{username}")]
        Task<User> GetUser(string userName);

        [Get("/users/{username}")]
        IObservable<User> GetUserObservable(string userName);

		[Get("/orgs/{orgname}/members")]
		Task<List<User>> GetOrgMembers(string orgName);

		[Get("/search/users")]
		Task<UserSearchResult> FindUsers(string q);

        [Get("/")]
        Task<HttpResponseMessage> GetIndex();

        [Get("/give-me-some-404-action")]
        Task NothingToSeeHere();
    }

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

			Assert.Greater(result.Count, 0);
			Assert.IsTrue(result.Any(member => member.Type == "User"));
		}

		[Test]
		public async Task HitTheGitHubUserSearchApi()
		{
			var fixture = RestService.For<IGitHubApi>("https://api.github.com");
			JsonConvert.DefaultSettings = 
				() => new JsonSerializerSettings { ContractResolver = new SnakeCasePropertyNamesContractResolver() };

			var result = await fixture.FindUsers("tom repos:>42 followers:>1000");

			Assert.Greater(result.TotalCount, 0);
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
        public void PostToRequestBin()
        {
            var fixture = RestService.For<IRequestBin>("http://requestb.in/");
            var result = fixture.Post();

            try {
                result.Wait();
            } catch (AggregateException ae) {
                ae.Handle(x => {
                    if (x is ApiException) {
                        // we should be good but maybe a 404 occurred
                        return true;
                    }

                    // other exception types might be valid failures
                    return false;
                });
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

        public interface IRequestBin
        {
            [Post("/1h3a5jm1")]
            Task Post();
        }
    }
}

