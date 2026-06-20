// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETSTANDARD2_0 || NET462
namespace System
{
    /// <summary>
    /// Minimal polyfill for <c>System.Index</c> to support the C# index syntax when targeting
    /// .NET Standard 2.0 or .NET Framework 4.6.2. This implementation only exposes the members
    /// required by this codebase and is not a full replacement for the BCL type.
    /// </summary>
    /// <remarks>
    /// This type exists solely to allow the source to compile on older targets where
    /// <c>System.Index</c> is not available. It should not be used as a general-purpose
    /// substitute outside of this project.
    /// </remarks>
    [Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA2225:Operator overloads have named alternates",
        Justification =
            "Polyfill mirroring the shape of the BCL System.Index; it is internal to the generator and intentionally matches the framework type.")]
    public readonly record struct Index
    {
        /// <summary>Initializes a new instance of the <see cref="Index"/> struct.</summary>
        /// <param name="value">The zero-based index from the start.</param>
        public Index(int value)
        {
            Value = value;
            IsFromEnd = false;
        }

        /// <summary>Initializes a new instance of the <see cref="Index"/> struct.</summary>
        /// <param name="value">The index position value.</param>
        /// <param name="fromEnd">
        /// When <see langword="true"/>, the index is calculated from the end of the sequence; otherwise from the start.
        /// </param>
        public Index(int value, bool fromEnd)
        {
            Value = value;
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

        /// <summary>Calculates the zero-based offset for a sequence of the given length.</summary>
        /// <param name="length">The sequence length.</param>
        /// <returns>The zero-based offset from the start of the sequence.</returns>
        public int GetOffset(int length) => IsFromEnd ? length - Value : Value;
    }
}
#endif
