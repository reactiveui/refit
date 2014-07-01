using System;
using System.Net.Http;
using NUnit.Framework;
using System.Threading.Tasks;
using Refit.Tests.Support;
using Newtonsoft.Json;
using Refit.Tests.Support.Serialization;

namespace Refit.Tests
{
    public class User
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string AvatarUrl { get; set; }
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

