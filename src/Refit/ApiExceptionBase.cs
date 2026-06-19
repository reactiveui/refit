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
[Serializable]
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
    public HttpRequestMessage RequestMessage { get; }

    /// <summary>Gets the Refit settings used to send the request.</summary>
    public RefitSettings RefitSettings { get; }
}
