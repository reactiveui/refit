// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Extension methods for registering Refit clients with dependency injection.</summary>
public static partial class HttpClientFactoryExtensions
{
    /// <summary>Registers Refit clients on an existing <see cref="IHttpClientBuilder"/>.</summary>
    /// <param name="builder">The HTTP client builder the Refit client is registered with.</param>
    extension(IHttpClientBuilder builder)
    {
        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddRefitClient(Type refitInterfaceType)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                builder.Services,
                refitInterfaceType,
                static _ => null,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddRefitClient(
            Type refitInterfaceType,
            RefitSettings? settings)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                builder.Services,
                refitInterfaceType,
                _ => settings,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddRefitClient(
            Type refitInterfaceType,
            Func<IServiceProvider, RefitSettings?>? settingsAction)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                builder.Services,
                refitInterfaceType,
                settingsAction,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>()
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                builder.Services,
                static _ => null,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(RefitSettings? settings)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                builder.Services,
                _ => settings,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                builder.Services,
                settingsAction,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient(
            Type refitInterfaceType,
            object? serviceKey)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                builder.Services,
                refitInterfaceType,
                serviceKey,
                static _ => null,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient(
            Type refitInterfaceType,
            object? serviceKey,
            RefitSettings? settings)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                builder.Services,
                refitInterfaceType,
                serviceKey,
                _ => settings,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient(
            Type refitInterfaceType,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction)
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                builder.Services,
                refitInterfaceType,
                serviceKey,
                settingsAction,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(object? serviceKey)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                builder.Services,
                serviceKey,
                static _ => null,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(
            object? serviceKey,
            RefitSettings? settings)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                builder.Services,
                serviceKey,
                _ => settings,
                builder.Name);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(builder);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                builder.Services,
                serviceKey,
                settingsAction,
                builder.Name);
        }

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
