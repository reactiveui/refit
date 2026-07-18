// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Buffers;
using System.Globalization;

namespace Refit.Generator;

/// <summary>A drop-in fluent string builder for the emitter that grows using <see cref="ArrayPool{T}"/> buffers.</summary>
/// <remarks>The emitter builds many transient fragment strings. Backing accumulation with a pooled <c>char[]</c> lets the
/// underlying buffer be reused across emissions instead of allocating fresh <see cref="System.Text.StringBuilder"/>
/// chunks each time. The final <see cref="ToString"/> returns the buffer to the pool, so an instance is single-use.</remarks>
internal sealed class PooledStringBuilder
{
    /// <summary>The default rented capacity, sized to hold a typical generated statement block without growing.</summary>
    private const int DefaultCapacity = 256;

    /// <summary>The buffer growth factor applied when the current backing array is exhausted.</summary>
    private const int GrowthFactor = 2;

    /// <summary>The line terminator emitted by the <see cref="AppendLine()"/> overloads.</summary>
    /// <remarks>Fixed to <c>\n</c> for deterministic generated output (matching the emitter's explicit <c>\n</c>
    /// literals); analyzers are banned from reading <c>Environment.NewLine</c>.</remarks>
    private const string NewLine = "\n";

    /// <summary>The pooled array currently backing the builder.</summary>
    private char[] _buffer;

    /// <summary>The current write position within the buffer.</summary>
    private int _pos;

    /// <summary>Initializes a new instance of the <see cref="PooledStringBuilder"/> class.</summary>
    public PooledStringBuilder()
        : this(DefaultCapacity)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PooledStringBuilder"/> class with an initial capacity.</summary>
    /// <param name="capacity">The initial buffer capacity to rent.</param>
    public PooledStringBuilder(int capacity) => _buffer = ArrayPool<char>.Shared.Rent(Math.Max(capacity, DefaultCapacity));

    /// <summary>Appends a string.</summary>
    /// <param name="value">The string to append, or null.</param>
    /// <returns>This builder, for chaining.</returns>
    public PooledStringBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        EnsureCapacity(_pos + value!.Length);
        value.CopyTo(0, _buffer, _pos, value.Length);
        _pos += value.Length;
        return this;
    }

    /// <summary>Appends the invariant decimal rendering of a 32-bit integer.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>This builder, for chaining.</returns>
    public PooledStringBuilder Append(int value) => Append(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Appends a single character.</summary>
    /// <param name="value">The character to append.</param>
    /// <returns>This builder, for chaining.</returns>
    public PooledStringBuilder Append(char value)
    {
        EnsureCapacity(_pos + 1);
        _buffer[_pos] = value;
        _pos++;
        return this;
    }

    /// <summary>Appends a string followed by a line terminator.</summary>
    /// <param name="value">The string to append, or null.</param>
    /// <returns>This builder, for chaining.</returns>
    public PooledStringBuilder AppendLine(string? value) => Append(value).Append(NewLine);

    /// <summary>Appends a line terminator.</summary>
    /// <returns>This builder, for chaining.</returns>
    public PooledStringBuilder AppendLine() => Append(NewLine);

    /// <summary>Materializes the accumulated content into a string and returns the pooled buffer.</summary>
    /// <returns>The accumulated string.</returns>
    public override string ToString()
    {
        var result = _pos == 0 ? string.Empty : new string(_buffer, 0, _pos);
        var toReturn = _buffer;
        _buffer = [];
        _pos = 0;
        ArrayPool<char>.Shared.Return(toReturn);
        return result;
    }

    /// <summary>Ensures the backing buffer can hold at least the requested number of characters.</summary>
    /// <param name="required">The required total capacity.</param>
    internal void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
        {
            return;
        }

        var next = ArrayPool<char>.Shared.Rent(Math.Max(required, _buffer.Length * GrowthFactor));
        Array.Copy(_buffer, next, _pos);
        var toReturn = _buffer;
        _buffer = next;
        ArrayPool<char>.Shared.Return(toReturn);
    }
}
