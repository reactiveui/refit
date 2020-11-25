using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace Refit
{
    public static class RestService
    {
        static readonly ConcurrentDictionary<Type, Type> TypeMapping = new();

        public static T For<T>(HttpClient client, IRequestBuilder<T> builder)
        {
            return (T)For(typeof(T), client, builder);            
        }

        public static T For<T>(HttpClient client, RefitSettings? settings)
        {
            var requestBuilder = RequestBuilder.ForType<T>(settings);

            return For(client, requestBuilder);
        }

        public static T For<T>(HttpClient client) => For<T>(client, (RefitSettings?)null);

        public static T For<T>(string hostUrl, RefitSettings? settings)
        {
            var client = CreateHttpClient(hostUrl, settings);

            return For<T>(client, settings);
        }

        public static T For<T>(string hostUrl) => For<T>(hostUrl, null);

        public static object For(Type refitInterfaceType, HttpClient client, IRequestBuilder builder)
        {
            var generatedType = TypeMapping.GetOrAdd(refitInterfaceType, GetGeneratedType(refitInterfaceType));

            return Activator.CreateInstance(generatedType, client, builder)!;
        }

        public static object For(Type refitInterfaceType, HttpClient client, RefitSettings? settings)
        {
            var requestBuilder = RequestBuilder.ForType(refitInterfaceType, settings);

            return For(refitInterfaceType, client, requestBuilder);
        }

        public static object For(Type refitInterfaceType, HttpClient client) => For(refitInterfaceType, client, (RefitSettings?)null);

        public static object For(Type refitInterfaceType, string hostUrl, RefitSettings? settings)
        {
            var client = CreateHttpClient(hostUrl, settings);
            
            return For(refitInterfaceType, client, settings);
        }

        public static object For(Type refitInterfaceType, string hostUrl) => For(refitInterfaceType, hostUrl, null);             

        public static HttpClient CreateHttpClient(string hostUrl, RefitSettings? settings)
        {
            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                throw new ArgumentException(
                    $"`{nameof(hostUrl)}` must not be null or whitespace.",
                    nameof(hostUrl));
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
                    innerHandler = new AuthenticatedHttpClientHandler(settings.AuthorizationHeaderValueGetter, innerHandler);
                }
                else if (settings.AuthorizationHeaderValueWithParamGetter != null)
                {
                    innerHandler = new AuthenticatedParameterizedHttpClientHandler(settings.AuthorizationHeaderValueWithParamGetter, innerHandler);
                }
            }

            return new HttpClient(innerHandler ?? new HttpClientHandler()) { BaseAddress = new Uri(hostUrl.TrimEnd('/')) };
        }

        static Type GetGeneratedType(Type refitInterfaceType)
        {
            var typeName = UniqueName.ForType(refitInterfaceType);

            var generatedType = Type.GetType(typeName);

            if (generatedType == null)
            {
                var message = refitInterfaceType.Name + " doesn't look like a Refit interface. Make sure it has at least one " + "method with a Refit HTTP method attribute and Refit is installed in the project.";

                throw new InvalidOperationException(message);
            }

            return generatedType;
        }
    }
}
