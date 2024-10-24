using System.Collections;

namespace Refit.Generator;

internal static class ImmutableEquatableArray
{
    public static ImmutableEquatableArray<T> Empty<T>()
        where T : IEquatable<T> => ImmutableEquatableArray<T>.Empty;

    public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(
        this IEnumerable<T>? values
    )
        where T : IEquatable<T> => values == null ? Empty<T>() : new(values);
}

/// <summary>
/// Provides an immutable list implementation which implements sequence equality.
/// </summary>
internal sealed class ImmutableEquatableArray<T>
    : IEquatable<ImmutableEquatableArray<T>>,
        IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static ImmutableEquatableArray<T> Empty { get; } = new(Array.Empty<T>());

    private readonly T[] _values;
    public T this[int index] => _values[index];
    public int Count => _values.Length;

    public ImmutableEquatableArray(T[] values) => _values = values;

    public ImmutableEquatableArray(IEnumerable<T> values) => _values = values.ToArray();

    public T[] AsArray() => _values;

    public bool Equals(ImmutableEquatableArray<T>? other) =>
        other != null && ((ReadOnlySpan<T>)_values).SequenceEqual(other._values);

    public override bool Equals(object? obj) =>
        obj is ImmutableEquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (T value in _values)
        {
            hash = Combine(hash, value.GetHashCode());
        }

        static int Combine(int h1, int h2)
        {
            // RyuJIT optimizes this to use the ROL instruction
            // Related GitHub pull request: https://github.com/dotnet/coreclr/pull/1830
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }

        return hash;
    }

    public Enumerator GetEnumerator() => new(_values);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public struct Enumerator
    {
        private readonly T[] _values;
        private int _index;

        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        public bool MoveNext()
        {
            var newIndex = _index + 1;

            if ((uint)newIndex < (uint)_values.Length)
            {
                _index = newIndex;
                return true;
            }

            return false;
        }

        public readonly T Current => _values[_index];
    }
}
