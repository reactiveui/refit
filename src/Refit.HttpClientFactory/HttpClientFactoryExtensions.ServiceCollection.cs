// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Extension methods for registering Refit clients with dependency injection.</summary>
public static partial class HttpClientFactoryExtensions
{
    /// <summary>Diagnostic message explaining why the reflection-based registration path is not trim-safe.</summary>
    private const string RequiresUnreferencedCodeMessage =
        "Refit's HttpClientFactory integration uses reflection to build clients and is not trim-safe; use the Refit source generator for trimmed/AOT apps.";

    /// <summary>Diagnostic message explaining why the reflection-based registration path is not AOT-safe.</summary>
    private const string RequiresDynamicCodeMessage =
        "Refit's HttpClientFactory integration builds generic types at runtime; use the Refit source generator for AOT apps.";

    /// <summary>Registers Refit clients directly on an <see cref="IServiceCollection"/>.</summary>
    /// <param name="services">The service collection the Refit client is registered with.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddRefitClient(Type refitInterfaceType)
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                services,
                refitInterfaceType,
                static _ => null,
                null);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                services,
                refitInterfaceType,
                _ => settings,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddRefitClient(
            Type refitInterfaceType,
            RefitSettings? settings,
            string? httpClientName)
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                services,
                refitInterfaceType,
                _ => settings,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                services,
                refitInterfaceType,
                settingsAction,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddRefitClient(
            Type refitInterfaceType,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            return HttpClientFactoryCore.AddRefitClientCore(
                services,
                refitInterfaceType,
                settingsAction,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                services,
                static _ => null,
                null);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                services,
                _ => settings,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitClient<T>(
            RefitSettings? settings,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                services,
                _ => settings,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                services,
                settingsAction,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitClient<T>(
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitClientCore<T>(
                services,
                settingsAction,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                services,
                refitInterfaceType,
                serviceKey,
                static _ => null,
                null);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                services,
                refitInterfaceType,
                serviceKey,
                _ => settings,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient(
            Type refitInterfaceType,
            object? serviceKey,
            RefitSettings? settings,
            string? httpClientName)
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                services,
                refitInterfaceType,
                serviceKey,
                _ => settings,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                services,
                refitInterfaceType,
                serviceKey,
                settingsAction,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <param name="refitInterfaceType">The type of the Refit interface.</param>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient(
            Type refitInterfaceType,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(refitInterfaceType);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore(
                services,
                refitInterfaceType,
                serviceKey,
                settingsAction,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                services,
                serviceKey,
                static _ => null,
                null);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                services,
                serviceKey,
                _ => settings,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settings">The settings used to configure the instance.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitClient<T>(
            object? serviceKey,
            RefitSettings? settings,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                services,
                serviceKey,
                _ => settings,
                httpClientName);
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
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                services,
                serviceKey,
                settingsAction,
                null);
        }

        /// <summary>Adds a Refit client to the dependency injection container with a specified service key.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="settingsAction">An action used to configure the Refit settings from the service provider.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitClient<T>(
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(
                services,
                serviceKey,
                settingsAction,
                httpClientName);
        }
    }
}
