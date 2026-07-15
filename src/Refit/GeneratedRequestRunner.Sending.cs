// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Request-sending entry points that dispatch a built request and process, observe, or stream its response.</summary>
public static partial class GeneratedRequestRunner
{
    /// <summary>The request-options key under which a method's <c>[Timeout]</c> value is stashed.</summary>
    private const string TimeoutOptionKey = "Refit.Timeout";

    /// <summary>Sends a generated request with no response body, throwing on HTTP errors.</summary>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The generated request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    public static async Task SendVoidAsync(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool bufferBody,
        CancellationToken cancellationToken)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using (request)
        {
            await RequestExecutionHelpers.SendVoidAsync(
                    client,
                    request,
                    settings,
                    bufferBody,
                    true,
                    GetRequestTimeout(request),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Sends a generated request and deserializes or wraps its response.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The generated request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="isApiResponse">Whether the result type is an API response wrapper.</param>
    /// <param name="shouldDisposeResponse">Whether the response should be disposed by this helper.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The deserialized or wrapped response.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    public static async Task<T?> SendAsync<T, TBody>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool isApiResponse,
        bool shouldDisposeResponse,
        bool bufferBody,
        CancellationToken cancellationToken)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using (request)
        {
            return await RequestExecutionHelpers.SendAndProcessResponseAsync<T, TBody>(
                    client,
                    request,
                    settings,
                    new(
                        isApiResponse,
                        shouldDisposeResponse,
                        bufferBody,
                        true),
                    GetRequestTimeout(request),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Sends a generated request as a cold <see cref="IObservable{T}"/>: each subscription rebuilds and sends
    /// the request, mirroring the reflection request builder.</summary>
    /// <typeparam name="T">The result type yielded to subscribers.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="requestFactory">Builds a fresh request per subscription, so a second subscription never reuses a
    /// disposed request.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="isApiResponse">Whether the result type is an API response wrapper.</param>
    /// <param name="shouldDisposeResponse">Whether the response should be disposed by this helper.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="methodCancellationToken">The cancellation token supplied as a method argument, if any.</param>
    /// <returns>A cold observable of the deserialized or wrapped response.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameters intentionally specified explicitly by generated callers.")]
    public static IObservable<T?> SendObservable<T, TBody>(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        RefitSettings settings,
        bool isApiResponse,
        bool shouldDisposeResponse,
        bool bufferBody,
        CancellationToken methodCancellationToken) =>
        new ReactiveUI.Primitives.Advanced.FromAsyncSignal<T?>(async subscriptionToken =>
        {
            // Link the method's CancellationToken argument (if any) with the per-subscription token, allocating a linked
            // source only when both can cancel - mirroring StreamAsync. The per-call timeout (read from the per-subscription
            // request built by requestFactory) is layered on inside SendAsync.
            var (token, linked) = ResolveRequestCancellationToken(methodCancellationToken, subscriptionToken);

            try
            {
                return await SendAsync<T, TBody>(client, requestFactory(), settings, isApiResponse, shouldDisposeResponse, bufferBody, token)
                    .ConfigureAwait(false);
            }
            finally
            {
                linked?.Dispose();
            }
        });

    /// <summary>Sends a generated request and streams the response as an <see cref="IAsyncEnumerable{T}"/>.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The generated request message; disposed when streaming completes.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="methodCancellationToken">The cancellation token supplied as a method argument, if any.</param>
    /// <param name="cancellationToken">The token supplied by the consumer's enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    [SuppressMessage(
        "Design",
        "SST2309:An externally visible member declares an optional parameter, so callers bake in the default",
        Justification = "The optional CancellationToken carries the [EnumeratorCancellation] token for the await-foreach WithCancellation pattern.")]
    [ExcludeFromCodeCoverage] // async-iterator dispose-mode epilogue: the compiler-generated <>w__disposeMode false-edge cannot be exercised or removed.
    public static async IAsyncEnumerable<T?> StreamAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        CancellationToken methodCancellationToken,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        // Read the per-call timeout before enumeration so the request is still readable; StreamResponseAsync layers it
        // onto the effective token.
        var timeoutMilliseconds = GetRequestTimeout(request);

        // Only allocate a linked source when both tokens can actually cancel; linking a non-cancelable token is a
        // no-op, so when the method has no CancellationToken parameter or the consumer enumerates without
        // WithCancellation the request runs against whichever token can cancel (or none) with no CTS allocation.
        var (token, linked) = ResolveRequestCancellationToken(methodCancellationToken, cancellationToken);

        try
        {
            await foreach (var item in RequestExecutionHelpers
                               .StreamResponseAsync<T>(client, request, settings, true, timeoutMilliseconds, token)
                               .ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            linked?.Dispose();
        }
    }

    /// <summary>Stashes a method's <c>[Timeout]</c> value on the request for the send helpers to apply.</summary>
    /// <param name="request">The generated request message to annotate.</param>
    /// <param name="timeoutMilliseconds">The per-call timeout in milliseconds.</param>
    /// <remarks>Emitted by the source generator only for methods that declare a positive timeout.</remarks>
    public static void SetRequestTimeout(HttpRequestMessage request, int timeoutMilliseconds) =>
        AddRequestProperty(request, TimeoutOptionKey, timeoutMilliseconds);

    /// <summary>Resolves the effective cancellation token for a request, linking the method-argument token with the
    /// per-subscription or enumeration token into a new source only when both can cancel.</summary>
    /// <param name="methodCancellationToken">The cancellation token supplied as a method argument, if any.</param>
    /// <param name="consumerCancellationToken">The per-subscription (or per-enumeration) cancellation token.</param>
    /// <returns>The token the request should run against, and the linked source to dispose once the request completes
    /// (null when no source was allocated).</returns>
    private static (CancellationToken Token, CancellationTokenSource? LinkedSource) ResolveRequestCancellationToken(
        CancellationToken methodCancellationToken,
        CancellationToken consumerCancellationToken)
    {
        if (methodCancellationToken.CanBeCanceled && consumerCancellationToken.CanBeCanceled)
        {
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(methodCancellationToken, consumerCancellationToken);
            return (linkedSource.Token, linkedSource);
        }

        return (methodCancellationToken.CanBeCanceled ? methodCancellationToken : consumerCancellationToken, null);
    }

    /// <summary>Reads the per-call timeout stashed by <see cref="SetRequestTimeout"/>, or 0 when none was set.</summary>
    /// <param name="request">The generated request message to inspect.</param>
    /// <returns>The per-call timeout in milliseconds, or 0 when none was set.</returns>
    private static int GetRequestTimeout(HttpRequestMessage request) =>
#if NET6_0_OR_GREATER
        request.Options.TryGetValue(new HttpRequestOptionsKey<int>(TimeoutOptionKey), out var timeoutMilliseconds)
            ? timeoutMilliseconds
            : 0;
#else
        request.Properties.TryGetValue(TimeoutOptionKey, out var value) && value is int timeoutMilliseconds
            ? timeoutMilliseconds
            : 0;
#endif
}
