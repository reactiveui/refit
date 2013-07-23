using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Refit
{
    public interface IRequestBuilder
    {
        IEnumerable<string> InterfaceHttpMethods { get; }
        Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(string methodName);
        Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName);
    }

    interface IRequestBuilderFactory
    {
        IRequestBuilder Create(Type interfaceType);
    }

    public static class RequestBuilder
    {
#if PORTABLE
        static IRequestBuilderFactory platformRequestBuilderFactory;

        static RequestBuilder()
        {
            var fullName = typeof(RequestBuilder).AssemblyQualifiedName;
            var toFind = fullName.Replace("RequestBuilder", "RequestBuilderFactory").Replace("Refit-Portable", "Refit");

            var type = Type.GetType(toFind);
            platformRequestBuilderFactory = Activator.CreateInstance(type) as IRequestBuilderFactory;
        }
#else
        static readonly IRequestBuilderFactory platformRequestBuilderFactory = new RequestBuilderFactory();
#endif
        public static IRequestBuilder ForType(Type interfaceType)
        {
            return platformRequestBuilderFactory.Create(interfaceType);
        }

        public static IRequestBuilder ForType<T>()
        {
            return ForType(typeof(T));
        }
    }
}

