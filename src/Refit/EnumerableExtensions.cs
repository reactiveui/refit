// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Extension helpers for working with enumerables.</summary>
internal static class EnumerableExtensions
{
    /// <summary>Provides peek helpers for an <see cref="IEnumerable{T}"/> sequence.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="enumerable">The sequence to inspect.</param>
    extension<T>(IEnumerable<T> enumerable)
    {
        /// <summary>Tries to get the single element of the sequence.</summary>
        /// <param name="value">When the sequence has a single element, receives that element; otherwise the default.</param>
        /// <returns>A value indicating whether the sequence was empty, single, or had many elements.</returns>
        internal EnumerablePeek TryGetSingle(out T? value)
        {
            value = default;
            using var enumerator = enumerable.GetEnumerator();
            var hasFirst = enumerator.MoveNext();
            if (!hasFirst)
            {
                return EnumerablePeek.Empty;
            }

            value = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return EnumerablePeek.Single;
            }

            value = default;
            return EnumerablePeek.Many;
        }
    }
}
