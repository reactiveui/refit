using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return new CachedRequestBuilderImplementation(new RequestBuilderImplementation(interfaceType, settings));
        }
    }
}
