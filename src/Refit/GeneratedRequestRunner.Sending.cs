// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Request-sending entry points that dispatch a built request and process, observe, or stream its response.</summary>
public static partial class GeneratedRequestRunner
{
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
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
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
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
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
            // source only when both can cancel - mirroring StreamAsync.
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
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
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

        // Only allocate a linked source when both tokens can actually cancel; linking a non-cancelable token is a
        // no-op, so when the method has no CancellationToken parameter or the consumer enumerates without
        // WithCancellation the request runs against whichever token can cancel (or none) with no CTS allocation.
        var (token, linked) = ResolveRequestCancellationToken(methodCancellationToken, cancellationToken);

        try
        {
            await foreach (var item in RequestExecutionHelpers
                               .StreamResponseAsync<T>(client, request, settings, true, token)
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
}
