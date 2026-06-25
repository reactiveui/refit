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
        "Registering Refit clients with HttpClientFactory requires reflected interface and request metadata.";

    /// <summary>Diagnostic message explaining why the reflection-based registration path is not AOT-safe.</summary>
    private const string RequiresDynamicCodeMessage =
        "Registering Refit clients by Type with HttpClientFactory requires runtime generic type and method instantiation.";

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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>()
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(RefitSettings? settings)
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(Func<IServiceProvider, RefitSettings?>? settingsAction)
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(object? serviceKey)
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
        public IHttpClientBuilder AddKeyedRefitClient<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.Interfaces |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            T>(
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

        /// <summary>
        /// Adds a source-generated Refit client to the dependency injection container without any reflection
        /// fallback, making it safe for Native AOT and trimming. The client is built through
        /// <c>RestService.ForGenerated&lt;T&gt;</c>; if no generated implementation exists for <typeparamref name="T"/>,
        /// resolving the client throws an <see cref="InvalidOperationException"/>. The usual HttpClientFactory
        /// features (base address, handlers, resilience pipelines) remain available on the returned builder.
        /// </summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
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
    }
}
