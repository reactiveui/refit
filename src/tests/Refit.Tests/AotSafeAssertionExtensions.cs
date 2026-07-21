// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace Refit.Tests;

/// <summary>AOT-safe collection-equality helpers.</summary>
internal static class AotSafeAssertionExtensions
{
    /// <summary>Provides collection-equality assertions for assertion sources.</summary>
    /// <param name="source">The assertion source.</param>
    /// <typeparam name="TCollection">The collection type being asserted.</typeparam>
    /// <typeparam name="TItem">The element type.</typeparam>
    extension<TCollection, TItem>(IAssertionSource<TCollection> source)
        where TCollection : IEnumerable<TItem>
    {
        /// <summary>
        /// Asserts the collection is equivalent to <paramref name="expected"/>
        /// using the element type's default <see cref="EqualityComparer{T}"/>
        /// (mirroring <c>IsEquivalentTo</c>) without the reflection-based structural
        /// comparison that triggers trim/AOT warnings.
        /// </summary>
        /// <param name="expected">The expected element sequence.</param>
        /// <returns>The chained collection-equivalency assertion.</returns>
        internal IsEquivalentToAssertion<TCollection, TItem> IsCollectionEqualTo(IEnumerable<TItem> expected) =>
            source.IsEquivalentTo(expected, EqualityComparer<TItem>.Default);
    }
}
