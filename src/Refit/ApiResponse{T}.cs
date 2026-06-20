// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace Refit;

/// <summary>Implementation of <see cref="IApiResponse{T}"/> that provides additional functionalities.</summary>
/// <typeparam name="T">The deserialized response content type.</typeparam>
/// <remarks>
/// Create an instance of <see cref="ApiResponse{T}"/> with type <typeparamref name="T"/>.
/// </remarks>
/// <param name="request">Original HTTP Request.</param>
/// <param name="response">Original HTTP Response message.</param>
/// <param name="content">Response content.</param>
/// <param name="settings">Refit settings used to send the request.</param>
/// <param name="error">The exception, if the request failed.</param>
public sealed class ApiResponse<T>(
    HttpRequestMessage request,
    HttpResponseMessage? response,
    T? content,
    RefitSettings settings,
    ApiExceptionBase? error = null) : IApiResponse<T>
{
    /// <summary>Tracks whether this instance has already been disposed.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ApiResponse{T}"/> class.</summary>
    /// <param name="response">Original HTTP Response message.</param>
    /// <param name="content">Response content.</param>
    /// <param name="settings">Refit settings used to send the request.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public ApiResponse(
        HttpResponseMessage response,
        T? content,
        RefitSettings settings)
        : this(response, content, settings, null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApiResponse{T}"/> class.</summary>
    /// <param name="response">Original HTTP Response message.</param>
    /// <param name="content">Response content.</param>
    /// <param name="settings">Refit settings used to send the request.</param>
    /// <param name="error">The exception, if the request failed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public ApiResponse(
        HttpResponseMessage response,
        T? content,
        RefitSettings settings,
        ApiExceptionBase? error)
        : this(
            (response ?? throw new ArgumentNullException(nameof(response))).RequestMessage
            ?? throw new ArgumentException(
                "Response must have an associated request message.",
                nameof(response)),
            response,
            content,
            settings,
            error)
    {
    }

    /// <summary>Gets the deserialized request content as <typeparamref name="T"/>.</summary>
    public T? Content { get; } = content;

    /// <summary>Gets the Refit settings used to send the request.</summary>
    public RefitSettings Settings { get; } = settings;

    /// <summary>Gets the HTTP response headers.</summary>
    public HttpResponseHeaders? Headers => response?.Headers;

    /// <summary>Gets the HTTP response content headers as defined in RFC 2616.</summary>
    public HttpContentHeaders? ContentHeaders => response?.Content?.Headers;

    /// <summary>Gets a value indicating whether the request was successful.</summary>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccessStatusCode => response?.IsSuccessStatusCode ?? false;

    /// <summary>
    /// Gets a value indicating whether the request was successful and there wasn't any other error (for example, during content deserialization).
    /// </summary>
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(Content))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccessful => IsSuccessStatusCode && Error is null;

    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(Headers))]
    [MemberNotNullWhen(true, nameof(Content))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(true, nameof(StatusCode))]
    [MemberNotNullWhen(true, nameof(Version))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsReceived => response is not null;

    /// <summary>Gets the reason phrase which typically is sent by the server together with the status code.</summary>
    public string? ReasonPhrase => response?.ReasonPhrase;

    /// <summary>Gets the HTTP Request message which led to this response.</summary>
    public HttpRequestMessage RequestMessage => request;

    /// <summary>Gets the HTTP response status code.</summary>
    public HttpStatusCode? StatusCode => response?.StatusCode;

    /// <summary>Gets the HTTP Message version.</summary>
    public Version? Version => response?.Version;

    /// <summary>Gets the <see cref="ApiExceptionBase" /> object in case of unsuccessful request or response.</summary>
    /// <remarks>
    /// The <see cref="HasRequestError"/> and <see cref="HasResponseError"/> methods can be
    /// used to check the type of error.
    /// </remarks>
    public ApiExceptionBase? Error { get; } = error;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose() => Dispose(true);

    /// <summary>Ensures the request was successful by throwing an exception in case of failure.</summary>
    /// <returns>The current <see cref="ApiResponse{T}"/></returns>
    /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
    /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
    public Task<ApiResponse<T>> EnsureSuccessStatusCodeAsync()
    {
        return IsSuccessStatusCode
            ? Task.FromResult(this)
            : EnsureSlowAsync();
    }

    /// <summary>Ensures the request was successful and without any other error by throwing an exception in case of failure.</summary>
    /// <returns>The current <see cref="ApiResponse{T}"/></returns>
    /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
    /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
    public Task<ApiResponse<T>> EnsureSuccessfulAsync()
    {
        return IsSuccessful
            ? Task.FromResult(this)
            : EnsureSlowAsync();
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "By Design")]
    public bool HasRequestError(
        [MaybeNullWhen(false)] out ApiRequestException? error)
    {
        error = Error as ApiRequestException;
        return error is not null;
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "By Design")]
    public bool HasResponseError(
        [MaybeNullWhen(false)]
        out ApiException? error)
    {
        error = Error as ApiException;
        return error is not null;
    }

    /// <summary>Releases the underlying response when disposing.</summary>
    /// <param name="disposing">Whether the method is being called from <see cref="Dispose()"/>.</param>
    private void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        _disposed = true;

        response?.Dispose();
    }

    /// <summary>Throws the appropriate API exception for an unsuccessful response.</summary>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    private Task<ApiResponse<T>> EnsureSlowAsync() => ThrowsApiExceptionAsync();

    /// <summary>Throws the appropriate API exception for an unsuccessful response.</summary>
    /// <returns>A task that represents the asynchronous throw operation.</returns>
    private async Task<ApiResponse<T>> ThrowsApiExceptionAsync()
    {
        var responseMessage = response
                              ?? throw new InvalidOperationException(
                                  "The response is unavailable for this API response.");

        var exception =
            Error
            ?? await ApiException
                .Create(
                    request,
                    request.Method,
                    responseMessage,
                    Settings)
                .ConfigureAwait(false);

        Dispose();

        throw exception;
    }
}
