// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Compares raw model arrays by their contents for incremental cache keys.</summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class ArraySequenceComparer<T> : IEqualityComparer<T[]>
{
    /// <summary>Gets the sequence comparer.</summary>
    internal static ArraySequenceComparer<T> Instance { get; } = new();

    /// <inheritdoc/>
    public bool Equals(
        T[]? x,
        T[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null || x.Length != y.Length)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < x.Length; i++)
        {
            if (!comparer.Equals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public int GetHashCode(
        T[] obj)
    {
        var hash = 0;
        for (var i = 0; i < obj.Length; i++)
        {
            hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(obj[i]));
        }

        return hash;
    }
}
