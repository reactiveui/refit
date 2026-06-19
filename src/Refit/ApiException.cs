// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace Refit;

/// <summary>Represents an error that occurred after a response was received from the server.</summary>
[SuppressMessage(
    "Usage",
    "CA1032:Implement standard exception constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
[SuppressMessage(
    "Major Code Smell",
    "S4027:Exceptions should provide standard constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
public class ApiException : ApiExceptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ApiException"/> class.</summary>
    /// <param name="message">The message.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="content">The content.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="refitSettings">The refit settings.</param>
    protected ApiException(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        string? content,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        HttpResponseHeaders headers,
        RefitSettings refitSettings)
        : this(
            message,
            httpMethod,
            content,
            statusCode,
            reasonPhrase,
            headers,
            refitSettings,
            null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApiException"/> class.</summary>
    /// <param name="message">The message.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="content">The content.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="refitSettings">The refit settings.</param>
    /// <param name="innerException">The inner exception.</param>
    protected ApiException(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        string? content,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        HttpResponseHeaders headers,
        RefitSettings refitSettings,
        Exception? innerException)
        : this(
            CreateMessage(statusCode, reasonPhrase),
            message,
            httpMethod,
            content,
            statusCode,
            reasonPhrase,
            headers,
            refitSettings,
            innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApiException"/> class.</summary>
    /// <param name="exceptionMessage">The exception message.</param>
    /// <param name="message">The message.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="content">The content.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="refitSettings">The refit settings.</param>
    protected ApiException(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        string? content,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        HttpResponseHeaders headers,
        RefitSettings refitSettings)
        : this(
            exceptionMessage,
            message,
            httpMethod,
            content,
            statusCode,
            reasonPhrase,
            headers,
            refitSettings,
            null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApiException"/> class.</summary>
    /// <param name="exceptionMessage">The exception message.</param>
    /// <param name="message">The message.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="content">The content.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="refitSettings">The refit settings.</param>
    /// <param name="innerException">The inner exception.</param>
    protected ApiException(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        string? content,
        HttpStatusCode statusCode,
        string? reasonPhrase,
        HttpResponseHeaders headers,
        RefitSettings refitSettings,
        Exception? innerException)
        : base(exceptionMessage, message, httpMethod, refitSettings, innerException)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Headers = headers;
        Content = content;
    }

    /// <summary>Gets the HTTP response status code.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Gets the reason phrase which typically is sent by the server together with the status code.</summary>
    public string? ReasonPhrase { get; }

    /// <summary>Gets the HTTP response headers.</summary>
    public HttpResponseHeaders Headers { get; }

    /// <summary>Gets the HTTP response content headers as defined in RFC 2616.</summary>
    public HttpContentHeaders? ContentHeaders { get; protected set; }

    /// <summary>Gets the HTTP Response content as string.</summary>
    public string? Content { get; private set; }

    /// <summary>Gets a value indicating whether the response has content.</summary>
    [MemberNotNullWhen(true, nameof(Content))]
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>Create an instance of <see cref="ApiException"/>.</summary>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="response">The HTTP Response message.</param>
    /// <param name="refitSettings">Refit settings used to sent the request.</param>
    /// <returns>A newly created <see cref="ApiException"/>.</returns>
    [SuppressMessage(
        "Usage",
        "VSTHRD200:Use \"Async\" suffix for async methods",
        Justification = "Public API name preserved for backwards compatibility.")]
    public static Task<ApiException> Create(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        HttpResponseMessage response,
        RefitSettings refitSettings) =>
        Create(message, httpMethod, response, refitSettings, null);

    /// <summary>Create an instance of <see cref="ApiException"/>.</summary>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="response">The HTTP Response message.</param>
    /// <param name="refitSettings">Refit settings used to sent the request.</param>
    /// <param name="innerException">Add an inner exception to the <see cref="ApiException"/>.</param>
    /// <returns>A newly created <see cref="ApiException"/>.</returns>
    [SuppressMessage("Usage", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Public API name preserved for backwards compatibility.")]
    public static Task<ApiException> Create(
        HttpRequestMessage message,
        HttpMethod httpMethod,
        HttpResponseMessage response,
        RefitSettings refitSettings,
        Exception? innerException)
    {
        if (response?.IsSuccessStatusCode == true)
        {
            throw new ArgumentException("Response is successful, cannot create an ApiException.", nameof(response));
        }

        var exceptionMessage = CreateMessage(response!.StatusCode, response.ReasonPhrase);
        return Create(
            exceptionMessage,
            message,
            httpMethod,
            response,
            refitSettings,
            innerException);
    }

    /// <summary>Create an instance of <see cref="ApiException"/> with a custom exception message.</summary>
    /// <param name="exceptionMessage">A custom exception message.</param>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="response">The HTTP Response message.</param>
    /// <param name="refitSettings">Refit settings used to send the request.</param>
    /// <returns>A newly created <see cref="ApiException"/>.</returns>
    [SuppressMessage("Usage", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Public API name preserved for backwards compatibility.")]
    public static Task<ApiException> Create(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        HttpResponseMessage response,
        RefitSettings refitSettings) =>
        Create(exceptionMessage, message, httpMethod, response, refitSettings, null);

    /// <summary>Create an instance of <see cref="ApiException"/> with a custom exception message.</summary>
    /// <param name="exceptionMessage">A custom exception message.</param>
    /// <param name="message">The HTTP Request message used to send the request.</param>
    /// <param name="httpMethod">The HTTP method used to send the request.</param>
    /// <param name="response">The HTTP Response message.</param>
    /// <param name="refitSettings">Refit settings used to send the request.</param>
    /// <param name="innerException">Add an inner exception to the <see cref="ApiException"/>.</param>
    /// <returns>A newly created <see cref="ApiException"/>.</returns>
    [SuppressMessage(
        "Usage",
        "VSTHRD200:Use \"Async\" suffix for async methods",
        Justification = "Public API name preserved for backwards compatibility.")]
    [SuppressMessage(
        "Major Code Smell",
        "CA1031:Do not catch general exception types",
        Justification = "Best-effort content read while already handling an error; any failure must not hide the original error.")]
    [SuppressMessage(
        "Minor Code Smell",
        "SST1429:Do not use an empty catch of the base exception",
        Justification = "Best-effort content read while already handling an error; any failure must not hide the original error.")]
    public static async Task<ApiException> Create(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        HttpResponseMessage response,
        RefitSettings refitSettings,
        Exception? innerException)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var exception = new ApiException(
            exceptionMessage,
            message,
            httpMethod,
            null,
            response.StatusCode,
            response.ReasonPhrase,
            response.Headers,
            refitSettings,
            innerException);

        if (response.Content is null)
        {
            return exception;
        }

        try
        {
            exception.ContentHeaders = response.Content.Headers;
            exception.Content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (
                response
                    .Content.Headers?.ContentType
                    ?.MediaType
                    ?.Equals("application/problem+json", StringComparison.Ordinal) ?? false
            )
            {
                exception = await ValidationApiException
                    .CreateAsync(exception)
                    .ConfigureAwait(false);
            }

            response.Content.Dispose();
        }
        catch
        {
            // NB: We're already handling an exception at this point,
            // so we want to make sure we don't throw another one
            // that hides the real error.
        }

        return exception;
    }

    /// <summary>Get the deserialized response content as nullable <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Type to deserialize the content to.</typeparam>
    /// <returns>The response content deserialized as <typeparamref name="T"/></returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public async Task<T?> GetContentAsAsync<T>() =>
        HasContent
            ? await RefitSettings
                .ContentSerializer.FromHttpContentAsync<T>(new StringContent(Content!))
                .ConfigureAwait(false)
            : default;

    /// <summary>Builds the exception message from the status code and reason phrase.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <returns>The formatted exception message.</returns>
    private static string CreateMessage(HttpStatusCode statusCode, string? reasonPhrase) =>
        $"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).";
}
