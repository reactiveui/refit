using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit
{
    interface IRestService
    {
        T For<T>(HttpClient client);
    }

    public static class RestService
    {
        static readonly ConcurrentDictionary<Type, Type> TypeMapping = new ConcurrentDictionary<Type, Type>();

        public static T For<T>(HttpClient client, IRequestBuilder<T> builder)
        {
            var generatedType = TypeMapping.GetOrAdd(typeof(T), GetGeneratedType<T>());

            return (T)Activator.CreateInstance(generatedType, client, builder);
        }

        public static T For<T>(HttpClient client, RefitSettings settings)
        {
            IRequestBuilder<T> requestBuilder = RequestBuilder.ForType<T>(settings);

            return For<T>(client, requestBuilder);
        }

        public static T For<T>(HttpClient client) => For<T>(client, (RefitSettings)null);

        public static T For<T>(string hostUrl, RefitSettings settings)
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
            }

            var client = new HttpClient(innerHandler ?? new HttpClientHandler()) { BaseAddress = new Uri(hostUrl?.TrimEnd('/')) };
            return For<T>(client, settings);
        }

        public static T For<T>(string hostUrl) => For<T>(hostUrl, null);

        static Type GetGeneratedType<T>()
        {
            string typeName = UniqueName.ForType<T>();

            var generatedType = Type.GetType(typeName);

            if (generatedType == null)
            {
                var message = typeof(T).Name + " doesn't look like a Refit interface. Make sure it has at least one " + "method with a Refit HTTP method attribute and Refit is installed in the project.";

                throw new InvalidOperationException(message);
            }

            return generatedType;
        }
    }
}
