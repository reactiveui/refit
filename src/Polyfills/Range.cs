// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NETFRAMEWORK
namespace System;

/// <summary>Minimal polyfill for <c>System.Range</c> so C# range syntax compiles on .NET Framework targets.</summary>
/// <remarks>
/// This is a compiler hook for older target frameworks. It intentionally mirrors the BCL member names used
/// by range lowering and should not grow into a general-purpose replacement for the framework type.
/// </remarks>
[Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA2225:Operator overloads have named alternates",
    Justification = "Compiler-required polyfill matching the BCL System.Range shape for C# range syntax.")]
[Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal readonly struct Range : IEquatable<Range>
{
    /// <summary>Initializes a new instance of the <see cref="Range"/> struct.</summary>
    /// <param name="start">The inclusive start index.</param>
    /// <param name="end">The exclusive end index.</param>
    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Gets a <see cref="Range"/> that represents the entire sequence.</summary>
    public static Range All => new(Index.Start, Index.End);

    /// <summary>Gets the inclusive start index.</summary>
    public Index Start { get; }

    /// <summary>Gets the exclusive end index.</summary>
    public Index End { get; }

    /// <summary>Creates a <see cref="Range"/> that starts at the specified index and ends at <see cref="Index.End"/>.</summary>
    /// <param name="start">The inclusive start index.</param>
    /// <returns>The created <see cref="Range"/>.</returns>
    public static Range StartAt(Index start) => new(start, Index.End);

    /// <summary>Creates a <see cref="Range"/> that starts at <see cref="Index.Start"/> and ends at the specified index.</summary>
    /// <param name="end">The exclusive end index.</param>
    /// <returns>The created <see cref="Range"/>.</returns>
    public static Range EndAt(Index end) => new(Index.Start, end);

    /// <inheritdoc/>
    public static bool operator ==(Range left, Range right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(Range left, Range right) => !left.Equals(right);

    /// <summary>Calculates the start offset and length for a sequence of the given length.</summary>
    /// <param name="length">The sequence length.</param>
    /// <returns>The offset and length represented by the range.</returns>
    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        var start = Start.GetOffset(length);
        var end = End.GetOffset(length);

        if ((uint)end > (uint)length || (uint)start > (uint)end)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        return (start, end - start);
    }

    /// <inheritdoc/>
    public bool Equals(Range other) => Start.Equals(other.Start) && End.Equals(other.End);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Range other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <inheritdoc/>
    public override string ToString() => Start.ToString() + ".." + End.ToString();
}
#endif
