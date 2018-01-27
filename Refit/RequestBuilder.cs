using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit
{
    public interface IRequestBuilder
    {
        Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null);
    }

    public static class RequestBuilder
    {
        static readonly IRequestBuilderFactory platformRequestBuilderFactory = new RequestBuilderFactory();

        public static IRequestBuilder ForType(Type interfaceType, RefitSettings settings)
        {
            return platformRequestBuilderFactory.Create(interfaceType, settings);
        }

        public static IRequestBuilder ForType(Type interfaceType)
        {
            return platformRequestBuilderFactory.Create(interfaceType, null);
        }

        public static IRequestBuilder ForType<T>(RefitSettings settings)
        {
            return ForType(typeof(T), settings);
        }

        public static IRequestBuilder ForType<T>()
        {
            return ForType(typeof(T), null);
        }
    }

#if NETSTANDARD1_1
    class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder Create(Type interfaceType, RefitSettings settings = null)
        {
            throw new NotImplementedException("You've somehow included the PCL version of Refit in your app. You need to use the platform-specific version!");
        }
    }
#endif
}
