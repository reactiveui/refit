// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Provides an immutable list implementation which implements sequence equality.</summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>A <see langword="readonly struct"/> so the value sits inline in its owning model instead of adding a
/// separate wrapper heap object per parsed collection. It still carries sequence value-equality (the incremental
/// cache key) and an allocation-free struct enumerator; only the explicit <see cref="IEnumerable{T}"/> path boxes.</remarks>
internal readonly partial struct ImmutableEquatableArray<T>
    where T : IEquatable<T>
{
    /// <summary>The backing array of values, or null for a defaulted value (treated as empty).</summary>
    private readonly T[]? _values;

    /// <summary>Initializes a new instance of the <see cref="ImmutableEquatableArray{T}"/> struct.</summary>
    /// <param name="values">The array to wrap.</param>
    internal ImmutableEquatableArray(T[] values) => _values = values;

    /// <summary>Gets a shared empty array instance.</summary>
    internal static ImmutableEquatableArray<T> Empty => default;

    /// <summary>Returns the underlying array.</summary>
    /// <returns>The backing array of values.</returns>
    internal T[] AsArray() => _values ?? [];

    /// <summary>Combines two hash codes into one.</summary>
    /// <param name="h1">The accumulated hash code.</param>
    /// <param name="h2">The next hash code to fold in.</param>
    /// <returns>The combined hash code.</returns>
    private static int Combine(int h1, int h2)
    {
        // RyuJIT optimizes this to use the ROL instruction
        // Related GitHub pull request: https://github.com/dotnet/coreclr/pull/1830
        var rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)rol5 + h1) ^ h2;
    }
}
