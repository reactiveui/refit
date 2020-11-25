using System;
using System.Net.Http;

namespace Refit
{
    public interface IRequestBuilder
    {
        Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(string methodName, Type[]? parameterTypes = null, Type[]? genericArgumentTypes = null);
    }

    public interface IRequestBuilder<T> : IRequestBuilder
    {
    }

    public static class RequestBuilder
    {
        static readonly IRequestBuilderFactory PlatformRequestBuilderFactory = new RequestBuilderFactory();

        public static IRequestBuilder<T> ForType<T>(RefitSettings? settings)
        {
            return PlatformRequestBuilderFactory.Create<T>(settings);
        }

        public static IRequestBuilder<T> ForType<T>()
        {
            return PlatformRequestBuilderFactory.Create<T>(null);
        }

        public static IRequestBuilder ForType(Type refitInterfaceType, RefitSettings? settings)
        {
            return PlatformRequestBuilderFactory.Create(refitInterfaceType, settings);
        }

        public static IRequestBuilder ForType(Type refitInterfaceType)
        {
            return PlatformRequestBuilderFactory.Create(refitInterfaceType, null);
        }
    }
}
