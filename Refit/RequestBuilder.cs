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
        public static Func<Type, IRequestParameterFormatter> FormatterFactory = type => new DefaultRequestParameterFormatter(type);
        static readonly IRequestBuilderFactory platformRequestBuilderFactory = new RequestBuilderFactory();
        
        public static IRequestBuilder ForType(Type interfaceType)
        {
            return platformRequestBuilderFactory.Create(interfaceType, FormatterFactory(interfaceType));
        }

        public static IRequestBuilder ForType<T>()
        {
            return ForType(typeof(T));
        }
    }

#if PORTABLE
    class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder Create(Type interfaceType, IRequestParameterFormatter requestParameterFormatter)
        {
            throw new NotImplementedException("You've somehow included the PCL version of Refit in your app. You need to use the platform-specific version!");
        }
    }
#endif
}

