// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Streaming send and framing helpers for the shared request execution pipeline.</summary>
internal static partial class RequestExecutionHelpers
{
    /// <summary>Sends the request and streams its body, throwing on HTTP errors and disposing the request when done.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The request message; disposed when streaming completes.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="streamingSerializer">The streaming-capable content serializer.</param>
    /// <param name="applyAuthorizationHeader">Whether to apply the configured authorization getter before sending.</param>
    /// <param name="timeoutSource">The per-call timeout source to dispose when streaming completes, or null when no timeout was applied.</param>
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
        CancellationTokenSource? timeoutSource,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
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
        finally
        {
            timeoutSource?.Dispose();
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
}
