using System.Threading.Tasks;
using Refit; // Needed by the build task

namespace Refit.Profiler
{
    public interface IGitHubService
    {
        [Get("/users/{name}.json")]
        public Task<User> GetUserAsync(string name);
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
