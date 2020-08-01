using System;
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

namespace Refit
{
    public static class HttpClientFactoryExtensions
    {
        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="services">container</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitClient<T>(this IServiceCollection services, RefitSettings settings = null) where T : class
        {
            services.AddSingleton(provider => RequestBuilder.ForType<T>(settings));

            return services.AddHttpClient(UniqueName.ForType<T>())
                           .ConfigureHttpMessageHandlerBuilder(builder =>
                           {
                               // check to see if user provided custom auth token
                               HttpMessageHandler innerHandler = null;
                               if (settings != null)
                               {
                                   if (settings.HttpMessageHandlerFactory != null)
                                   {
                                       innerHandler = settings.HttpMessageHandlerFactory();
                                   }

                                   if (settings.AuthorizationHeaderValueGetter != null)
                                   {
                                       innerHandler = new AuthenticatedHttpClientHandler(settings.AuthorizationHeaderValueGetter, innerHandler);
                                   }
                                   else if (settings.AuthorizationHeaderValueWithParamGetter != null)
                                   {
                                       innerHandler = new AuthenticatedParameterizedHttpClientHandler(settings.AuthorizationHeaderValueWithParamGetter, innerHandler);
                                   }
                               }

                               if(innerHandler != null)
                               {
                                   builder.PrimaryHandler = innerHandler;
                               }    

                           })
                           .AddTypedClient((client, serviceProvider) => RestService.For<T>(client, serviceProvider.GetService<IRequestBuilder<T>>()));
        }

        /// <summary>
        /// Adds a Refit client to the DI container
        /// </summary>
        /// <param name="services">container</param>
        /// <param name="refitInterfaceType">Type of the Refit interface</typeparam>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddRefitClient(this IServiceCollection services, Type refitInterfaceType, RefitSettings settings = null)
        {
            return services.AddHttpClient(UniqueName.ForType(refitInterfaceType))
                            .ConfigureHttpMessageHandlerBuilder(builder =>
                            {
                                // check to see if user provided custom auth token
                                HttpMessageHandler innerHandler = null;
                                if (settings != null)
                                {
                                    if (settings.HttpMessageHandlerFactory != null)
                                    {
                                        innerHandler = settings.HttpMessageHandlerFactory();
                                    }

                                    if (settings.AuthorizationHeaderValueGetter != null)
                                    {
                                        innerHandler = new AuthenticatedHttpClientHandler(settings.AuthorizationHeaderValueGetter, innerHandler);
                                    }
                                    else if (settings.AuthorizationHeaderValueWithParamGetter != null)
                                    {
                                        innerHandler = new AuthenticatedParameterizedHttpClientHandler(settings.AuthorizationHeaderValueWithParamGetter, innerHandler);
                                    }
                                }

                                if (innerHandler != null)
                                {
                                    builder.PrimaryHandler = innerHandler;
                                }

                            })
                           .AddTypedClient(refitInterfaceType, (client, serviceProvider) => RestService.For(refitInterfaceType, client, settings));
        }

        private static IHttpClientBuilder AddTypedClient(this IHttpClientBuilder builder, Type type, Func<HttpClient, IServiceProvider, object> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddTransient(type, s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);

                return factory(httpClient, s);
            });

            return builder;
        }
    }
}
