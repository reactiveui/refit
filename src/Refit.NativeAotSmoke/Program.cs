using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refit;

var handler = new NativeAotSmokeHandler();
using var client = new HttpClient(handler)
{
    BaseAddress = new Uri("https://aot.refit.test")
};

var jsonOptions = new JsonSerializerOptions(AotJsonContext.Default.Options)
{
    TypeInfoResolver = AotJsonContext.Default
};

var api = RestService.For<INativeAotApi>(
    client,
    new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
    }
);

var created = await api.CreateTodo(new Todo("prove native aot")).ConfigureAwait(false);
if (created.Id != 42 || created.Title != "prove native aot")
{
    throw new InvalidOperationException("The AOT POST response was not deserialized correctly.");
}

var status = await api.GetStatus().ConfigureAwait(false);
if (!status.IsSuccessStatusCode || status.Content?.Name != "native-aot")
{
    throw new InvalidOperationException("The AOT ApiResponse<T> result was not deserialized correctly.");
}

if (!handler.SawPostBody)
{
    throw new InvalidOperationException("The AOT request body was not serialized through Refit.");
}

Console.WriteLine("Native AOT Refit smoke test passed.");

public interface INativeAotApi
{
    [Post("/todos")]
    Task<Todo> CreateTodo([Body] Todo todo);

    [Get("/status")]
    Task<ApiResponse<ServiceStatus>> GetStatus();
}

public sealed record Todo(string Title)
{
    public int Id { get; init; }
}

public sealed record ServiceStatus(string Name);

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(ServiceStatus))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
)]
internal sealed partial class AotJsonContext : JsonSerializerContext;

sealed class NativeAotSmokeHandler : HttpMessageHandler
{
    public bool SawPostBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (request.RequestUri?.AbsolutePath == "/todos")
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            SawPostBody = body.Contains("prove native aot", StringComparison.Ordinal);
            return Json("""{"id":42,"title":"prove native aot"}""");
        }

        if (request.RequestUri?.AbsolutePath == "/status")
        {
            return Json("""{"name":"native-aot"}""");
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
}
