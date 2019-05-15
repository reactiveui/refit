using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Refit; // InterfaceStubGenerator looks for this

using static System.Math; // This is here to verify https://github.com/reactiveui/refit/issues/283

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
        public bool? Hireable { get; set; }
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

        [Get("/users/{userName}")]
        IObservable<User> GetUserCamelCase(string userName);

        [Get("/orgs/{orgname}/members")]
        Task<List<User>> GetOrgMembers(string orgName);

        [Get("/search/users")]
        Task<UserSearchResult> FindUsers(string q);

        [Get("/")]
        Task<HttpResponseMessage> GetIndex();

        [Get("/")]
        IObservable<string> GetIndexObservable();

        [Get("/give-me-some-404-action")]
        Task<User> NothingToSeeHere();

        [Get("/give-me-some-404-action")]
        Task<ApiResponse<User>> NothingToSeeHereWithMetadata();

        [Get("/users/{username}")]
        Task<ApiResponse<User>> GetUserWithMetadata(string userName);

        [Get("/users/{username}")]
        IObservable<ApiResponse<User>> GetUserObservableWithMetadata(string userName);

        [Post("/users")]
        Task<User> CreateUser(User user);

        [Post("/users")]
        Task<ApiResponse<User>> CreateUserWithMetadata(User user);
    }

    public class TestNested
    {
        [Headers("User-Agent: Refit Integration Tests")]
        public interface INestedGitHubApi
        {
            [Get("/users/{username}")]
            Task<User> GetUser(string userName);

            [Get("/users/{username}")]
            IObservable<User> GetUserObservable(string userName);

            [Get("/users/{userName}")]
            IObservable<User> GetUserCamelCase(string userName);

            [Get("/orgs/{orgname}/members")]
            Task<List<User>> GetOrgMembers(string orgName);

            [Get("/search/users")]
            Task<UserSearchResult> FindUsers(string q);

            [Get("/")]
            Task<HttpResponseMessage> GetIndex();

            [Get("/")]
            IObservable<string> GetIndexObservable();

            [Get("/give-me-some-404-action")]
            Task NothingToSeeHere();
        }
    }
}
