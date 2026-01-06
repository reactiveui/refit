
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Refit
{
    /// <summary>
    /// HttpClientFactoryExtensions
    /// </summary>
    internal static class HttpClientFactoryCore
    {

        internal static IHttpClientBuilder AddRefitClientCore(
             IServiceCollection services,
             Type refitInterfaceType,
             Func<IServiceProvider, RefitSettings?>? settings,
             string? httpClientName
         )
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));

            // register settings
            var settingsType = typeof(SettingsFor<>).MakeGenericType(refitInterfaceType);
            services.AddSingleton(
                settingsType,
                provider => Activator.CreateInstance(
                    typeof(SettingsFor<>).MakeGenericType(refitInterfaceType)!,
                    settings?.Invoke(provider)
                )!
            );

            // register RequestBuilder
            var requestBuilderType = typeof(IRequestBuilder<>).MakeGenericType(refitInterfaceType);
            services.AddSingleton(
                requestBuilderType,
                provider => RequestBuilderGenericForTypeMethod
                    .MakeGenericMethod(refitInterfaceType)
                    .Invoke(
                        null,
                        [((ISettingsFor)provider.GetRequiredService(settingsType)).Settings]
                    )!
            );

            // create HttpClientBuilder
            var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType(refitInterfaceType));

            // configure message handler
            builder.ConfigureHttpMessageHandlerBuilder(builderConfig =>
            {
                var handler = CreateInnerHandlerIfProvided(
                    ((ISettingsFor)builderConfig.Services.GetRequiredService(settingsType)).Settings
                );
                if (handler != null)
                {
                    builderConfig.PrimaryHandler = handler;
                }
            });

            // add typed client (register transient that resolves HttpClient from IHttpClientFactory and creates Refit client)
            builder.Services.AddTransient(
                refitInterfaceType,
                s =>
                {
                    var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(builder.Name);
                    return RestService.For(
                        refitInterfaceType,
                        httpClient,
                        (IRequestBuilder)s.GetRequiredService(requestBuilderType)
                    );
                }
            );

            return builder;
        }

        internal static IHttpClientBuilder AddKeyedRefitClientCore(
            IServiceCollection services,
            Type refitInterfaceType,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settings,
            string? httpClientName
        )
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (refitInterfaceType == null) throw new ArgumentNullException(nameof(refitInterfaceType));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            // register settings
            var settingsType = typeof(SettingsFor<>).MakeGenericType(refitInterfaceType);
            services.AddKeyedSingleton(
                settingsType,
                serviceKey,
                (provider, _) => Activator.CreateInstance(
                    typeof(SettingsFor<>).MakeGenericType(refitInterfaceType)!,
                    settings?.Invoke(provider)
                )!
            );

            // register RequestBuilder
            var requestBuilderType = typeof(IRequestBuilder<>).MakeGenericType(refitInterfaceType);
            services.AddKeyedSingleton(
                requestBuilderType,
                serviceKey,
                (provider, _) => RequestBuilderGenericForTypeMethod
                    .MakeGenericMethod(refitInterfaceType)
                    .Invoke(
                        null,
                        [((ISettingsFor)provider.GetRequiredKeyedService(settingsType, serviceKey)).Settings]
                    )!
            );

            // create HttpClientBuilder
            var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType(refitInterfaceType, serviceKey));

            // configure primary handler
            builder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var settingsInstance = (ISettingsFor)serviceProvider.GetRequiredKeyedService(settingsType, serviceKey);
                return settingsInstance.Settings?.HttpMessageHandlerFactory?.Invoke() ?? new HttpClientHandler();
            });

            // configure additional handlers
            builder.ConfigureAdditionalHttpMessageHandlers((handlers, serviceProvider) =>
            {
                var settingsInstance = (ISettingsFor)serviceProvider.GetRequiredKeyedService(settingsType, serviceKey);
                if (settingsInstance.Settings?.AuthorizationHeaderValueGetter is { } getToken)
                {
                    handlers.Add(new AuthenticatedHttpClientHandler(null, getToken));
                }
            });

            // add keyed typed client (register keyed transient that resolves HttpClient and creates Refit client)
            builder.Services.AddKeyedTransient(
                refitInterfaceType,
                serviceKey,
                (s, _) =>
                {
                    var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(builder.Name);
                    return RestService.For(
                        refitInterfaceType,
                        httpClient,
                        (IRequestBuilder)s.GetRequiredKeyedService(requestBuilderType, serviceKey)
                    );
                }
            );

            return builder;
        }

        internal static IHttpClientBuilder AddRefitClientCore<T>(
            IServiceCollection services,
            Type refitInterfaceType,
            Func<IServiceProvider, RefitSettings?>? settings,
            string? httpClientName
        ) where T : class
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // register settings
            services.AddSingleton(provider => new SettingsFor<T>(settings?.Invoke(provider)));

            // register RequestBuilder
            services.AddSingleton(provider =>
                RequestBuilder.ForType<T>(provider.GetRequiredService<SettingsFor<T>>().Settings)
            );

            // create HttpClientBuilder
            var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType<T>());

            // configure message handler
            builder.ConfigureHttpMessageHandlerBuilder(builderConfig =>
            {
                var handler = CreateInnerHandlerIfProvided(
                    builderConfig.Services.GetRequiredService<SettingsFor<T>>().Settings
                );
                if (handler != null)
                {
                    builderConfig.PrimaryHandler = handler;
                }
            });

            // add typed client using framework AddTypedClient
            return builder.AddTypedClient((client, serviceProvider) =>
                RestService.For<T>(
                    client,
                    serviceProvider.GetRequiredService<IRequestBuilder<T>>()
                )
            );
        }

        internal static IHttpClientBuilder AddKeyedRefitClientCore<T>(
            IServiceCollection services,
            Type refitInterfaceType,
            object? serviceKey,
            Func<IServiceProvider, RefitSettings?>? settings,
            string? httpClientName
        ) where T : class
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (serviceKey == null) throw new ArgumentNullException(nameof(serviceKey));

            // register settings
            services.AddKeyedSingleton(
                serviceKey,
                (provider, _) => new SettingsFor<T>(settings?.Invoke(provider))
            );

            // register RequestBuilder
            services.AddKeyedSingleton(
                serviceKey,
                (provider, _) =>
                    RequestBuilder.ForType<T>(
                        provider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey).Settings
                    )
            );

            // create HttpClientBuilder
            var builder = services.AddHttpClient(httpClientName ?? UniqueName.ForType<T>(serviceKey));

            // configure primary handler
            builder.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                var settingsInstance = serviceProvider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey).Settings;
                return settingsInstance?.HttpMessageHandlerFactory?.Invoke() ?? new HttpClientHandler();
            });

            // configure additional handlers
            builder.ConfigureAdditionalHttpMessageHandlers((handlers, serviceProvider) =>
            {
                var settingsInstance = serviceProvider.GetRequiredKeyedService<SettingsFor<T>>(serviceKey).Settings;
                if (settingsInstance?.AuthorizationHeaderValueGetter is { } getToken)
                {
                    handlers.Add(new AuthenticatedHttpClientHandler(null, getToken));
                }
            });

            // add keyed typed client (inline keyed registration)
            builder.Services.AddKeyedTransient(
                serviceKey,
                (s, _) =>
                {
                    var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(builder.Name);
                    return RestService.For<T>(
                        httpClient,
                        s.GetRequiredKeyedService<IRequestBuilder<T>>(serviceKey)
                    );
                }
            );

            return builder;
        }

        // helper - used by AddRefitClientCore and AddRefitClientCore<T>
        private static HttpMessageHandler? CreateInnerHandlerIfProvided(RefitSettings? settings)
        {
            HttpMessageHandler? innerHandler = null;
            if (settings != null)
            {
                if (settings.HttpMessageHandlerFactory != null)
                {
                    innerHandler = settings.HttpMessageHandlerFactory();
                }

                if (settings.AuthorizationHeaderValueGetter != null)
                {
                    innerHandler = new AuthenticatedHttpClientHandler(
                        settings.AuthorizationHeaderValueGetter,
                        innerHandler
                    );
                }
            }

            return innerHandler;
        }

        private static readonly MethodInfo RequestBuilderGenericForTypeMethod =
            typeof(RequestBuilder)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(z => z.IsGenericMethodDefinition && z.GetParameters().Length == 1);
    }
}
