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
    /// <remarks>
    /// A successful status code does not imply a response body. Per RFC 9110 a 204 (No Content),
    /// a 304 (Not Modified), a response to HEAD, or any 2xx with zero-length content carries no
    /// content, so this intentionally does not narrow <see cref="ContentHeaders"/>. Use
    /// <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> or <see cref="IApiResponse{T}.HasContent"/>
    /// when you need deserialized content.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    bool IsSuccessStatusCode { get; }

    /// <summary>
    /// Gets a value indicating whether the request was successful and there wasn't any other error (for example, during content deserialization).
    /// </summary>
    /// <remarks>
    /// "No error" is not "has content": a 2xx with a <c>null</c> or empty body deserializes to
    /// <c>null</c> without an error, so this does not narrow content. Use
    /// <see cref="IApiResponse{T}.IsSuccessfulWithContent"/> or <see cref="IApiResponse{T}.HasContent"/>
    /// when you need deserialized content.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    bool IsSuccessful { get; }

    /// <summary>Gets a value indicating whether a response was received from the server.</summary>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
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
    /// An unsuccessful response is not guaranteed to carry an error (for example, a non-2xx response
    /// constructed without one), so a failed state does not narrow this to non-null. Null-check it, or
    /// use <see cref="HasRequestError"/> / <see cref="HasResponseError"/> for null-safe typed access.
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
