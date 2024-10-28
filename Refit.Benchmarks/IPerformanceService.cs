namespace Refit.Benchmarks;

public interface IPerformanceService
{
    [Get("/users")]
    public Task<HttpResponseMessage> ConstantRoute();

    [Get("/users/{id}")]
    public Task<HttpResponseMessage> DynamicRoute(int id);

    [Get("/users/{id}/{user}/{status}")]
    public Task<HttpResponseMessage> ComplexDynamicRoute(int id, string user, string status);

    [Get("/users/{request.someProperty}")]
    public Task<HttpResponseMessage> ObjectRequest(PathBoundObject request);

    [Post("/users/{id}/{request.someProperty}")]
    [Headers("User-Agent: Awesome Octocat App", "X-Emoji: :smile_cat:")]
    public Task<HttpResponseMessage> ComplexRequest(int id, PathBoundObject request, [Query(CollectionFormat.Multi)]int[] queries);
}

public class PathBoundObject
{
    public string SomeProperty { get; set; }

    [Query]
    public string SomeQuery { get; set; }
}
