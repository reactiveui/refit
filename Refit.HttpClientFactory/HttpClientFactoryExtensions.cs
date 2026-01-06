using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Refit
{
    /// <summary>
    /// HttpClientFactoryExtensions.
    /// </summary>
    public static class HttpClientFactoryExtensions
    {
        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="services">container</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitClient(
            this IServiceCollection services,
            Type refitInterfaceType,
            RefitSettings? settings = null,
            string? httpClientName = null
        )
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));

            return HttpClientFactoryCore.AddRefitClientCore(services, refitInterfaceType, _ => settings, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="services">container</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddKeyedRefitClient(
            this IServiceCollection services,
            Type refitInterfaceType,
            object? serviceKey,
            RefitSettings? settings = null,
            string? httpClientName = null
        )
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore(services, refitInterfaceType, serviceKey, _ => settings, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="services">container</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitClient<T>(
            this IServiceCollection services,
            RefitSettings? settings = null,
            string? httpClientName = null
        )
            where T : class
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return HttpClientFactoryCore.AddRefitClientCore<T>(services, typeof(T), _ => settings, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="services">container</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddKeyedRefitClient<T>(
            this IServiceCollection services,
            object? serviceKey,
            RefitSettings? settings = null,
            string? httpClientName = null
        )
            where T : class
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(services, typeof(T), serviceKey, _ => settings, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitClient(
            this IHttpClientBuilder builder,
            Type refitInterfaceType,
            RefitSettings? settings = null
        )
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));

            return HttpClientFactoryCore.AddRefitClientCore(builder.Services, refitInterfaceType, _ => settings, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddKeyedRefitClient(
            this IHttpClientBuilder builder,
            Type refitInterfaceType,
            object? serviceKey,
            RefitSettings? settings = null
        )
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore(builder.Services, refitInterfaceType, serviceKey, _ => settings, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitClient<T>(
            this IHttpClientBuilder builder,
            RefitSettings? settings = null
        )
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return HttpClientFactoryCore.AddRefitClientCore<T>(builder.Services, typeof(T), _ => settings, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddKeyedRefitClient<T>(
            this IHttpClientBuilder builder,
            object? serviceKey,
            RefitSettings? settings = null
        )
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(builder.Services, typeof(T), serviceKey, _ => settings, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="services">container</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddRefitClient(
            this IServiceCollection services,
            Type refitInterfaceType,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName = null
        )
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));

            return HttpClientFactoryCore.AddRefitClientCore(services, refitInterfaceType, settingsAction, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="services">container</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddKeyedRefitClient(
            this IServiceCollection services,
            Type refitInterfaceType,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName = null
        )
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore(services, refitInterfaceType, serviceKey, settingsAction, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="services">container</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddRefitClient<T>(
            this IServiceCollection services,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName = null
        )
            where T : class
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return HttpClientFactoryCore.AddRefitClientCore<T>(services, typeof(T), settingsAction, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="services">container</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <param name="httpClientName">Optional. Allows users to change the HttpClient name.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddKeyedRefitClient<T>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction,
            string? httpClientName = null
        )
            where T : class
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(services, typeof(T), serviceKey, settingsAction, httpClientName);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddRefitClient(
            this IHttpClientBuilder builder,
            Type refitInterfaceType,
            Func<IServiceProvider, RefitSettings?>? settingsAction
        )
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));

            return HttpClientFactoryCore.AddRefitClientCore(builder.Services, refitInterfaceType, settingsAction, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <param name="refitInterfaceType">Type of the Refit interface</param>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddKeyedRefitClient(
            this IHttpClientBuilder builder,
            Type refitInterfaceType,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction
        )
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore(builder.Services, refitInterfaceType, serviceKey, settingsAction, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddRefitClient<T>(
            this IHttpClientBuilder builder,
            Func<IServiceProvider, RefitSettings?>? settingsAction
        )
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return HttpClientFactoryCore.AddRefitClientCore<T>(builder.Services, typeof(T), settingsAction, builder.Name);
        }

        /// <summary>
        /// Adds a Refit client to the DI container with a specified service key
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="builder">The HTTP client builder</param>
        /// <param name="serviceKey">An optional key to associate with the specific Refit client instance</param>
        /// <param name="settingsAction">Optional. Action to configure refit settings.</param>
        /// <returns></returns>
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
#endif
        public static IHttpClientBuilder AddKeyedRefitClient<T>(
            this IHttpClientBuilder builder,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settingsAction
        )
            where T : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            return HttpClientFactoryCore.AddKeyedRefitClientCore<T>(builder.Services, typeof(T), serviceKey, settingsAction, builder.Name);
        }

    }
}