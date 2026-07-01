// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Refit.Testing;

/// <summary>
/// A hand-written <see cref="IApiResponse{T}"/> for unit-testing code that consumes a Refit interface
/// returning <see cref="IApiResponse{T}"/> or <see cref="ApiResponse{T}"/>, without going through HTTP.
/// Every member is an <c>init</c>-only property, so a test configures exactly the fields it needs and
/// leaves the rest defaulted.
/// </summary>
/// <remarks>
/// The nullability-narrowing contracts (for example <see cref="IApiResponse{T}.IsSuccessfulWithContent"/>
/// implying non-null <see cref="IApiResponse{T}.Content"/>) come from the interface declaration, so a
/// consumer's <c>if (response.IsSuccessfulWithContent)</c> flow-narrows exactly as it does against a real
/// response. Prefer <see cref="StubHttp"/> for end-to-end tests; reach for this only when the code under
/// test is handed an <see cref="IApiResponse{T}"/> directly.
/// </remarks>
/// <typeparam name="T">The deserialized response content type.</typeparam>
public sealed class StubApiResponse<T> : IApiResponse<T>
{
    /// <summary>Gets the deserialized response content.</summary>
    public T? Content { get; init; }

    /// <summary>Gets a value indicating whether deserialized content is available.</summary>
    public bool HasContent { get; init; }

    /// <summary>Gets a value indicating whether the request was successful and content is available.</summary>
    public bool IsSuccessfulWithContent { get; init; }

    /// <summary>Gets the HTTP response headers.</summary>
    public HttpResponseHeaders? Headers { get; init; }

    /// <summary>Gets the HTTP response content headers.</summary>
    public HttpContentHeaders? ContentHeaders { get; init; }

    /// <summary>Gets a value indicating whether the response has a success status code.</summary>
    public bool IsSuccessStatusCode { get; init; }

    /// <summary>Gets a value indicating whether the request was successful.</summary>
    public bool IsSuccessful { get; init; }

    /// <summary>Gets a value indicating whether a response was received from the server.</summary>
    public bool IsReceived { get; init; }

    /// <summary>Gets the HTTP response status code.</summary>
    public HttpStatusCode? StatusCode { get; init; }

    /// <summary>Gets the reason phrase sent with the status code.</summary>
    public string? ReasonPhrase { get; init; }

    /// <summary>Gets the HTTP request message which led to this response.</summary>
    public HttpRequestMessage? RequestMessage { get; init; }

    /// <summary>Gets the HTTP message version.</summary>
    public Version? Version { get; init; }

    /// <summary>Gets the exception object in case of an unsuccessful request or response.</summary>
    public ApiExceptionBase? Error { get; init; }

    /// <summary>Reports the request-phase error, if any, exposed by <see cref="Error"/>.</summary>
    /// <param name="error">The request exception, when present.</param>
    /// <returns><see langword="true"/> when a request error is available.</returns>
    public bool HasRequestError([NotNullWhen(true)] out ApiRequestException? error)
    {
        error = Error as ApiRequestException;
        return error is not null;
    }

    /// <summary>Reports the response-phase error, if any, exposed by <see cref="Error"/>.</summary>
    /// <param name="error">The response exception, when present.</param>
    /// <returns><see langword="true"/> when a response error is available.</returns>
    public bool HasResponseError([NotNullWhen(true)] out ApiException? error)
    {
        error = Error as ApiException;
        return error is not null;
    }

    /// <summary>No-op; the stub owns no disposable resources.</summary>
    public void Dispose()
    {
    }
}
