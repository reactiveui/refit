// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Defines the allocation-free enumeration pattern used by immutable equatable arrays.</summary>
/// <typeparam name="T">The element type.</typeparam>
internal interface IImmutableEquatableArrayEnumerable<T>
{
    /// <summary>Returns a span enumerator for the underlying values.</summary>
    /// <returns>An allocation-free span enumerator.</returns>
    ReadOnlySpan<T>.Enumerator GetEnumerator();
}
