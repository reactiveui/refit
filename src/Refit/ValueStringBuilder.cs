// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Refit;

// From https://github/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs
/// <summary>A stack-allocated string builder that grows using pooled buffers.</summary>
public ref struct ValueStringBuilder
{
    /// <summary>The pooled array currently backing the builder, if one has been rented.</summary>
    private char[]? _arrayToReturnToPool;

    /// <summary>The span the builder is writing into.</summary>
    private Span<char> _chars;

    /// <summary>The current write position within the builder.</summary>
    private int _pos;

    /// <summary>Initializes a new instance of the <c>ValueStringBuilder</c> struct using an initial stack buffer.</summary>
    /// <param name="initialBuffer">The initial buffer to write into.</param>
    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    /// <summary>Initializes a new instance of the <c>ValueStringBuilder</c> struct using a pooled buffer of the given capacity.</summary>
    /// <param name="initialCapacity">The initial capacity to rent.</param>
    public ValueStringBuilder(int initialCapacity)
    {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    /// <summary>Gets or sets the number of characters currently in the builder.</summary>
    public int Length
    {
        readonly get => _pos;
        set
        {
            Debug.Assert(value >= 0, "Length must not be negative.");
            Debug.Assert(value <= _chars.Length, "Length must not exceed the buffer capacity.");
            _pos = value;
        }
    }

    /// <summary>Gets the total capacity of the current buffer.</summary>
    public readonly int Capacity => _chars.Length;

    /// <summary>Gets the underlying storage of the builder.</summary>
    public readonly Span<char> RawChars => _chars;

    /// <summary>Gets a reference to the character at the specified index.</summary>
    /// <param name="index">The zero-based index of the character.</param>
    /// <returns>A reference to the character at the index.</returns>
    public ref char this[int index]
    {
        get
        {
            Debug.Assert(index < _pos, "Index must be within the current length.");
            return ref _chars[index];
        }
    }

    /// <summary>Ensures the builder can hold at least the requested number of characters.</summary>
    /// <param name="capacity">The required capacity.</param>
    public void EnsureCapacity(int capacity)
    {
        // This is not expected to be called this with negative capacity
        Debug.Assert(capacity >= 0, "Capacity must not be negative.");

        // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
        if ((uint)capacity <= (uint)_chars.Length)
        {
            return;
        }

        Grow(capacity - _pos);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>.
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)".
    /// </summary>
    /// <returns>A reference to the first character of the builder.</returns>
    public readonly ref char GetPinnableReference() => ref MemoryMarshal.GetReference(_chars);

    /// <summary>Get a pinnable reference to the builder.</summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/>.</param>
    /// <returns>A reference to the first character of the builder.</returns>
    public ref char GetPinnableReference(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }

        return ref MemoryMarshal.GetReference(_chars);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var s = _chars[.._pos].ToString();
        Dispose();
        return s;
    }

    /// <summary>Returns a span around the contents of the builder.</summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/>.</param>
    /// <returns>A span over the contents of the builder.</returns>
    public ReadOnlySpan<char> AsSpan(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }

        return _chars[.._pos];
    }

    /// <summary>Returns a span over the current contents of the builder.</summary>
    /// <returns>A span over the contents of the builder.</returns>
    public readonly ReadOnlySpan<char> AsSpan() => _chars[.._pos];

    /// <summary>Returns a span over the contents from the given start index to the end.</summary>
    /// <param name="start">The start index.</param>
    /// <returns>A span over the requested portion of the builder.</returns>
    public readonly ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);

    /// <summary>Returns a span over the contents at the given start index with the given length.</summary>
    /// <param name="start">The start index.</param>
    /// <param name="length">The length of the span.</param>
    /// <returns>A span over the requested portion of the builder.</returns>
    public readonly ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    /// <summary>Attempts to copy the contents of the builder into the destination span.</summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters copied.</param>
    /// <returns><see langword="true"/> if the contents were copied; otherwise, <see langword="false"/>.</returns>
    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (_chars[.._pos].TryCopyTo(destination))
        {
            charsWritten = _pos;
            Dispose();
            return true;
        }

        charsWritten = 0;
        Dispose();
        return false;
    }

    /// <summary>Inserts a character repeated a number of times at the given index.</summary>
    /// <param name="index">The index at which to insert.</param>
    /// <param name="value">The character to insert.</param>
    /// <param name="count">The number of times to insert the character.</param>
    public void Insert(int index, char value, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        var remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        _chars.Slice(index, count).Fill(value);
        _pos += count;
    }

    /// <summary>Inserts a string at the given index.</summary>
    /// <param name="index">The index at which to insert.</param>
    /// <param name="s">The string to insert.</param>
    public void Insert(int index, string? s)
    {
        if (s is null)
        {
            return;
        }

        var count = s.Length;

        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        var remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        s
#if !NETCOREAPP
            .AsSpan()
#endif
            .CopyTo(_chars[index..]);
        _pos += count;
    }

    /// <summary>Appends a single character to the builder.</summary>
    /// <param name="c">The character to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        var pos = _pos;
        var chars = _chars;
        if ((uint)pos < (uint)chars.Length)
        {
            chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    /// <summary>Appends a string to the builder.</summary>
    /// <param name="s">The string to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? s)
    {
        if (s is null)
        {
            return;
        }

        var pos = _pos;
        if (s.Length == 1 &&
            (uint)pos < (uint)_chars
                .Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendSlow(s);
        }
    }

    /// <summary>Appends a character repeated a number of times.</summary>
    /// <param name="c">The character to append.</param>
    /// <param name="count">The number of times to append the character.</param>
    public void Append(char c, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        var dst = _chars.Slice(_pos, count);
        for (var i = 0; i < dst.Length; i++)
        {
            dst[i] = c;
        }

        _pos += count;
    }

    /// <summary>Appends the characters of a span to the builder.</summary>
    /// <param name="value">The characters to append.</param>
    public void Append(ReadOnlySpan<char> value)
    {
        var pos = _pos;
        if (pos > _chars.Length - value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    /// <summary>Reserves and returns a span of the given length at the end of the builder.</summary>
    /// <param name="length">The number of characters to reserve.</param>
    /// <returns>A writable span for the reserved characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        var origPos = _pos;
        if (origPos > _chars.Length - length)
        {
            Grow(length);
        }

        _pos = origPos + length;
        return _chars.Slice(origPos, length);
    }

    /// <summary>Returns any rented buffer to the pool and resets the builder.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        var toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn is null)
        {
            return;
        }

        ArrayPool<char>.Shared.Return(toReturn);
    }

    /// <summary>Appends a string using the slow path that may grow the buffer.</summary>
    /// <param name="s">The string to append.</param>
    private void AppendSlow(string s)
    {
        var pos = _pos;
        if (pos > _chars.Length - s.Length)
        {
            Grow(s.Length);
        }

        s
#if !NETCOREAPP
            .AsSpan()
#endif
            .CopyTo(_chars[pos..]);
        _pos += s.Length;
    }

    /// <summary>Grows the buffer and then appends the character.</summary>
    /// <param name="c">The character to append.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    /// <summary>Resize the internal buffer either by doubling current buffer size or by adding <paramref name="additionalCapacityBeyondPos"/> to <see cref="_pos"/> whichever is greater.</summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        Debug.Assert(additionalCapacityBeyondPos > 0, "Grow must be called with a positive additional capacity.");
        Debug.Assert(
            _pos > _chars.Length - additionalCapacityBeyondPos,
            "Grow called incorrectly, no resize is needed.");

        const uint ArrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

        // Increase to at least the required size (_pos + additionalCapacityBeyondPos), but try
        // to double the size if possible, bounding the doubling to not go beyond the max array length.
        var newCapacity = (int)Math.Max(
            (uint)(_pos + additionalCapacityBeyondPos),
            Math.Min((uint)_chars.Length * 2, ArrayMaxLength));

        // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative.
        // This could also go negative if the actual required length wraps around.
        var poolArray = ArrayPool<char>.Shared.Rent(newCapacity);

        _chars[.._pos].CopyTo(poolArray);

        var toReturn = _arrayToReturnToPool;
        _arrayToReturnToPool = poolArray;
        _chars = poolArray;
        if (toReturn is null)
        {
            return;
        }

        ArrayPool<char>.Shared.Return(toReturn);
    }
}
