// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

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

        /// <summary>Builds and sends the request for a method with no response body, throwing on error.</summary>
        /// <param name="client">The HTTP client to send with.</param>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="paramList">The argument values for the call.</param>
        /// <param name="paramsContainsCancellationToken">Whether the argument list contains a cancellation token.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>A task that completes when the request finishes.</returns>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
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
                    request!,
                    _settings,
                    IsBodyBuffered(restMethod, request),
                    false,
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        [SuppressMessage(
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
            RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

            using var request = await BuildRequestMessageForMethodAsync(
                    restMethod,
                    client.BaseAddress!.AbsolutePath,
                    paramsContainsCancellationToken,
                    paramList)
                .ConfigureAwait(false);

            return await SendAndProcessResponseAsync<T, TBody>(client, restMethod, request!, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>Builds a cancellable task delegate that sends the request and deserializes the response.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="restMethod">The rest method to build a delegate for.</param>
        /// <returns>A delegate that sends the request with a cancellation token.</returns>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private Func<HttpClient, CancellationToken, object[], Task<T?>> BuildCancellableTaskFuncForMethod<T, TBody>(
            RestMethodInfoInternal restMethod)
        {
            return async (client, ct, paramList) =>
            {
                RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

                var request = await BuildRequestMessageForMethodAsync(
                    restMethod,
                    client.BaseAddress!.AbsolutePath,
                    restMethod.CancellationToken is not null,
                    paramList).ConfigureAwait(false);

                try
                {
                    return await SendAndProcessResponseAsync<T, TBody>(client, restMethod, request!, ct)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // Ensure we clean up the request, especially if it has open files/streams.
                    request?.Dispose();
                }
            };
        }

        /// <summary>Processes a response for a reflection-built request using the shared runtime state machine.</summary>
        /// <typeparam name="T">The result type returned to the caller.</typeparam>
        /// <typeparam name="TBody">The body type used for API responses.</typeparam>
        /// <param name="client">The HTTP client to send with.</param>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The deserialized result, or default when there is no content.</returns>
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
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
                new RequestExecutionOptions(
                    restMethod.IsApiResponse,
                    restMethod.ShouldDisposeResponse,
                    IsBodyBuffered(restMethod, request),
                    false),
                cancellationToken);
    }
}
