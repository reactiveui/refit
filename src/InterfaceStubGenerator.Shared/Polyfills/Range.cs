// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NETSTANDARD2_0 || NET462
namespace System
{
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
        /// <summary>Initializes a new instance of the <see cref="Range"/> struct.</summary>
        /// <param name="start">The inclusive start <see cref="Index"/>.</param>
        /// <param name="end">The exclusive end <see cref="Index"/>.</param>
        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        /// <summary>Gets a <see cref="Range"/> that represents the entire sequence.</summary>
        public static Range All => new(Index.Start, Index.End);

        /// <summary>Gets the inclusive start <see cref="Index"/> of the range.</summary>
        public Index Start { get; }

        /// <summary>Gets the exclusive end <see cref="Index"/> of the range.</summary>
        public Index End { get; }

        /// <summary>Creates a <see cref="Range"/> that starts at the specified index and ends at <see cref="Index.End"/>.</summary>
        /// <param name="start">The inclusive start <see cref="Index"/>.</param>
        /// <returns>A new <see cref="Range"/>.</returns>
        public static Range StartAt(Index start) => new(start, Index.End);

        /// <summary>Creates a <see cref="Range"/> that starts at <see cref="Index.Start"/> and ends at the specified index.</summary>
        /// <param name="end">The exclusive end <see cref="Index"/>.</param>
        /// <returns>A new <see cref="Range"/>.</returns>
        public static Range EndAt(Index end) => new(Index.Start, end);
    }
}
#endif
