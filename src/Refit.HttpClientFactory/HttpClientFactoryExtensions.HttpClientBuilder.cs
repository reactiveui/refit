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
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
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
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitClient<T>()
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
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitClient<T>(RefitSettings? settings)
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
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitClient<T>(Func<IServiceProvider, RefitSettings?>? settingsAction)
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
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
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
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitClient<T>(object? serviceKey)
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
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitClient<T>(
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
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitClient<T>(
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
    }
}
