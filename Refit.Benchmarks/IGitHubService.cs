using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

using Refit;
using System.Threading.Tasks;

namespace Refit.Benchmarks
{
    public interface IGitHubService
    {
        //Task - throws
        [Get("/users")]
        public Task GetUsersTaskAsync();
        [Post("/users")]
        public Task PostUsersTaskAsync([Body] IEnumerable<User> users);

        //Task<string> - throws
        [Get("/users")]
        public Task<string> GetUsersTaskStringAsync();
        [Post("/users")]
        public Task<string> PostUsersTaskStringAsync([Body] IEnumerable<User> users);

        //Task<Stream> - throws
        [Get("/users")]
        public Task<Stream> GetUsersTaskStreamAsync();
        [Post("/users")]
        public Task<Stream> PostUsersTaskStreamAsync([Body] IEnumerable<User> users);

        //Task<HttpContent> - throws
        [Get("/users")]
        public Task<HttpContent> GetUsersTaskHttpContentAsync();
        [Post("/users")]
        public Task<HttpContent> PostUsersTaskHttpContentAsync([Body] IEnumerable<User> users);

        //Task<HttpResponseMessage>
        [Get("/users")]
        public Task<HttpResponseMessage> GetUsersTaskHttpResponseMessageAsync();
        [Post("/users")]
        public Task<HttpResponseMessage> PostUsersTaskHttpResponseMessageAsync([Body] IEnumerable<User> users);

        //IObservable<HttpResponseMessage>
        [Get("/users")]
        public IObservable<HttpResponseMessage> GetUsersObservableHttpResponseMessage();
        [Post("/users")]
        public IObservable<HttpResponseMessage> PostUsersObservableHttpResponseMessage([Body] IEnumerable<User> users);

        //Task<<T>> - throws
        [Get("/users")]
        public Task<List<User>> GetUsersTaskTAsync();
        [Post("/users")]
        public Task<List<User>> PostUsersTaskTAsync([Body] IEnumerable<User> users);

        //Task<ApiResponse<T>>
        [Get("/users")]
        public Task<ApiResponse<List<User>>> GetUsersTaskApiResponseTAsync();
        [Post("/users")]
        public Task<ApiResponse<List<User>>> PostUsersTaskApiResponseTAsync([Body] IEnumerable<User> users);
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public int Followers { get; set; }
        public int Following { get; set; }
        public string Url { get; set; }
    }
}


