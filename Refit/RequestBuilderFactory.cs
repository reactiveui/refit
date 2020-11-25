using System;

namespace Refit
{
    interface IRequestBuilderFactory
    {
        IRequestBuilder<T> Create<T>(RefitSettings? settings);
        IRequestBuilder Create(Type refitInterfaceType, RefitSettings? settings);
    }

    class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder<T> Create<T>(RefitSettings? settings = null)
        {
            return new CachedRequestBuilderImplementation<T>(new RequestBuilderImplementation<T>(settings));
        }

        public IRequestBuilder Create(Type refitInterfaceType, RefitSettings? settings = null)
        {
            return new CachedRequestBuilderImplementation(new RequestBuilderImplementation(refitInterfaceType, settings));
        }
    }
}
