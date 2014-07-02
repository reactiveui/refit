using System;
using System.Net.Http;
using NUnit.Framework;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Refit.Tests.Support.Serialization;

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

    [Headers("User-Agent: Refit Integration Tests")]
    public interface IGitHubApi
    {
        [Get("/users/{username}")]
        Task<User> GetUser(string userName);

        [Get("/")]
        Task<HttpResponseMessage> GetIndex();
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
        public void HitTheGitHubUserApi()
        {
            var fixture = RestService.For<IGitHubApi>("https://api.github.com");
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings() { ContractResolver = new SnakeCasePropertyNamesContractResolver() };
            var result = fixture.GetUser("octocat");

            result.Wait();
            Assert.AreEqual("octocat", result.Result.Login);
            Assert.IsNotEmpty(result.Result.AvatarUrl);
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
        public void HitTheNpmJs()
        {
            var fixture = RestService.For<INpmJs>("https://registry.npmjs.us/public");
            var result = fixture.GetCongruence();

            result.Wait();
            Assert.AreEqual("congruence", result.Result._id);
        }

        [Test]
        public void PostToRequestBin()
        {
            var fixture = RestService.For<IRequestBin>("http://requestb.in/");
            var result = fixture.Post();

            try
            {
                result.Wait();
            }
            catch (AggregateException ae)
            {
                ae.Handle(
                    x =>
                    {
                        if (x is HttpRequestException)
                        {
                            // we should be good but maybe a 404 occurred
                            return true;
                        }

                        // other exception types might be valid failures
                        return false;
                    });
            }
        }

        public interface IRequestBin
        {
            [Post("/1h3a5jm1")]
            Task Post();
        }
    }
}

