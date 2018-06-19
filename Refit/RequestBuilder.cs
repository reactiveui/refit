using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit
{
    public interface IRequestBuilder
    {
        Func<HttpClient, object[], object> BuildRestResultFuncForMethod(string methodName, Type[] parameterTypes = null, Type[] genericArgumentTypes = null);
    }

    public interface IRequestBuilder<T> : IRequestBuilder
    {
    }

    public static class RequestBuilder
    {
        static readonly IRequestBuilderFactory platformRequestBuilderFactory = new RequestBuilderFactory();

        public static IRequestBuilder<T> ForType<T>(RefitSettings settings)
        {
            return platformRequestBuilderFactory.Create<T>(settings);
        }

        public static IRequestBuilder<T> ForType<T>()
        {
            return platformRequestBuilderFactory.Create<T>(null);
        }
    }
}
