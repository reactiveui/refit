#if NETSTANDARD2_0
using Microsoft.Extensions.DependencyInjection;

namespace Refit
{
    public static class HttpClientFactoryExtensions
    {
        public static IHttpClientBuilder AddRefitClient<T>(this IServiceCollection services) where T : class
        {
            services.AddSingleton(provider => RequestBuilder.ForType<T>());

            return services.AddHttpClient(typeof(T).Name)
                           .AddTypedClient((client, serviceProvider) => RestService.For<T>(client, serviceProvider.GetService<IRequestBuilder<T>>()));
        }
    }
}
#endif
