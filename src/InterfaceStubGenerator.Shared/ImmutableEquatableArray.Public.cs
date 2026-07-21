// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;

namespace Refit.Generator;

/// <summary>Provides the public collection and equality contracts for an immutable equatable array.</summary>
internal readonly partial struct ImmutableEquatableArray<T>
    : IEquatable<ImmutableEquatableArray<T>>,
        IReadOnlyList<T>,
        IImmutableEquatableArrayEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>Gets the number of elements in the array.</summary>
    public int Count => _values?.Length ?? 0;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The element at the given index.</returns>
    public T this[int index] => _values![index];

    /// <inheritdoc/>
    public bool Equals(ImmutableEquatableArray<T> other)
    {
        var values = _values;
        var otherValues = other._values;
        var length = values?.Length ?? 0;
        if (length != (otherValues?.Length ?? 0))
        {
            return false;
        }

        for (var i = 0; i < length; i++)
        {
            if (!values![i].Equals(otherValues![i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is ImmutableEquatableArray<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var values = _values;
        var hash = 0;
        if (values is not null)
        {
            for (var i = 0; i < values.Length; i++)
            {
                hash = Combine(hash, values[i].GetHashCode());
            }
        }

        return hash;
    }

    /// <inheritdoc/>
    public ReadOnlySpan<T>.Enumerator GetEnumerator() => new ReadOnlySpan<T>(_values ?? []).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)(_values ?? [])).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => (_values ?? []).GetEnumerator();
}
