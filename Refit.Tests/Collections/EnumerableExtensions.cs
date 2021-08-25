using System.Collections.Generic;

namespace Refit.Tests.Collections
{
    public static class EnumerableExtensions
    {
        public static EnumerableExtended<T> Extend<T>(this IEnumerable<T> enumerable, IDictionary<string, string> parameters)
        {
            return new EnumerableExtended<T>(enumerable, parameters);
        }
    }
}
