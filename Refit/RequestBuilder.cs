using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Refit
{
    public interface IRequestBuilder
    {
        IEnumerable<string> InterfaceHttpMethods { get; }
        Func<object[], HttpRequestMessage> BuildRequestFactoryForMethod(string methodName, string basePath = "");
        Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName);
    }

    interface IRequestBuilderFactory
    {
        IRequestBuilder Create(Type interfaceType, IRequestParameterFormatter requestParameterFormatter);
    }

    public static class RequestBuilder
    {
        static readonly IRequestBuilderFactory platformRequestBuilderFactory = new RequestBuilderFactory();
        
        public static IRequestBuilder ForType(Type interfaceType, IRequestParameterFormatter requestParameterFormatter = null)
        {
            return platformRequestBuilderFactory.Create(interfaceType, requestParameterFormatter);
        }

        public static IRequestBuilder ForType<T>(IRequestParameterFormatter requestParameterFormatter = null)
        {
            return ForType(typeof(T), requestParameterFormatter);
        }
    }

#if PORTABLE
    class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder Create(Type interfaceType, IRequestParameterFormatter requestParameterFormatter = null)
        {
            throw new NotImplementedException("You've somehow included the PCL version of Refit in your app. You need to use the platform-specific version!");
        }
    }
#endif
}

