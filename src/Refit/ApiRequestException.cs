// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Represents an error that occurred while sending an API request before a response could be received from the server.</summary>
/// <remarks>
/// This exception will wrap those thrown by <see cref="HttpClient.SendAsync(HttpRequestMessage)"/>,
/// such as <see cref="HttpRequestException"/> and <see cref="OperationCanceledException"/>.
/// </remarks>
[SuppressMessage(
    "Usage",
    "CA1032:Implement standard exception constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
[SuppressMessage(
    "Major Code Smell",
    "S4027:Exceptions should provide standard constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
[Serializable]
public class ApiRequestException : ApiExceptionBase
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
        RefitSettings refitSettings)
        : base(exceptionMessage, message, httpMethod, refitSettings)
    {
    }

    /// <inheritdoc/>
    public ApiRequestException(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        RefitSettings refitSettings,
        Exception? innerException)
        : base(exceptionMessage, message, httpMethod, refitSettings, innerException)
    {
    }
}
