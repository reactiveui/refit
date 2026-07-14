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

        await CaptureRequestContentAsync(request, settings, cancellationToken).ConfigureAwait(false);

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
        "Maintainability",
        "SST1442:A function has too many direct branch points",
        Justification = "This is Refit's shared response state machine; keeping it centralized avoids duplicated generated/reflection hot paths.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
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
            await PrepareRequestAsync(request, settings, options, cancellationToken).ConfigureAwait(false);

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
            content = EnsureResponseContent(response);
            disposeResponse = options.ShouldDisposeResponse;

            var exception = typeof(T) != typeof(HttpResponseMessage)
                ? await settings.ExceptionFactory(response).ConfigureAwait(false)
                : null;

            // A non-ApiResponse error hands ownership of the response to the thrown exception, so it must outlive this scope.
            if (!options.IsApiResponse && exception is not null)
            {
                disposeResponse = false;
            }

            return await DispatchResponseAsync<T, TBody>(request, response, content, settings, options, exception, cancellationToken)
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

    /// <summary>Sends a request and streams the response body into a sequence, throwing on HTTP errors.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The request message; disposed when streaming completes.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="applyAuthorizationHeader">Whether to apply the configured authorization getter before sending.</param>
    /// <param name="cancellationToken">A token to cancel streaming.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by generated and reflection callers.")]
    public static IAsyncEnumerable<T?> StreamResponseAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool applyAuthorizationHeader,
        CancellationToken cancellationToken)
    {
        if (settings.ContentSerializer is not IStreamingContentSerializer streamingSerializer)
        {
            request.Dispose();
            throw new NotSupportedException(
                $"The configured {nameof(IHttpContentSerializer)} does not support streaming responses. Implement {nameof(IStreamingContentSerializer)} to return an IAsyncEnumerable<T>.");
        }

        return StreamResponseIteratorAsync<T>(
            client,
            request,
            settings,
            streamingSerializer,
            applyAuthorizationHeader,
            cancellationToken);
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

    /// <summary>
    /// Buffers the request body into a string before sending and stashes it on the request options so a failed
    /// request can expose it via <see cref="ApiExceptionBase.RequestContent"/> (#1189). No-op unless
    /// <see cref="RefitSettings.CaptureRequestContent"/> is enabled and the request has content.
    /// </summary>
    /// <param name="request">The request whose body should be captured.</param>
    /// <param name="settings">The Refit settings controlling capture.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>A task that completes once the body has been captured.</returns>
    internal static async Task CaptureRequestContentAsync(
        HttpRequestMessage request,
        RefitSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.CaptureRequestContent || request.Content is null)
        {
            return;
        }

#if NET6_0_OR_GREATER
        var captured = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        _ = cancellationToken;
        var captured = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        GeneratedRequestRunner.AddRequestProperty(
            request,
            HttpRequestMessageOptions.RequestContent,
            captured);
    }

    /// <summary>Returns the response content, substituting empty content when the response has none.</summary>
    /// <param name="response">The response whose content is read.</param>
    /// <returns>The response content, or empty content when none is present.</returns>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Content is nullable in the BCL contract, but HttpClient always sets it, so the null-defensive fallback is unreachable.
    internal static HttpContent EnsureResponseContent(HttpResponseMessage response) =>
        response.Content ?? new StringContent(string.Empty);

    /// <summary>Buffers the request body and applies the authorization header when the options request it.</summary>
    /// <param name="request">The request message to prepare.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="options">The send and response-processing options.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes once the request has been prepared.</returns>
    private static async Task PrepareRequestAsync(
        HttpRequestMessage request,
        RefitSettings settings,
        RequestExecutionOptions options,
        CancellationToken cancellationToken)
    {
        if (options.BufferBody && request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
        }

        await CaptureRequestContentAsync(request, settings, cancellationToken).ConfigureAwait(false);

        if (!options.ApplyAuthorizationHeader)
        {
            return;
        }

        await AddAuthorizationHeaderFromGetterAsync(request, settings, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Sends the request and streams its body, throwing on HTTP errors and disposing the request when done.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The request message; disposed when streaming completes.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="streamingSerializer">The streaming-capable content serializer.</param>
    /// <param name="applyAuthorizationHeader">Whether to apply the configured authorization getter before sending.</param>
    /// <param name="cancellationToken">A token to cancel streaming.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by generated and reflection callers.")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // async-iterator dispose-mode epilogue: the compiler-generated <>w__disposeMode false-edge cannot be exercised or removed.
    private static async IAsyncEnumerable<T?> StreamResponseIteratorAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        IStreamingContentSerializer streamingSerializer,
        bool applyAuthorizationHeader,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using (request)
        {
            if (applyAuthorizationHeader)
            {
                await AddAuthorizationHeaderFromGetterAsync(request, settings, cancellationToken)
                    .ConfigureAwait(false);
            }

            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            var exception = await settings.ExceptionFactory(response).ConfigureAwait(false);
            if (exception is not null)
            {
                throw exception;
            }

            var content = EnsureResponseContent(response);
            var format = DetectStreamingFormat(content);

#if NET6_0_OR_GREATER
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                await foreach (var item in streamingSerializer
                    .DeserializeStreamAsync<T>(stream, format, cancellationToken)
                    .ConfigureAwait(false))
                {
                    yield return item;
                }
            }
#else
            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            await foreach (var item in streamingSerializer
                .DeserializeStreamAsync<T>(stream, format, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return item;
            }
#endif
        }
    }

    /// <summary>Chooses the streaming frame format from the response content type.</summary>
    /// <param name="content">The response content whose media type is inspected.</param>
    /// <returns>The detected streaming format.</returns>
    private static StreamingContentFormat DetectStreamingFormat(HttpContent content)
    {
        var mediaType = content.Headers.ContentType?.MediaType;
        if (string.Equals(mediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            return StreamingContentFormat.ServerSentEvents;
        }

        return string.Equals(mediaType, "application/jsonl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mediaType, "application/x-ndjson", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mediaType, "application/x-jsonlines", StringComparison.OrdinalIgnoreCase)
            ? StreamingContentFormat.JsonLines
            : StreamingContentFormat.JsonArray;
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
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
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
            var transportException = settings.TransportExceptionFactory(request, ex, cancellationToken);
            if (transportException is ApiExceptionBase apiExceptionBase)
            {
                if (!isApiResponse)
                {
                    throw apiExceptionBase;
                }

                var failure = ApiResponse.Create<T, TBody>(
                    request,
                    null,
                    default,
                    settings,
                    apiExceptionBase);
                return SendResult<T>.FromFailure(failure);
            }

            throw Rethrow(transportException);
        }
    }

    /// <summary>Rethrows an exception preserving its original stack trace; never returns normally.</summary>
    /// <param name="exception">The exception to rethrow.</param>
    /// <returns>Never returns; the return type only lets callers write <c>throw Rethrow(...)</c> as a terminator.</returns>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // The trailing return is unreachable: ExceptionDispatchInfo.Throw() always throws first.
    private static Exception Rethrow(Exception exception)
    {
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        return exception;
    }

    /// <summary>Turns a received response into the caller's result: an API-response wrapper, a thrown error, or a deserialized value.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="response">The response message.</param>
    /// <param name="content">The response content.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="options">The send and response-processing options.</param>
    /// <param name="exception">The exception produced by the exception factory, if any.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The deserialized or wrapped response.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "TBody is intentionally passed explicitly by generated and reflection callers for ApiResponse<T> body deserialization.")]
    private static async Task<T?> DispatchResponseAsync<T, TBody>(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpContent content,
        RefitSettings settings,
        RequestExecutionOptions options,
        Exception? exception,
        CancellationToken cancellationToken)
    {
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
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "TBody is intentionally passed explicitly by callers for ApiResponse<T> body deserialization.")]
    private static async ValueTask<T?> BuildApiResponseAsync<T, TBody>(
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
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Callers intentionally close the result type; type inference is not part of this helper contract.")]
    private static async ValueTask<T?> DeserializeOrThrowAsync<T>(
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
    private static async ValueTask<Exception?> CreateDeserializationExceptionAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        RefitSettings settings,
        Exception exception) =>
        settings.DeserializationExceptionFactory is not null
            ? await settings.DeserializationExceptionFactory(response, exception)
                .ConfigureAwait(false)
            : await ApiException.Create(
                DeserializationErrorMessage,
                request,
                request.Method,
                response,
                settings,
                exception).ConfigureAwait(false);

    /// <summary>Deserializes the response content into the requested type.</summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="response">The response message.</param>
    /// <param name="content">The response content.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The deserialized value, or default when there is no content.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Callers intentionally close the result type; type inference is not part of this helper contract.")]
    private static async ValueTask<T?> DeserializeContentAsync<T>(
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
            string text;
#if NET8_0_OR_GREATER
            await using (stream.ConfigureAwait(false))
#else
            using (stream)
#endif
            {
                using var reader = new StreamReader(stream);
#if NET8_0_OR_GREATER
                text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
                cancellationToken.ThrowIfCancellationRequested();
                text = await reader.ReadToEndAsync().ConfigureAwait(false);
#endif
            }

            return (T)(object)text;
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
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Callers intentionally close the result type; type inference is not part of this helper contract.")]
    private static async ValueTask<T?> DeserializeSerializedContentAsync<T>(
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
