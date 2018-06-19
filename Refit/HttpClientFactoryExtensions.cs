#if NETSTANDARD2_0
using Microsoft.Extensions.DependencyInjection;

namespace Refit
{
    public static class HttpClientFactoryExtensions
    {
        public static IHttpClientBuilder AddRefitClient<T>(this IServiceCollection services, RefitSettings settings = null) where T : class
        {
            services.AddSingleton(provider => RequestBuilder.ForType<T>(settings));

            return services.AddHttpClient(typeof(T).Name)
                           .AddTypedClient((client, serviceProvider) => RestService.For<T>(client, serviceProvider.GetService<IRequestBuilder<T>>()));
        }
    }
}
#endif
