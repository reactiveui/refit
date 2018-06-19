using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Refit
{
    interface IRequestBuilderFactory
    {
        IRequestBuilder<T> Create<T>(RefitSettings settings);
    }

    class RequestBuilderFactory : IRequestBuilderFactory
    {
        public IRequestBuilder<T> Create<T>(RefitSettings settings = null)
        {
            return new CachedRequestBuilderImplementation<T>(new RequestBuilderImplementation<T>(settings));
        }
    }
}
