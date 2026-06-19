// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Refit
{
    /// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
    internal partial class RequestBuilderImplementation
    {
        /// <summary>Determines whether the request body should be buffered before sending.</summary>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="request">The request message, if built.</param>
        /// <returns><see langword="true"/> if the body should be buffered; otherwise <see langword="false"/>.</returns>
        private static bool IsBodyBuffered(
            RestMethodInfoInternal restMethod,
            HttpRequestMessage? request) =>
            (restMethod.BodyParameterInfo?.Item2 ?? false) && (request?.Content is not null);

        /// <summary>Attempts to buffer content into memory, ignoring buffering failures.</summary>
        /// <param name="content">The content to buffer.</param>
        /// <param name="cancellationToken">A token to cancel the buffering.</param>
        /// <returns>A task that completes once buffering has been attempted.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Best-effort buffering: any failure falls back to streaming deserialization (pre-existing behavior).")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Minor Code Smell",
            "SST1429:Do not use an empty catch of the base exception",
            Justification = "Best-effort buffering: any failure falls back to streaming deserialization (pre-existing behavior).")]
        private static async Task TryBufferContentAsync(HttpContent content, CancellationToken cancellationToken)
        {
            try
            {
#if NET8_0_OR_GREATER
                await content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
#else
                _ = cancellationToken;
                await content.LoadIntoBufferAsync().ConfigureAwait(false);
#endif
            }
            catch
            {
                // Best-effort: if the content cannot be buffered we fall back to
                // streaming deserialization. The only downside is that the raw body
                // may be unavailable on failure, which is the pre-existing behavior.
            }
        }

        /// <summary>Builds and sends the request for a method with no response body, throwing on error.</summary>
        /// <param name="client">The HTTP client to send with.</param>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="paramList">The argument values for the call.</param>
        /// <param name="paramsContainsCancellationToken">Whether the argument list contains a cancellation token.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A task that completes when the request finishes.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private async Task ExecuteVoidRequestAsync(
            HttpClient client,
            RestMethodInfoInternal restMethod,
            object[] paramList,
            bool paramsContainsCancellationToken,
            CancellationToken cancellationToken)
        {
            if (client.BaseAddress is null)
            {
                throw new InvalidOperationException(BaseAddressRequiredMessage);
            }

            using var rq = await BuildRequestMessageForMethodAsync(
                    restMethod,
                    client.BaseAddress.AbsolutePath,
                    paramsContainsCancellationToken,
                    paramList)
                .ConfigureAwait(false);

            if (IsBodyBuffered(restMethod, rq))
            {
                await rq!.Content!.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
            }

            using var resp = await client
                .SendAsync(rq!, cancellationToken)
                .ConfigureAwait(false);

            var exception = await _settings.ExceptionFactory(resp).ConfigureAwait(false);
            if (exception is null)
            {
                return;
            }

            throw exception;
        }

        /// <summary>Builds, sends and deserializes the request for a method that returns a value.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="client">The HTTP client to send with.</param>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="paramList">The argument values for the call.</param>
        /// <param name="paramsContainsCancellationToken">Whether the argument list contains a cancellation token.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized result, or default when there is no content.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<T?> ExecuteRequestAsync<T, TBody>(
            HttpClient client,
            RestMethodInfoInternal restMethod,
            object[] paramList,
            bool paramsContainsCancellationToken,
            CancellationToken cancellationToken)
        {
            if (client.BaseAddress is null)
            {
                throw new InvalidOperationException(BaseAddressRequiredMessage);
            }

            using var rq = await BuildRequestMessageForMethodAsync(
                    restMethod,
                    client.BaseAddress.AbsolutePath,
                    paramsContainsCancellationToken,
                    paramList)
                .ConfigureAwait(false);

            return await SendAndProcessResponseAsync<T, TBody>(client, restMethod, rq!, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>Builds a cancellable task delegate that sends the request and deserializes the response.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that sends the request with a cancellation token.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private Func<HttpClient, CancellationToken, object[], Task<T?>> BuildCancellableTaskFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod)
        {
            return async (client, ct, paramList) =>
            {
                if (client.BaseAddress is null)
                {
                    throw new InvalidOperationException(BaseAddressRequiredMessage);
                }

                var rq = await BuildRequestMessageForMethodAsync(
                    restMethod,
                    client.BaseAddress.AbsolutePath,
                    restMethod.CancellationToken is not null,
                    paramList).ConfigureAwait(false);

                try
                {
                    return await SendAndProcessResponseAsync<T, TBody>(client, restMethod, rq!, ct)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // Ensure we clean up the request
                    // Especially important if it has open files/streams
                    rq?.Dispose();
                }
            };
        }

        /// <summary>Buffers, sends and processes the request, deserializing the response or building an API response.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="client">The HTTP client to send with.</param>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="rq">The request message to send.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized result, or default when there is no content.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<T?> SendAndProcessResponseAsync<T, TBody>(
            HttpClient client,
            RestMethodInfoInternal restMethod,
            HttpRequestMessage rq,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? resp = null;
            HttpContent? content = null;
            var disposeResponse = true;
            try
            {
                if (IsBodyBuffered(restMethod, rq))
                {
                    await rq.Content!.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
                }

                var sendResult = await SendOrCaptureExceptionAsync<T, TBody>(
                        client,
                        restMethod,
                        rq,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!sendResult.HasResponse)
                {
                    return sendResult.FailureResult;
                }

                resp = sendResult.Response!;
                content = resp.Content ?? new StringContent(string.Empty);
                disposeResponse = restMethod.ShouldDisposeResponse;

                var e = typeof(T) != typeof(HttpResponseMessage)
                    ? await _settings.ExceptionFactory(resp).ConfigureAwait(false)
                    : null;

                if (restMethod.IsApiResponse)
                {
                    return await BuildApiResponseAsync<T, TBody>(rq, resp, content, e, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (e is not null)
                {
                    disposeResponse = false; // caller has to dispose
                    throw e;
                }

                return await DeserializeOrThrowAsync<T>(rq, resp, content, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (disposeResponse)
                {
                    resp?.Dispose();
                    content?.Dispose();
                }
            }
        }

        /// <summary>Sends the request, capturing a transport failure as a wrapped result for API-response methods.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="client">The HTTP client to send with.</param>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="rq">The request message to send.</param>
        /// <param name="completionOption">The completion option to use.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The send result, either a response or a captured failure result.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<SendResult<T>> SendOrCaptureExceptionAsync<T, TBody>(
            HttpClient client,
            RestMethodInfoInternal restMethod,
            HttpRequestMessage rq,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await client
                    .SendAsync(rq, completionOption, cancellationToken)
                    .ConfigureAwait(false);
                return SendResult<T>.FromResponse(response);
            }
            catch (Exception ex)
            {
                if (!restMethod.IsApiResponse)
                {
                    throw new ApiRequestException(rq, rq.Method, _settings, ex);
                }

                var failure = ApiResponse.Create<T, TBody>(
                    rq,
                    null,
                    default,
                    _settings,
                    new ApiRequestException(rq, rq.Method, _settings, ex));
                return SendResult<T>.FromFailure(failure);
            }
        }

        /// <summary>Builds an API response, deserializing the body and capturing any deserialization failure.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="rq">The request message.</param>
        /// <param name="resp">The response message.</param>
        /// <param name="content">The response content.</param>
        /// <param name="existingException">An exception already produced by the exception factory, if any.</param>
        /// <param name="cancellationToken">A token to cancel the read.</param>
        /// <returns>The constructed API response.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<T?> BuildApiResponseAsync<T, TBody>(
            HttpRequestMessage rq,
            HttpResponseMessage resp,
            HttpContent content,
            Exception? existingException,
            CancellationToken cancellationToken)
        {
            var e = existingException;
            var body = default(TBody);

            try
            {
                // Only attempt to deserialize content if no error present for backward-compatibility.
                body =
                    e is null
                        ? await DeserializeContentAsync<TBody>(resp, content, cancellationToken)
                            .ConfigureAwait(false)
                        : default;
            }
            catch (Exception ex)
            {
                // If an error occured while attempting to deserialize, return the wrapped ApiException.
                e = await CreateDeserializationExceptionAsync(rq, resp, ex).ConfigureAwait(false);
            }

            return ApiResponse.Create<T, TBody>(
                rq,
                resp,
                body,
                _settings,
                e as ApiException);
        }

        /// <summary>Deserializes the response content, throwing a wrapped exception on failure.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <param name="rq">The request message.</param>
        /// <param name="resp">The response message.</param>
        /// <param name="content">The response content.</param>
        /// <param name="cancellationToken">A token to cancel the read.</param>
        /// <returns>The deserialized result.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<T?> DeserializeOrThrowAsync<T>(
            HttpRequestMessage rq,
            HttpResponseMessage resp,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            try
            {
                return await DeserializeContentAsync<T>(resp, content, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_settings.DeserializationExceptionFactory is not null)
                {
                    var customEx = await _settings
                        .DeserializationExceptionFactory(resp, ex)
                        .ConfigureAwait(false);
                    if (customEx is not null)
                    {
                        throw customEx;
                    }

                    return default;
                }

                throw await ApiException.Create(
                    DeserializationErrorMessage,
                    rq,
                    rq.Method,
                    resp,
                    _settings,
                    ex).ConfigureAwait(false);
            }
        }

        /// <summary>Produces a wrapped deserialization exception using the configured factory or a default.</summary>
        /// <param name="rq">The request message.</param>
        /// <param name="resp">The response message.</param>
        /// <param name="ex">The original deserialization exception.</param>
        /// <returns>The wrapped exception, or null when a configured factory returns null.</returns>
        private async Task<Exception?> CreateDeserializationExceptionAsync(
            HttpRequestMessage rq,
            HttpResponseMessage resp,
            Exception ex)
        {
            if (_settings.DeserializationExceptionFactory is not null)
            {
                return await _settings.DeserializationExceptionFactory(resp, ex).ConfigureAwait(false);
            }

            return await ApiException.Create(
                DeserializationErrorMessage,
                rq,
                rq.Method,
                resp,
                _settings,
                ex).ConfigureAwait(false);
        }

        /// <summary>Deserializes the response content into the requested type.</summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="resp">The response message.</param>
        /// <param name="content">The response content.</param>
        /// <param name="cancellationToken">A token to cancel the read.</param>
        /// <returns>The deserialized value, or default when there is no content.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<T?> DeserializeContentAsync<T>(
            HttpResponseMessage resp,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            T? result;
            if (typeof(T) == typeof(HttpResponseMessage))
            {
                // NB: This double-casting manual-boxing hate crime is the only way to make
                // this work without a 'class' generic constraint. It could blow up at runtime
                // and would be A Bad Idea if we hadn't already vetted the return type.
                result = (T)(object)resp;
            }
            else if (typeof(T) == typeof(HttpContent))
            {
                result = (T)(object)content;
            }
            else if (typeof(T) == typeof(Stream))
            {
                var stream = (object)
                    await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                result = (T)stream;
            }
            else if (typeof(T) == typeof(string))
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
                    result = (T)str;
                }
            }
            else
            {
                result = await DeserializeSerializedContentAsync<T>(resp, content, cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>Buffers and deserializes serialized content (e.g. JSON or XML) via the configured serializer.</summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="resp">The response message.</param>
        /// <param name="content">The response content.</param>
        /// <param name="cancellationToken">A token to cancel the read.</param>
        /// <returns>The deserialized value, or default when there is no content.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private async Task<T?> DeserializeSerializedContentAsync<T>(
            HttpResponseMessage resp,
            HttpContent content,
            CancellationToken cancellationToken)
        {
            // A 204 No Content response (or an explicitly empty body) has nothing to
            // deserialize. Return the default value rather than letting the serializer
            // fail on empty content.
            if (resp.StatusCode == System.Net.HttpStatusCode.NoContent
                || content.Headers.ContentLength == 0)
            {
                return default;
            }

            // Buffer the content into memory before deserializing so that, if the
            // serializer throws, ApiException.Create can still re-read the raw body
            // (see #2098). We deliberately do NOT probe the stream via
            // ReadAsStreamAsync first: that consumes non-seekable network streams and
            // breaks serializers that re-read via ReadAsStringAsync, e.g. XML (#1729,
            // which reverted #1705). LoadIntoBufferAsync is a no-op for the already
            // buffered content that HttpClient produces by default.
            await TryBufferContentAsync(content, cancellationToken).ConfigureAwait(false);

            return await _serializer
                .FromHttpContentAsync<T>(content, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>The outcome of attempting to send a request: either a response or a captured failure result.</summary>
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
}
