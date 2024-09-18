// See https://aka.ms/new-console-template for more information

using Refit;

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com");
Console.WriteLine("Success?");

public interface IGitHubApi
{
    [Get("/users/{user}")]
    Task<User> GetUser(string user);
}

public record User();
