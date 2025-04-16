using System.Text.Json.Serialization;
using Refit;

// Note: this is for testing purposes, Refit does not currently support AOT.
var settings = new RefitSettings()
{
    ContentSerializer = new AotContentSerializer(SandboxJsonSerializerContext.Default),
};

var gitHubApi = RestService.For<IGitHubApi>("https://api.github.com", settings);
var octocat = await gitHubApi.GetUser("octocat");
Console.WriteLine(octocat);

Console.ReadLine();

public interface IGitHubApi
{
    [Get("/users/{user}")]
    [Headers("User-Agent: Refit")]
    Task<User> GetUser(string user);
}

public record User
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("blog")]
    public string Blog { get; set; }
}

[JsonSerializable(typeof(User))]
partial class SandboxJsonSerializerContext : JsonSerializerContext;
