using System.Collections.Concurrent;
using System.Net.Http;

namespace Refit
{
    public static class RestService
    {
        static readonly ConcurrentDictionary<Type, Type> TypeMapping = new();

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        public static T For<T>(HttpClient client, IRequestBuilder<T> builder)
        {
            return (T)For(typeof(T), client, builder);
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        public static T For<T>(HttpClient client, RefitSettings? settings)
        {
            var requestBuilder = RequestBuilder.ForType<T>(settings);

            return For(client, requestBuilder);
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        public static T For<T>(HttpClient client) => For<T>(client, (RefitSettings?)null);

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        public static T For<T>(string hostUrl, RefitSettings? settings)
        {
            var client = CreateHttpClient(hostUrl, settings);

            return For<T>(client, settings);
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
        public static T For<T>(string hostUrl) => For<T>(hostUrl, null);

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(
            Type refitInterfaceType,
            HttpClient client,
            IRequestBuilder builder
        )
        {
            var generatedType = TypeMapping.GetOrAdd(refitInterfaceType, GetGeneratedType);

            return Activator.CreateInstance(generatedType, client, builder)!;
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(
            Type refitInterfaceType,
            HttpClient client,
            RefitSettings? settings
        )
        {
            var requestBuilder = RequestBuilder.ForType(refitInterfaceType, settings);

            return For(refitInterfaceType, client, requestBuilder);
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(Type refitInterfaceType, HttpClient client) =>
            For(refitInterfaceType, client, (RefitSettings?)null);

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(Type refitInterfaceType, string hostUrl, RefitSettings? settings)
        {
            var client = CreateHttpClient(hostUrl, settings);

            return For(refitInterfaceType, client, settings);
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
        public static object For(Type refitInterfaceType, string hostUrl) =>
            For(refitInterfaceType, hostUrl, null);

        /// <summary>
        /// Create an <see cref="HttpClient"/> with <paramref name="hostUrl"/> as the base address.
        /// </summary>
        /// <param name="hostUrl">Base address.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>A <see cref="HttpClient"/> with the various parameters provided.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static HttpClient CreateHttpClient(string hostUrl, RefitSettings? settings)
        {
            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                throw new ArgumentException(
                    $"`{nameof(hostUrl)}` must not be null or whitespace.",
                    nameof(hostUrl)
                );
            }

            // check to see if user provided custom auth token
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

            return new HttpClient(innerHandler ?? new HttpClientHandler())
            {
                BaseAddress = new Uri(hostUrl.TrimEnd('/'))
            };
        }

        static Type GetGeneratedType(Type refitInterfaceType)
        {
            var typeName = UniqueName.ForType(refitInterfaceType);

            var generatedType = Type.GetType(typeName);

            if (generatedType == null)
            {
                var message =
                    refitInterfaceType.Name
                    + " doesn't look like a Refit interface. Make sure it has at least one "
                    + "method with a Refit HTTP method attribute and Refit is installed in the project.";

                throw new InvalidOperationException(message);
            }

            return generatedType;
        }
    }
}
