using System.Net.Http;

namespace Refit;

/// <summary>
/// Represents an error that occurred while sending an API request before a response could be received from the server.
/// </summary>
/// <remarks>
/// This exception will wrap those thrown by <see cref="HttpClient.SendAsync(HttpRequestMessage)"/>,
/// such as <see cref="HttpRequestException"/> and <see cref="OperationCanceledException"/>.
/// </remarks>
[Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
public class ApiRequestException : ApiExceptionBase
#pragma warning restore CA1032 // Implement standard exception constructors
{
    /// <inheritdoc/>
    public ApiRequestException(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings,
        Exception innerException)
        : base(message, httpMethod, refitSettings, innerException)
    {
    }

    /// <inheritdoc/>
    public ApiRequestException(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings,
        Exception? innerException = null)
        : base(exceptionMessage, message, httpMethod, refitSettings, innerException)
    {
    }
}
