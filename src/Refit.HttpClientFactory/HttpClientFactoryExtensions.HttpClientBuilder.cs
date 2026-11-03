// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Extension methods for registering Refit clients with dependency injection.</summary>
public static partial class HttpClientFactoryExtensions
{
    /// <summary>Registers Refit clients on an existing <see cref="IHttpClientBuilder"/>.</summary>
    /// <param name="builder">The HTTP client builder the Refit client is registered with.</param>
    extension(IHttpClientBuilder builder)
    {
        /// <summary>Adds a message handler that resolves each request's authorization token from a fresh dependency-injection scope.</summary>
        /// <param name="getToken">
        /// A delegate that resolves the authorization token from a per-request <see cref="IServiceProvider"/>, the outgoing
        /// request, and a cancellation token. Returning null, empty, or whitespace omits the <c>Authorization</c> header for
        /// that request.
        /// </param>
        /// <returns>The HTTP client builder for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="getToken"/> is null.</exception>
        /// <remarks>
        /// <see cref="IHttpClientFactory"/> pools message handlers for their configured lifetime, so a scoped token provider
        /// captured directly would bleed across requests. This registers a handler that creates a fresh
        /// <see cref="IServiceScope"/> per request, resolving <paramref name="getToken"/> from that scope's provider, so a
        /// scoped service (or ambient <c>AsyncLocal</c> state such as a host-registered <c>IHttpContextAccessor</c>) resolves
        /// correctly per request without any ASP.NET Core dependency.
        /// </remarks>
        public IHttpClientBuilder AddAuthorizationHeaderValueProvider(
            Func<IServiceProvider, HttpRequestMessage, CancellationToken, ValueTask<string>> getToken)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(getToken);

            return builder.ConfigureAdditionalHttpMessageHandlers((handlers, serviceProvider) =>
                handlers.Add(new ScopedAuthorizationHeaderHandler(serviceProvider, getToken)));
        }
    }
}
