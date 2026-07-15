// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
internal partial class RequestBuilderImplementation
{
    /// <summary>Builds and streams the response for a method returning an asynchronous sequence.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <param name="linked">The linked cancellation source, disposed when enumeration finishes.</param>
    /// <returns>The streamed elements.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    internal async IAsyncEnumerable<T?> StreamAsyncEnumerableRequestAsync<T>(
        HttpClient client,
        RestMethodInfoInternal restMethod,
        object[] paramList,
        CancellationTokenSource linked)
    {
        // Disposed through an explicit finally rather than 'using': the compiler would emit a null check
        // on the source, and CreateLinkedTokenSource never returns null.
        try
        {
            RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

            var request = await BuildRequestMessageForMethodAsync(
                    restMethod,
                    client.BaseAddress!.AbsolutePath,
                    restMethod.CancellationToken is not null,
                    paramList)
                .ConfigureAwait(false);

            await foreach (var item in RequestExecutionHelpers
                .StreamResponseAsync<T>(client, request, _settings, false, restMethod.TimeoutMilliseconds, linked.Token)
                .ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            linked.Dispose();
        }
    }

    /// <summary>Gets the cancellation token declared by the interface method, if any.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <returns>The method's cancellation token, or <see cref="CancellationToken.None"/>.</returns>
    private static CancellationToken GetMethodCancellationToken(
        RestMethodInfoInternal restMethod,
        object[] paramList) =>
        restMethod.CancellationToken is not null
            ? GetCancellationToken(paramList)
            : CancellationToken.None;

    /// <summary>Determines whether the request body should be buffered before sending.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="request">The built request message.</param>
    /// <returns><see langword="true"/> if the body should be buffered; otherwise <see langword="false"/>.</returns>
    private static bool IsBodyBuffered(
        RestMethodInfoInternal restMethod,
        HttpRequestMessage request) =>
        (restMethod.BodyParameterInfo?.Item2 ?? false) && request.Content is not null;

    /// <summary>Builds and sends the request for a method with no response body, throwing on error.</summary>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <param name="paramsContainsCancellationToken">Whether the argument list contains a cancellation token.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private async Task ExecuteVoidRequestAsync(
        HttpClient client,
        RestMethodInfoInternal restMethod,
        object[] paramList,
        bool paramsContainsCancellationToken,
        CancellationToken cancellationToken)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using var request = await BuildRequestMessageForMethodAsync(
                restMethod,
                client.BaseAddress!.AbsolutePath,
                paramsContainsCancellationToken,
                paramList)
            .ConfigureAwait(false);

        await RequestExecutionHelpers.SendVoidAsync(
                client,
                request,
                _settings,
                IsBodyBuffered(restMethod, request),
                false,
                restMethod.TimeoutMilliseconds,
                cancellationToken)
            .ConfigureAwait(false);
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
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private async Task<T?> ExecuteRequestAsync<T, TBody>(
        HttpClient client,
        RestMethodInfoInternal restMethod,
        object[] paramList,
        bool paramsContainsCancellationToken,
        CancellationToken cancellationToken)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using var request = await BuildRequestMessageForMethodAsync(
                restMethod,
                client.BaseAddress!.AbsolutePath,
                paramsContainsCancellationToken,
                paramList)
            .ConfigureAwait(false);

        return await SendAndProcessResponseAsync<T, TBody>(client, restMethod, request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Builds, sends and streams the response for a method returning <see cref="IAsyncEnumerable{T}"/>.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <param name="cancellationToken">A token, supplied by the consumer's enumeration, to cancel streaming.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    /// <remarks>
    /// A forwarding shell only. The <c>EnumeratorCancellation</c> attribute makes the compiler emit a
    /// <c>GetAsyncEnumerator</c> whose branches no test can drive, so the logic lives in
    /// <see cref="StreamAsyncEnumerableRequestAsync{T}"/>, which is covered directly.
    /// </remarks>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    [ExcludeFromCodeCoverage]
    private async IAsyncEnumerable<T?> ExecuteAsyncEnumerableRequestAsync<T>(
        HttpClient client,
        RestMethodInfoInternal restMethod,
        object[] paramList,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var methodCt = GetMethodCancellationToken(restMethod, paramList);
        var linked = CancellationTokenSource.CreateLinkedTokenSource(methodCt, cancellationToken);

        await foreach (var item in StreamAsyncEnumerableRequestAsync<T>(client, restMethod, paramList, linked)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>Builds a cancellable task delegate that sends the request and deserializes the response.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="restMethod">The rest method to build a delegate for.</param>
    /// <returns>A delegate that sends the request with a cancellation token.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private Func<HttpClient, CancellationToken, object[], Task<T?>> BuildCancellableTaskFuncForMethod<T, TBody>(
        RestMethodInfoInternal restMethod) =>
        async (client, ct, paramList) =>
        {
            RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

            var request = await BuildRequestMessageForMethodAsync(
                restMethod,
                client.BaseAddress!.AbsolutePath,
                restMethod.CancellationToken is not null,
                paramList).ConfigureAwait(false);

            try
            {
                return await SendAndProcessResponseAsync<T, TBody>(client, restMethod, request, ct)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Ensure we clean up the request, especially if it has open files/streams.
                request.Dispose();
            }
        };

    /// <summary>Processes a response for a reflection-built request using the shared runtime state machine.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The body type used for API responses.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="request">The request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The deserialized result, or default when there is no content.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private Task<T?> SendAndProcessResponseAsync<T, TBody>(
        HttpClient client,
        RestMethodInfoInternal restMethod,
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        RequestExecutionHelpers.SendAndProcessResponseAsync<T, TBody>(
            client,
            request,
            _settings,
            new(
                restMethod.IsApiResponse,
                restMethod.ShouldDisposeResponse,
                IsBodyBuffered(restMethod, request),
                false),
            restMethod.TimeoutMilliseconds,
            cancellationToken);
}
