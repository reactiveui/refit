using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Refit
{
    sealed class AnonymousDisposable : IDisposable
    {
        readonly Action block;

        public AnonymousDisposable(Action block)
        {
            this.block = block;
        }

        public void Dispose()
        {
            block();
        }
    }
}
