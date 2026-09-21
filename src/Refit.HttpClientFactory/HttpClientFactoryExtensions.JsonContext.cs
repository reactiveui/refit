// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Extension methods for registering source-generated Refit clients that run on a source-generated JSON context.</summary>
public static partial class HttpClientFactoryExtensions
{
    /// <summary>Registers source-generated Refit clients that run on a source-generated JSON context.</summary>
    /// <param name="services">The service collection the Refit client is registered with.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a source-generated Refit client that reads and writes JSON with a source-generated context.
        /// Reflection-based JSON is disabled, so a type the context does not describe throws <see cref="NotSupportedException"/>.
        /// </summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(JsonSerializerContext context)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                HttpClientFactoryCore.WithJsonContext(null, context, false),
                null);
        }

        /// <summary>Adds a source-generated Refit client that reads and writes JSON with a source-generated context, optionally with a reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
        /// <param name="allowReflectionFallback"><see langword="true"/> to let a type the context does not describe use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            JsonSerializerContext context,
            bool allowReflectionFallback)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                HttpClientFactoryCore.WithJsonContext(null, context, allowReflectionFallback),
                null);
        }

        /// <summary>Adds a source-generated Refit client that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
        /// <param name="settingsAction">An action that supplies the Refit settings from the service provider. Null settings use the context's own options.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            JsonSerializerContext context,
            Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                HttpClientFactoryCore.WithJsonContext(settingsAction, context, false),
                null);
        }

        /// <summary>Adds a source-generated Refit client that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
        /// <param name="settingsAction">An action that supplies the Refit settings from the service provider. Null settings use the context's own options.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            JsonSerializerContext context,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                HttpClientFactoryCore.WithJsonContext(settingsAction, context, false),
                httpClientName);
        }

        /// <summary>Adds a source-generated Refit client that keeps the settings' serializer options and adds a source-generated JSON context, optionally with a reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
        /// <param name="settingsAction">An action that supplies the Refit settings from the service provider. Null settings use the context's own options.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddRefitGeneratedClient<T>(
            JsonSerializerContext context,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName,
            bool allowReflectionFallback)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            return HttpClientFactoryCore.AddRefitGeneratedClientCore<T>(
                services,
                HttpClientFactoryCore.WithJsonContext(settingsAction, context, allowReflectionFallback),
                httpClientName);
        }

        /// <summary>
        /// Adds a source-generated Refit client under the specified service key that reads and writes JSON with a source-generated context.
        /// Reflection-based JSON is disabled, so a type the context does not describe throws <see cref="NotSupportedException"/>.
        /// </summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="context">The generated context. Its own options supply the naming, converters and number handling.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            JsonSerializerContext context)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                HttpClientFactoryCore.WithJsonContext(null, context, false),
                null);
        }

        /// <summary>Adds a source-generated Refit client under the specified service key that keeps the settings' serializer options and adds a source-generated JSON context.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
        /// <param name="settingsAction">An action that supplies the Refit settings from the service provider. Null settings use the context's own options.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            JsonSerializerContext context,
            Func<IServiceProvider, RefitSettings?>? settingsAction)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                HttpClientFactoryCore.WithJsonContext(settingsAction, context, false),
                null);
        }

        /// <summary>Adds a keyed source-generated Refit client that keeps the settings' serializer options and adds a JSON context, optionally with a reflection fallback.</summary>
        /// <typeparam name="T">The type of the Refit interface.</typeparam>
        /// <param name="serviceKey">A key used to associate with the specific Refit client instance.</param>
        /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
        /// <param name="settingsAction">An action that supplies the Refit settings from the service provider. Null settings use the context's own options.</param>
        /// <param name="httpClientName">Allows the name of the underlying HttpClient to be changed.</param>
        /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        [SuppressMessage(
            "Design",
            "SST2307:Generic method type parameters should be inferable from the parameters",
            Justification = "The Refit interface type is intentionally specified explicitly by callers.")]
        public IHttpClientBuilder AddKeyedRefitGeneratedClient<T>(
            object? serviceKey,
            JsonSerializerContext context,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName,
            bool allowReflectionFallback)
            where T : class
        {
            ArgumentExceptionHelper.ThrowIfNull(services);

            ArgumentExceptionHelper.ThrowIfNull(serviceKey);

            return HttpClientFactoryCore.AddKeyedRefitGeneratedClientCore<T>(
                services,
                serviceKey,
                HttpClientFactoryCore.WithJsonContext(settingsAction, context, allowReflectionFallback),
                httpClientName);
        }
    }
}
#endif
