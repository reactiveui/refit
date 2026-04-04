using Refit;

namespace BlazorWasmIssue2065;

internal interface IIssue2067Api
{
    [Get("/sample-data/status.json")]
    Task<Issue2067Response> GetStatusAsync();
}

internal sealed class Issue2067Response
{
    public Issue2067Status Status { get; set; }
}

internal enum Issue2067Status
{
    [System.Text.Json.Serialization.JsonStringEnumMemberName("totally-ready")]
    TotallyReady,

    NeedsReview
}
