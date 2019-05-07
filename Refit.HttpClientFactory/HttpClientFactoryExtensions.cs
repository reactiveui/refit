using System;
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
                           .AddTypedClient((client, serviceProvider) => RestService.For(refitInterfaceType, client, settings));
        }
    }
}
