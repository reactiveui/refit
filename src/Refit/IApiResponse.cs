// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace Refit;

/// <summary>Base interface used to represent an API response.</summary>
public interface IApiResponse : IDisposable
{
    /// <summary>Gets the HTTP response headers.</summary>
    HttpResponseHeaders? Headers { get; }

    /// <summary>Gets the HTTP response content headers as defined in RFC 2616.</summary>
    HttpContentHeaders? ContentHeaders { get; }

    /// <summary>Gets a value indicating whether the request was successful.</summary>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    [MemberNotNullWhen(false, nameof(Error))]
    bool IsSuccessStatusCode { get; }

    /// <summary>
    /// Gets a value indicating whether the request was successful and there wasn't any other error (for example, during content deserialization).
    /// </summary>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    [MemberNotNullWhen(false, nameof(Error))]
    bool IsSuccessful { get; }

    /// <summary>Gets a value indicating whether a response was received from the server.</summary>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    [MemberNotNullWhen(false, nameof(Error))]
    bool IsReceived { get; }

    /// <summary>Gets the HTTP response status code.</summary>
    HttpStatusCode? StatusCode { get; }

    /// <summary>Gets the reason phrase which typically is sent by the server together with the status code.</summary>
    string? ReasonPhrase { get; }

    /// <summary>Gets the HTTP Request message which led to this response.</summary>
    HttpRequestMessage? RequestMessage { get; }

    /// <summary>Gets the HTTP Message version.</summary>
    Version? Version { get; }

    /// <summary>Gets the exception object in case of unsuccessful request or response.</summary>
    /// <remarks>
    /// <see cref="HasRequestError"/> and <see cref="HasResponseError"/> methods can be used to check the type of error.
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "By Design")]
    ApiExceptionBase? Error { get; }

    /// <summary>Checks if the call failed before a response was received from the server.</summary>
    /// <param name="error">The <see cref="ApiRequestException"/> object in case of an unsuccessful request.</param>
    /// <returns><c>true</c> if the call failed before a response was received from the server, otherwise <c>false</c>.</returns>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "By Design")]
    bool HasRequestError(
        [MaybeNullWhen(false)] out ApiRequestException? error);

    /// <summary>Checks if the call failed due to an unsuccessful response from the server.</summary>
    /// <param name="error">The <see cref="ApiException"/> object in case of unsuccessful response.</param>
    /// <returns><c>true</c> if the call failed due to an unsuccessful response from the server, otherwise <c>false</c>.</returns>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "By Design")]
    bool HasResponseError(
        [MaybeNullWhen(false)]
        out ApiException? error);
}
