using System;
using System.Collections.Generic;

namespace Refit
{
    public static class EnumerableEx
    {
        public static IEnumerable<T> Return<T>(T value)
        {
            yield return value;
        }
    }
}

