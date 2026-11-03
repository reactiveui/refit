// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Extension methods for registering Refit clients with dependency injection.</summary>
public static partial class HttpClientFactoryExtensions
{
    /// <summary>Registers Refit clients directly on an <see cref="IServiceCollection"/>.</summary>
    /// <param name="services">The service collection the Refit client is registered with.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a source-generated Refit client to the dependency injection container without any reflection
        /// fallback, making it safe for Native AOT and trimming. The client is built through
        /// <c>RestService.ForGenerated&lt;T&gt;</c>; if no generated implementation exists for <typeparamref name="T"/>,
        /// resolving the client throws an <see cref="InvalidOperationException"/>. The usual HttpClientFactory
        /// features (base address, handlers, resilience pipelines) remain available on the returned builder.
        /// </summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>()
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                static _ => null,
                null);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(RefitSettings? settings)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                _ => settings,
                null);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            RefitSettings? settings,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                _ => settings,
                httpClientName);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                settingsAction,
                null);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                settingsAction,
                httpClientName);
        }

        /// <summary>
        /// Adds a source-generated Refit client to the dependency injection container under the specified
        /// service key, without any reflection fallback, making it safe for Native AOT and trimming. The client
        /// is built through <c>RestService.ForGenerated&lt;T&gt;</c>; if no generated implementation exists for
        /// <typeparamref name="T"/>, resolving the client throws an <see cref="InvalidOperationException"/>. The
        /// usual HttpClientFactory features (base address, handlers, resilience pipelines) remain available on
        /// the returned builder.
        /// </summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(object? serviceKey)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                static _ => null,
                null);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container under the specified service key, without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            RefitSettings? settings)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                _ => settings,
                null);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container under the specified service key, without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            RefitSettings? settings,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                _ => settings,
                httpClientName);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container under the specified service key, without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                settingsAction,
                null);
        }

        /// <summary>Adds a source-generated Refit client to the dependency injection container under the specified service key, without any reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                settingsAction,
                httpClientName);
        }
    }
}
