using System.Collections;
using System.Collections.Generic;

namespace Refit.Tests.Collections
{
    public class EnumerableExtended<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> items;

        public EnumerableExtended(IEnumerable<T> items, IDictionary<string, string> parameters)
        {
            this.items = items;
            this.Parameters = parameters;
        }

        public IDictionary<string, string> Parameters { get; }

        public IEnumerator<T> GetEnumerator() => this.items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();
    }
}
