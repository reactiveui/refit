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
    public readonly record struct Index
    {
        private readonly int _value;
        private readonly bool _fromEnd;

        /// <summary>
        /// Creates a new <see cref="Index"/> from the start of a sequence.
        /// </summary>
        /// <param name="value">The zero-based index from the start.</param>
        public Index(int value) { _value = value; _fromEnd = false; }

        /// <summary>
        /// Creates a new <see cref="Index"/> with the specified origin.
        /// </summary>
        /// <param name="value">The index position value.</param>
        /// <param name="fromEnd">
        /// When <see langword="true"/>, the index is calculated from the end of the sequence; otherwise from the start.
        /// </param>
        public Index(int value, bool fromEnd) { _value = value; _fromEnd = fromEnd; }

        /// <summary>
        /// Gets the index value.
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// Gets a value indicating whether the index is from the end of the sequence.
        /// </summary>
        public bool IsFromEnd => _fromEnd;

        /// <summary>
        /// Gets an <see cref="Index"/> that points to the start of a sequence.
        /// </summary>
        public static Index Start => new(0);

        /// <summary>
        /// Gets an <see cref="Index"/> that points just past the end of a sequence.
        /// </summary>
        public static Index End => new(0, true);

        /// <summary>
        /// Implicitly converts an <see cref="int"/> to an <see cref="Index"/> from the start.
        /// </summary>
        /// <param name="value">The zero-based index from the start.</param>
        public static implicit operator Index(int value) => new(value);
    }

    /// <summary>
    /// Minimal polyfill for <c>System.Range</c> to support the C# range syntax when targeting
    /// .NET Standard 2.0 or .NET Framework 4.6.2. This implementation only exposes the members
    /// required by this codebase and is not a full replacement for the BCL type.
    /// </summary>
    /// <remarks>
    /// This type exists solely to allow the source to compile on older targets where
    /// <c>System.Range</c> is not available. It should not be used as a general-purpose
    /// substitute outside of this project.
    /// </remarks>
    public readonly record struct Range
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Range"/> struct.
        /// </summary>
        /// <param name="start">The inclusive start <see cref="Index"/>.</param>
        /// <param name="end">The exclusive end <see cref="Index"/>.</param>
        public Range(Index start, Index end) { Start = start; End = end; }

        /// <summary>
        /// Gets the inclusive start <see cref="Index"/> of the range.
        /// </summary>
        public Index Start { get; }

        /// <summary>
        /// Gets the exclusive end <see cref="Index"/> of the range.
        /// </summary>
        public Index End { get; }

        /// <summary>
        /// Creates a <see cref="Range"/> that starts at the specified index and ends at <see cref="Index.End"/>.
        /// </summary>
        /// <param name="start">The inclusive start <see cref="Index"/>.</param>
        /// <returns>A new <see cref="Range"/>.</returns>
        public static Range StartAt(Index start) => new(start, Index.End);

        /// <summary>
        /// Creates a <see cref="Range"/> that starts at <see cref="Index.Start"/> and ends at the specified index.
        /// </summary>
        /// <param name="end">The exclusive end <see cref="Index"/>.</param>
        /// <returns>A new <see cref="Range"/>.</returns>
        public static Range EndAt(Index end) => new(Index.Start, end);

        /// <summary>
        /// Gets a <see cref="Range"/> that represents the entire sequence.
        /// </summary>
        public static Range All => new(Index.Start, Index.End);
    }
}
#endif
