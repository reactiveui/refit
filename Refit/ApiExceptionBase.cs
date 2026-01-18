using System.Net.Http;

namespace Refit;

/// <summary>
/// Represents an error that occured while sending an API request.
/// </summary>
[Serializable]
#pragma warning disable CA1032 // Implement standard exception constructors
public abstract class ApiExceptionBase : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    /// <summary>
    /// The HTTP method used to send the request.
    /// </summary>
    public HttpMethod HttpMethod { get; }

    /// <summary>
    /// The <see cref="System.Uri"/> used to send the HTTP request.
    /// </summary>
    public Uri? Uri => RequestMessage.RequestUri;

    /// <summary>
    /// The HTTP Request message used to send the request.
    /// </summary>
    public HttpRequestMessage RequestMessage { get; }

    /// <summary>
    /// Refit settings used to send the request.
    /// </summary>
    public RefitSettings RefitSettings { get; }

    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="refitSettings">The refit settings used to send the request.</param>
    /// <param name="innerException">The exception that is the cause of the <see cref="ApiRequestException"/>.</param>
    protected ApiExceptionBase(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings,
        Exception innerException
    )
        : this(
            innerException?.Message ?? throw new ArgumentNullException(nameof(innerException)),
            message,
            httpMethod,
            refitSettings,
            innerException
        )
    { }

    /// <summary>
    /// Initializes a new instance of the exception with a custom exception message.
    /// </summary>
    /// <param name="exceptionMessage">The exception message.</param>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="refitSettings">The refit settings used to send the request.</param>
    /// <param name="innerException">The exception that is the cause of the API exception.</param>
    protected ApiExceptionBase(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings,
        Exception? innerException = null
    )
        : base(exceptionMessage, innerException)
    {
        RequestMessage = message;
        HttpMethod = httpMethod;
        RefitSettings = refitSettings;
    }
}
