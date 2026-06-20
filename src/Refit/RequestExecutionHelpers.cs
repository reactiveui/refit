// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Shared send and response-processing helpers for generated and reflection-built requests.</summary>
internal static class RequestExecutionHelpers
{
    /// <summary>The message used when content cannot be deserialized into the requested type.</summary>
    private const string DeserializationErrorMessage = "An error occured deserializing the response.";

    /// <summary>The error message used when the HTTP client has no base address configured.</summary>
    private const string BaseAddressRequiredMessage = "BaseAddress must be set on the HttpClient instance";

    /// <summary>Throws when a client cannot build relative generated requests.</summary>
    /// <param name="client">The HTTP client to inspect.</param>
    /// <exception cref="InvalidOperationException">Thrown when no base address is configured.</exception>
    public static void ThrowIfBaseAddressMissing(HttpClient client)
    {
        if (client.BaseAddress is not null)
        {
            return;
        }

        throw new InvalidOperationException(BaseAddressRequiredMessage);
    }

    /// <summary>Sends a request with no response body, throwing on HTTP errors.</summary>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="applyAuthorizationHeader">Whether to apply the configured authorization getter before sending.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    public static async Task SendVoidAsync(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool bufferBody,
        bool applyAuthorizationHeader,
        CancellationToken cancellationToken)
    {
        if (bufferBody && request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
        }

        if (applyAuthorizationHeader)
        {
            await AddAuthorizationHeaderFromGetterAsync(request, settings, cancellationToken)
                .ConfigureAwait(false);
        }

        using var response = await client
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var exception = await settings.ExceptionFactory(response).ConfigureAwait(false);
        if (exception is null)
        {
            return;
        }

        throw exception;
    }

    /// <summary>Buffers, sends, and processes the response for a request.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="options">The send and response-processing options.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The deserialized or wrapped response.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "CodeQuality",
        "S1541:Methods and properties should not be too complex",
        Justification = "This is Refit's shared response state machine; keeping it centralized avoids duplicated generated/reflection hot paths.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "TBody is intentionally passed explicitly by generated and reflection callers for ApiResponse<T> body deserialization.")]
    public static async Task<T?> SendAndProcessResponseAsync<T, TBody>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        RequestExecutionOptions options,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        HttpContent? content = null;
        var disposeResponse = true;
        try
        {
            if (options.BufferBody && request.Content is not null)
            {
                await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
            }

            if (options.ApplyAuthorizationHeader)
            {
                await AddAuthorizationHeaderFromGetterAsync(request, settings, cancellationToken)
                    .ConfigureAwait(false);
            }

            var sendResult = await SendOrCaptureExceptionAsync<T, TBody>(
                    client,
                    request,
                    settings,
                    options.IsApiResponse,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!sendResult.HasResponse)
            {
                return sendResult.FailureResult;
            }

            response = sendResult.Response!;
            content = response.Content ?? new StringContent(string.Empty);
            disposeResponse = options.ShouldDisposeResponse;

            var exception = typeof(T) != typeof(HttpResponseMessage)
                ? await settings.ExceptionFactory(response).ConfigureAwait(false)
                : null;

            if (options.IsApiResponse)
            {
                return await BuildApiResponseAsync<T, TBody>(
                        request,
                        response,
                        content,
                        settings,
                        exception,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (exception is not null)
            {
                disposeResponse = false;
                throw exception;
            }

            return await DeserializeOrThrowAsync<T>(
                    request,
                    response,
                    content,
                    settings,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            if (disposeResponse)
            {
                response?.Dispose();
                content?.Dispose();
            }
        }
    }

    /// <summary>Populates an empty Authorization header through the configured token getter.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="cancellationToken">A token to cancel the token getter.</param>
    /// <returns>A task that completes when the header has been updated.</returns>
    public static async Task AddAuthorizationHeaderFromGetterAsync(
        HttpRequestMessage request,
        RefitSettings settings,
        CancellationToken cancellationToken)
    {
        if (settings.AuthorizationHeaderValueGetter is null)
        {
            return;
        }

        var auth = request.Headers.Authorization;
        if (auth is null || !string.IsNullOrWhiteSpace(auth.Parameter))
        {
            return;
        }

        var token = await settings.AuthorizationHeaderValueGetter(request, cancellationToken)
            .ConfigureAwait(false);
        request.Headers.Authorization = new(auth.Scheme, token);
    }

    /// <summary>Sends the request, capturing a transport failure as an API response when required.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="isApiResponse">Whether the result type is an API response wrapper.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The send result.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "TBody is intentionally passed explicitly by callers for ApiResponse<T> failure wrapping.")]
    private static async Task<SendResult<T>> SendOrCaptureExceptionAsync<T, TBody>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool isApiResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            return SendResult<T>.FromResponse(response);
        }
        catch (Exception ex)
        {
            if (!isApiResponse)
            {
                throw new ApiRequestException(request, request.Method, settings, ex);
            }

            var failure = ApiResponse.Create<T, TBody>(
                request,
                null,
                default,
                settings,
                new ApiRequestException(request, request.Method, settings, ex));
            return SendResult<T>.FromFailure(failure);
        }
    }

    /// <summary>Builds an API response, deserializing content unless an earlier error exists.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The deserialized body type.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="response">The response message.</param>
    /// <param name="content">The response content.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="existingException">An exception already produced by the exception factory, if any.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The constructed API response.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "TBody is intentionally passed explicitly by callers for ApiResponse<T> body deserialization.")]
    private static async Task<T?> BuildApiResponseAsync<T, TBody>(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpContent content,
        RefitSettings settings,
        Exception? existingException,
        CancellationToken cancellationToken)
    {
        var exception = existingException;
        var body = default(TBody);

        try
        {
            body =
                exception is null
                    ? await DeserializeContentAsync<TBody>(response, content, settings, cancellationToken)
                        .ConfigureAwait(false)
                    : default;
        }
        catch (Exception ex)
        {
            exception = await CreateDeserializationExceptionAsync(request, response, settings, ex)
                .ConfigureAwait(false);
        }

        return ApiResponse.Create<T, TBody>(
            request,
            response,
            body,
            settings,
            exception as ApiException);
    }

    /// <summary>Deserializes the response content, throwing a wrapped exception on failure.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="response">The response message.</param>
    /// <param name="content">The response content.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The deserialized result.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Callers intentionally close the result type; type inference is not part of this helper contract.")]
    private static async Task<T?> DeserializeOrThrowAsync<T>(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpContent content,
        RefitSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            return await DeserializeContentAsync<T>(response, content, settings, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (settings.DeserializationExceptionFactory is not null)
            {
                var customEx = await settings
                    .DeserializationExceptionFactory(response, ex)
                    .ConfigureAwait(false);
                if (customEx is not null)
                {
                    throw customEx;
                }

                return default;
            }

            throw await ApiException.Create(
                DeserializationErrorMessage,
                request,
                request.Method,
                response,
                settings,
                ex).ConfigureAwait(false);
        }
    }

    /// <summary>Produces a wrapped deserialization exception using the configured factory or default behavior.</summary>
    /// <param name="request">The request message.</param>
    /// <param name="response">The response message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="exception">The original exception.</param>
    /// <returns>The wrapped exception, or null when a configured factory returns null.</returns>
    private static async Task<Exception?> CreateDeserializationExceptionAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        RefitSettings settings,
        Exception exception)
    {
        if (settings.DeserializationExceptionFactory is not null)
        {
            return await settings.DeserializationExceptionFactory(response, exception)
                .ConfigureAwait(false);
        }

        return await ApiException.Create(
            DeserializationErrorMessage,
            request,
            request.Method,
            response,
            settings,
            exception).ConfigureAwait(false);
    }

    /// <summary>Deserializes the response content into the requested type.</summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="response">The response message.</param>
    /// <param name="content">The response content.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The deserialized value, or default when there is no content.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Callers intentionally close the result type; type inference is not part of this helper contract.")]
    private static async Task<T?> DeserializeContentAsync<T>(
        HttpResponseMessage response,
        HttpContent content,
        RefitSettings settings,
        CancellationToken cancellationToken)
    {
        if (typeof(T) == typeof(HttpResponseMessage))
        {
            return (T)(object)response;
        }

        if (typeof(T) == typeof(HttpContent))
        {
            return (T)(object)content;
        }

        if (typeof(T) == typeof(Stream))
        {
            var stream = (object)
                await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return (T)stream;
        }

        if (typeof(T) == typeof(string))
        {
            var stream = await content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using (stream)
            {
                using var reader = new StreamReader(stream);
#if NET8_0_OR_GREATER
                var str = (object)await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
                cancellationToken.ThrowIfCancellationRequested();
                var str = (object)await reader.ReadToEndAsync().ConfigureAwait(false);
#endif
                return (T)str;
            }
        }

        return await DeserializeSerializedContentAsync<T>(
                response,
                content,
                settings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Buffers and deserializes serialized content via the configured serializer.</summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="response">The response message.</param>
    /// <param name="content">The response content.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The deserialized value, or default when there is no content.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Callers intentionally close the result type; type inference is not part of this helper contract.")]
    private static async Task<T?> DeserializeSerializedContentAsync<T>(
        HttpResponseMessage response,
        HttpContent content,
        RefitSettings settings,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent
            || content.Headers.ContentLength == 0)
        {
            return default;
        }

        await TryBufferContentAsync(content, cancellationToken).ConfigureAwait(false);

        return await settings.ContentSerializer
            .FromHttpContentAsync<T>(content, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Attempts to buffer content into memory, ignoring buffering failures.</summary>
    /// <param name="content">The content to buffer.</param>
    /// <param name="cancellationToken">A token to cancel buffering.</param>
    /// <returns>A task that completes once buffering has been attempted.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Best-effort buffering matches the existing runtime response path.")]
    private static async Task TryBufferContentAsync(HttpContent content, CancellationToken cancellationToken)
    {
        try
        {
            await content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception bufferingException)
        {
            _ = bufferingException;
        }
    }

    /// <summary>The outcome of attempting to send a request.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    private readonly record struct SendResult<T>
    {
        /// <summary>Initializes a new instance of the <see cref="SendResult{T}"/> struct.</summary>
        /// <param name="hasResponse">Whether the send produced a response.</param>
        /// <param name="response">The response, or null when the send failed.</param>
        /// <param name="failureResult">The captured failure result, valid only when <paramref name="hasResponse"/> is false.</param>
        private SendResult(bool hasResponse, HttpResponseMessage? response, T? failureResult)
        {
            HasResponse = hasResponse;
            Response = response;
            FailureResult = failureResult;
        }

        /// <summary>Gets a value indicating whether the send produced a response.</summary>
        public bool HasResponse { get; }

        /// <summary>Gets the response, or null when the send failed.</summary>
        public HttpResponseMessage? Response { get; }

        /// <summary>Gets the captured failure result, valid only when <see cref="HasResponse"/> is false.</summary>
        public T? FailureResult { get; }

        /// <summary>Creates a successful result wrapping the given response.</summary>
        /// <param name="response">The response produced by the send.</param>
        /// <returns>A result indicating a response is present.</returns>
        public static SendResult<T> FromResponse(HttpResponseMessage response) =>
            new(true, response, default);

        /// <summary>Creates a failed result wrapping the captured failure value.</summary>
        /// <param name="failureResult">The captured failure result.</param>
        /// <returns>A result indicating the send failed.</returns>
        public static SendResult<T> FromFailure(T? failureResult) =>
            new(false, null, failureResult);
    }
}
