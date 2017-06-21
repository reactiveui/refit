using System;

namespace Refit
{
    interface IRequestBuilderFactory
    {
        IRequestBuilder Create(Type interfaceType, RefitSettings settings);
    }

    class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder Create(Type interfaceType, RefitSettings settings = null)
        {
            return new RequestBuilderImplementation(interfaceType, settings);
        }
    }
}
