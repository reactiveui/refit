// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NETFRAMEWORK
namespace System;

/// <summary>Minimal polyfill for <c>System.Index</c> so C# index/range syntax compiles on .NET Framework targets.</summary>
/// <remarks>
/// This is a compiler hook for older target frameworks. It intentionally mirrors the BCL member names used
/// by range lowering and should not grow into a general-purpose replacement for the framework type.
/// </remarks>
[Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA2225:Operator overloads have named alternates",
    Justification = "Compiler-required polyfill matching the BCL System.Index shape for C# index syntax.")]
internal readonly struct Index : IEquatable<Index>
{
    /// <summary>Initializes a new instance of the <see cref="Index"/> struct.</summary>
    /// <param name="value">The index value.</param>
    public Index(int value)
        : this(value, false)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="Index"/> struct.</summary>
    /// <param name="value">The index value.</param>
    /// <param name="fromEnd">Whether the index is relative to the end of the sequence.</param>
    public Index(int value, bool fromEnd)
    {
        Value = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
        IsFromEnd = fromEnd;
    }

    /// <summary>Gets an <see cref="Index"/> that points to the start of a sequence.</summary>
    public static Index Start => new(0);

    /// <summary>Gets an <see cref="Index"/> that points just past the end of a sequence.</summary>
    public static Index End => new(0, true);

    /// <summary>Gets the index value.</summary>
    public int Value { get; }

    /// <summary>Gets a value indicating whether the index is from the end of the sequence.</summary>
    public bool IsFromEnd { get; }

    /// <summary>Implicitly converts an <see cref="int"/> to an <see cref="Index"/> from the start.</summary>
    /// <param name="value">The zero-based index from the start.</param>
    public static implicit operator Index(int value) => new(value);

    /// <inheritdoc/>
    public static bool operator ==(Index left, Index right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Index left, Index right) => !left.Equals(right);

    /// <summary>Creates an <see cref="Index"/> from the start of a sequence.</summary>
    /// <param name="value">The zero-based index from the start.</param>
    /// <returns>The created <see cref="Index"/>.</returns>
    public static Index FromStart(int value) => new(value);

    /// <summary>Creates an <see cref="Index"/> from the end of a sequence.</summary>
    /// <param name="value">The zero-based index from the end.</param>
    /// <returns>The created <see cref="Index"/>.</returns>
    public static Index FromEnd(int value) => new(value, true);

    /// <summary>Calculates the zero-based offset for a sequence of the given length.</summary>
    /// <param name="length">The sequence length.</param>
    /// <returns>The zero-based offset from the start of the sequence.</returns>
    public int GetOffset(int length) => IsFromEnd ? length - Value : Value;

    /// <inheritdoc/>
    public bool Equals(Index other) => Value == other.Value && IsFromEnd == other.IsFromEnd;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Index other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Value, IsFromEnd);

    /// <inheritdoc/>
    public override string ToString() => IsFromEnd ? "^" + Value.ToString() : Value.ToString();
}
#endif
