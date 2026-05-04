using Refit;

namespace BlazorWasmIssue2065;

public interface IIssue2065Api
{
    [Get("/sample-data/weather.json")]
    Task<string> GetPayload();
}
