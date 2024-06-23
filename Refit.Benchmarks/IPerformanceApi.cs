namespace Refit.Benchmarks;

public interface IPerformanceApi
{
    [Get("/users")]
    public Task<string> ConstantRoute();

    [Get("/users/{id}")]
    public Task<string> DynamicRoute(int id);

    [Get("/users/{id}/{user}/{status}")]
    public Task<string> ComplexDynamicRoute(int id, string user, string status);

    [Get("/users/{request.someProperty}")]
    public Task<string> ObjectRequest(PathBoundObject request);

    [Post("/users/{id}/{request.someProperty}")]
    [Headers("User-Agent: Awesome Octocat App", "X-Emoji: :smile_cat:")]
    public Task<string> ComplexRequest(int id, PathBoundObject request, [Query(CollectionFormat.Multi)]int[] queries);
}

public class PathBoundObject
{
    public string SomeProperty { get; set; }

    [Query]
    public string SomeQuery { get; set; }
}
