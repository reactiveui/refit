// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Provides an immutable list implementation which implements sequence equality.</summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class ImmutableEquatableArray<T>
    : IEquatable<ImmutableEquatableArray<T>>,
        IReadOnlyList<T>
    where T : IEquatable<T>
{
    /// <summary>The backing array of values.</summary>
    private readonly T[] _values;

    /// <summary>Initializes a new instance of the <see cref="ImmutableEquatableArray{T}"/> class.</summary>
    /// <param name="values">The array to wrap.</param>
    public ImmutableEquatableArray(T[] values) => _values = values;

    /// <summary>Gets a shared empty array instance.</summary>
    public static ImmutableEquatableArray<T> Empty { get; } = new([]);

    /// <summary>Gets the number of elements in the array.</summary>
    public int Count => _values.Length;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The element at the given index.</returns>
    public T this[int index] => _values[index];

    /// <summary>Returns the underlying array.</summary>
    /// <returns>The backing array of values.</returns>
    public T[] AsArray() => _values;

    /// <inheritdoc/>
    public bool Equals(ImmutableEquatableArray<T>? other)
    {
        if (other is null || other._values.Length != _values.Length)
        {
            return false;
        }

        for (var i = 0; i < _values.Length; i++)
        {
            if (!_values[i].Equals(other._values[i]))
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
        var hash = 0;
        for (var i = 0; i < _values.Length; i++)
        {
            hash = Combine(hash, _values[i].GetHashCode());
        }

        return hash;
    }

    /// <summary>Returns an allocation-free enumerator over the array.</summary>
    /// <returns>An enumerator for the array.</returns>
    public Enumerator GetEnumerator() => new(_values);

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

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

    /// <summary>A struct enumerator that iterates the backing array without allocation.</summary>
    [SuppressMessage("Style", "SST1803:Type can be made readonly", Justification = "Mutable iterator state (_index); cannot be readonly.")]
    public record struct Enumerator
    {
        /// <summary>The array being enumerated.</summary>
        private readonly T[] _values;

        /// <summary>The current zero-based position within the array.</summary>
        private int _index;

        /// <summary>Initializes a new instance of the <see cref="Enumerator"/> struct.</summary>
        /// <param name="values">The array to enumerate.</param>
        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        /// <summary>Gets the element at the current position.</summary>
        public readonly T Current => _values[_index];

        /// <summary>Advances the enumerator to the next element.</summary>
        /// <returns><see langword="true"/> if there is another element; otherwise <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            var newIndex = _index + 1;

            if ((uint)newIndex >= (uint)_values.Length)
            {
                return false;
            }

            _index = newIndex;
            return true;
        }
    }
}
