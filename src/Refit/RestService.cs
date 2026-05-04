using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.Http;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Refit
{
    /// <summary>
    /// RestService.
    /// </summary>
    public static class RestService
    {
        static readonly ConcurrentDictionary<Type, Type> TypeMapping = new();
        static readonly ConcurrentDictionary<Type, Func<HttpClient, IRequestBuilder, object>> GeneratedFactories = new();

        /// <summary>
        /// Registers a source-generated Refit implementation factory.
        /// </summary>
        /// <param name="refitInterfaceType">The Refit interface type.</param>
        /// <param name="factory">The generated implementation factory.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RegisterGeneratedFactory(
            Type refitInterfaceType,
            Func<HttpClient, IRequestBuilder, object> factory
        )
        {
            if (refitInterfaceType is null)
            {
                throw new ArgumentNullException(nameof(refitInterfaceType));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            GeneratedFactories[refitInterfaceType] = factory;
        }

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
#if NET8_0_OR_GREATER
        public static T For<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T>(HttpClient client, IRequestBuilder<T> builder) => (T)For(typeof(T), client, builder);
#else
        public static T For<T>(HttpClient client, IRequestBuilder<T> builder) => (T)For(typeof(T), client, builder);
#endif

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
#if NET8_0_OR_GREATER
        public static T For<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T>(HttpClient client, RefitSettings? settings)
#else
        public static T For<T>(HttpClient client, RefitSettings? settings)
#endif
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
#if NET8_0_OR_GREATER
        public static T For<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T>(HttpClient client) => For<T>(client, (RefitSettings?)null);
#else
        public static T For<T>(HttpClient client) => For<T>(client, (RefitSettings?)null);
#endif

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <typeparam name="T">Interface to create the implementation for.</typeparam>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <typeparamref name="T"/>.</returns>
#if NET8_0_OR_GREATER
        public static T For<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T>(string hostUrl, RefitSettings? settings)
#else
        public static T For<T>(string hostUrl, RefitSettings? settings)
#endif
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
#if NET8_0_OR_GREATER
        public static T For<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T>(string hostUrl) => For<T>(hostUrl, null);
#else
        public static T For<T>(string hostUrl) => For<T>(hostUrl, null);
#endif

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="client">The <see cref="HttpClient"/> the implementation will use to send requests.</param>
        /// <param name="builder"><see cref="IRequestBuilder"/> to use to build requests.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
#if NET8_0_OR_GREATER
        public static object For(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType,
            HttpClient client,
            IRequestBuilder builder
        )
#else
        public static object For(
            Type refitInterfaceType,
            HttpClient client,
            IRequestBuilder builder
        )
#endif
        {
            if (GeneratedFactories.TryGetValue(refitInterfaceType, out var factory))
            {
                return factory(client, builder);
            }

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
#if NET8_0_OR_GREATER
        public static object For(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType,
            HttpClient client,
            RefitSettings? settings
        )
#else
        public static object For(
            Type refitInterfaceType,
            HttpClient client,
            RefitSettings? settings
        )
#endif
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
#if NET8_0_OR_GREATER
        public static object For(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType,
            HttpClient client
        ) => For(refitInterfaceType, client, (RefitSettings?)null);
#else
        public static object For(Type refitInterfaceType, HttpClient client) =>
            For(refitInterfaceType, client, (RefitSettings?)null);
#endif

        /// <summary>
        /// Generate a Refit implementation of the specified interface.
        /// </summary>
        /// <param name="refitInterfaceType">Interface to create the implementation for.</param>
        /// <param name="hostUrl">Base address the implementation will use.</param>
        /// <param name="settings"><see cref="RefitSettings"/> to use to configure the HttpClient.</param>
        /// <returns>An instance that implements <paramref name="refitInterfaceType"/>.</returns>
#if NET8_0_OR_GREATER
        public static object For(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType,
            string hostUrl,
            RefitSettings? settings
        )
#else
        public static object For(Type refitInterfaceType, string hostUrl, RefitSettings? settings)
#endif
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
#if NET8_0_OR_GREATER
        public static object For(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType,
            string hostUrl
        ) => For(refitInterfaceType, hostUrl, null);
#else
        public static object For(Type refitInterfaceType, string hostUrl) =>
            For(refitInterfaceType, hostUrl, null);
#endif

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

#if NET8_0_OR_GREATER
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        static Type GetGeneratedType(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType
        )
#else
        static Type GetGeneratedType(Type refitInterfaceType)
#endif
        {
            var typeName = UniqueName.ForType(refitInterfaceType);

            var generatedType = Type.GetType(typeName, throwOnError: false);

            if (generatedType == null)
            {
                var message =
                    refitInterfaceType.Name
                    + " doesn't look like a Refit interface. Make sure it has at least one "
                    + "method with a Refit HTTP method attribute, the Refit source generator is installed in the project, "
                    + "and your build produced the generated client. For Native AOT or trimmed apps, prefer generated clients "
                    + "plus source-generated System.Text.Json metadata.";

                throw new InvalidOperationException(message);
            }

            return generatedType;
        }
    }
}
