// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Represents an error that occurred while sending an API request.</summary>
[SuppressMessage(
    "Usage",
    "CA1032:Implement standard exception constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
[SuppressMessage(
    "Major Code Smell",
    "S4027:Exceptions should provide standard constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
public abstract class ApiExceptionBase : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ApiExceptionBase"/> class.</summary>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="refitSettings">The refit settings used to send the request.</param>
    /// <param name="innerException">The exception that is the cause of the <see cref="ApiRequestException"/>.</param>
    protected ApiExceptionBase(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings,
        Exception innerException)
        : this(
            innerException?.Message ?? throw new ArgumentNullException(nameof(innerException)),
            message,
            httpMethod,
            refitSettings,
            innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApiExceptionBase"/> class with a custom exception message.</summary>
    /// <param name="exceptionMessage">The exception message.</param>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="refitSettings">The refit settings used to send the request.</param>
    protected ApiExceptionBase(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings)
        : this(exceptionMessage, message, httpMethod, refitSettings, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApiExceptionBase"/> class with a custom exception message.</summary>
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
        Exception? innerException)
        : base(exceptionMessage, innerException)
    {
        RequestMessage = message;
        HttpMethod = httpMethod;
        RefitSettings = refitSettings;
    }

    /// <summary>Gets the HTTP method used to send the request.</summary>
    public HttpMethod HttpMethod { get; }

    /// <summary>Gets the <see cref="System.Uri"/> used to send the HTTP request.</summary>
    public Uri? Uri => RequestMessage.RequestUri;

    /// <summary>Gets the HTTP Request message used to send the request.</summary>
    /// <remarks>
    /// This is the live request and still carries its headers, including the <c>Authorization</c> header (bearer
    /// token, basic credentials, or API key) and any secret <c>[Header]</c>/<c>[HeaderCollection]</c> values.
    /// <see cref="Exception.ToString"/> does not print them, but property-walking serializers (structured logging,
    /// telemetry) will. Use <see cref="RefitSettings.ExceptionRedactor"/> to scrub them before the exception
    /// propagates.
    /// </remarks>
    public HttpRequestMessage RequestMessage { get; }

    /// <summary>
    /// Gets or sets the request body content as a string, captured before sending. This is only populated when
    /// <see cref="RefitSettings.CaptureRequestContent"/> is enabled; otherwise the request content has
    /// already been disposed by <see cref="HttpClient"/> and cannot be read from <see cref="RequestMessage"/>.
    /// </summary>
    /// <remarks>
    /// When populated this is the raw, unredacted request body and frequently contains credentials or PII. The setter
    /// is accessible so <see cref="RefitSettings.ExceptionRedactor"/> can scrub it before the exception propagates.
    /// </remarks>
    public string? RequestContent { get; set; }

    /// <summary>Gets a value indicating whether the captured request content is available.</summary>
    [MemberNotNullWhen(true, nameof(RequestContent))]
    public bool HasRequestContent => !string.IsNullOrEmpty(RequestContent);

    /// <summary>Gets the Refit settings used to send the request.</summary>
    public RefitSettings RefitSettings { get; }
}
