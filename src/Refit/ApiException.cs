// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace Refit;

/// <summary>Represents an error that occurred after a response was received from the server.</summary>
[SuppressMessage(
    "Design",
    "SST1488:Exception types should declare the standard constructors",
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
    [SuppressMessage("Design", "SST1472:Signatures should not declare too many parameters", Justification = "Shipped protected API constructor; grouping parameters would break the public surface.")]
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
    [SuppressMessage("Design", "SST1472:Signatures should not declare too many parameters", Justification = "Shipped protected API constructor; grouping parameters would break the public surface.")]
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
    [SuppressMessage("Design", "SST1472:Signatures should not declare too many parameters", Justification = "Shipped protected API constructor; grouping parameters would break the public surface.")]
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
    /// <remarks>
    /// These are the raw, unredacted response headers and may contain sensitive values such as <c>Set-Cookie</c>.
    /// Anything that serializes this exception (structured logging, telemetry) will capture them; scrub with
    /// <see cref="RefitSettings.ExceptionRedactor"/> if that is a concern.
    /// </remarks>
    public HttpResponseHeaders Headers { get; }

    /// <summary>Gets the HTTP response content headers as defined in RFC 2616.</summary>
    public HttpContentHeaders? ContentHeaders { get; protected set; }

    /// <summary>Gets or sets the HTTP response content as a string.</summary>
    /// <remarks>
    /// This is the raw, unredacted response body. The setter exists so that
    /// <see cref="RefitSettings.ExceptionRedactor"/> can scrub it before the exception propagates.
    /// </remarks>
    public string? Content { get; set; }

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
        ArgumentExceptionHelper.ThrowIfNull(response);

        if (response.IsSuccessStatusCode)
        {
            throw new ArgumentException("Response is successful, cannot create an ApiException.", nameof(response));
        }

        var exceptionMessage = CreateMessage(response.StatusCode, response.ReasonPhrase);
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
    public static async Task<ApiException> Create(
        string exceptionMessage,
        HttpRequestMessage message,
        HttpMethod httpMethod,
        HttpResponseMessage response,
        RefitSettings refitSettings,
        Exception? innerException)
    {
        ArgumentExceptionHelper.ThrowIfNull(response);

        var exception = new ApiException(
            exceptionMessage,
            message,
            httpMethod,
            null,
            response.StatusCode,
            response.ReasonPhrase,
            response.Headers,
            refitSettings,
            innerException)
        {
            RequestContent = GetCapturedRequestContent(message)
        };

        if (response.Content is not null)
        {
            try
            {
                exception.ContentHeaders = response.Content.Headers;
                exception.Content = await ReadContentCappedAsync(
                        response.Content,
                        refitSettings.MaxExceptionContentLength)
                    .ConfigureAwait(false);

                // Content.Headers is never null on a real HttpContent, so only the ContentType/MediaType
                // null cases are meaningful here and both are exercised by the tests.
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType?.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    exception = await ValidationApiException
                        .CreateAsync(exception)
                        .ConfigureAwait(false);
                }

                response.Content.Dispose();
            }
            catch (Exception readException)
            {
                _ = readException;

                // NB: We're already handling an exception at this point,
                // so we want to make sure we don't throw another one
                // that hides the real error.
            }
        }

        refitSettings.ExceptionRedactor?.Invoke(exception);
        return exception;
    }

    /// <summary>Get the deserialized response content as nullable <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Type to deserialize the content to.</typeparam>
    /// <returns>The response content deserialized as <typeparamref name="T"/></returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public async Task<T?> GetContentAsAsync<T>() =>
        HasContent
            ? await RefitSettings
                .ContentSerializer.FromHttpContentAsync<T>(new StringContent(Content!))
                .ConfigureAwait(false)
            : default;

    /// <summary>
    /// Synchronously deserializes the buffered response content as <typeparamref name="T"/>. The content is already
    /// a string, so this can run inside an exception filter where awaiting is not allowed by the CLR (#1591).
    /// </summary>
    /// <typeparam name="T">Type to deserialize the content to.</typeparam>
    /// <returns>The deserialized content, or <see langword="default"/> when there is no content.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the configured <see cref="IHttpContentSerializer"/> does not implement
    /// <see cref="ISynchronousContentDeserializer"/>.
    /// </exception>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public T? GetContentAs<T>()
    {
        if (!HasContent)
        {
            return default;
        }

        if (RefitSettings.ContentSerializer is not ISynchronousContentDeserializer synchronousDeserializer)
        {
            throw new NotSupportedException(
                $"The configured content serializer '{RefitSettings.ContentSerializer.GetType()}' does not "
                + $"implement {nameof(ISynchronousContentDeserializer)}; use {nameof(GetContentAsAsync)} instead.");
        }

        return synchronousDeserializer.DeserializeFromString<T>(Content!);
    }

    /// <summary>
    /// Attempts to synchronously deserialize the buffered response content as <typeparamref name="T"/> without
    /// throwing, making it usable from an exception filter:
    /// <c>catch (ApiException ex) when (ex.TryGetContentAs&lt;Error&gt;(out var error))</c> (#1591).
    /// </summary>
    /// <typeparam name="T">Type to deserialize the content to.</typeparam>
    /// <param name="content">The deserialized content when this returns <see langword="true"/>; otherwise <see langword="default"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the content was present and deserialized to a non-null value; otherwise <see langword="false"/>.
    /// </returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public bool TryGetContentAs<T>(out T? content)
    {
        content = default;
        if (!HasContent || RefitSettings.ContentSerializer is not ISynchronousContentDeserializer synchronousDeserializer)
        {
            return false;
        }

        try
        {
            content = synchronousDeserializer.DeserializeFromString<T>(Content!);
        }
        catch (Exception)
        {
            content = default;
            return false;
        }

        return content is not null;
    }

    /// <summary>Reads the response body as a string, bounding the number of characters when a limit is configured.</summary>
    /// <param name="content">The response content to read.</param>
    /// <param name="maxChars">The maximum number of characters to read, or <see langword="null"/> for unbounded.</param>
    /// <returns>The (possibly truncated) response body.</returns>
    private static async Task<string> ReadContentCappedAsync(HttpContent content, int? maxChars)
    {
        if (maxChars is not { } limit)
        {
            return await content.ReadAsStringAsync().ConfigureAwait(false);
        }

        if (limit <= 0)
        {
            return string.Empty;
        }

        var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#if NET6_0_OR_GREATER
        await using var streamScope = stream.ConfigureAwait(false);
#else
        using var streamScope = stream;
#endif
        using var reader = new StreamReader(stream);
        var buffer = new char[limit];
        var total = 0;
        while (total < limit)
        {
            var read = await reader
                .ReadBlockAsync(buffer, total, limit - total)
                .ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return new string(buffer, 0, total);
    }

    /// <summary>Builds the exception message from the status code and reason phrase.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="reasonPhrase">The reason phrase.</param>
    /// <returns>The formatted exception message.</returns>
    private static string CreateMessage(HttpStatusCode statusCode, string? reasonPhrase) =>
        $"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).";

    /// <summary>Reads the request body string captured before sending, if request-content capture was enabled.</summary>
    /// <param name="request">The request message that carries the captured content option.</param>
    /// <returns>The captured request content, or <see langword="null"/> when none was captured.</returns>
    private static string? GetCapturedRequestContent(HttpRequestMessage request)
    {
#if NET6_0_OR_GREATER
        return request.Options.TryGetValue(
            new HttpRequestOptionsKey<string>(HttpRequestMessageOptions.RequestContent),
            out var captured)
            ? captured
            : null;
#else
        return request.Properties.TryGetValue(HttpRequestMessageOptions.RequestContent, out var captured)
            ? captured as string
            : null;
#endif
    }
}
