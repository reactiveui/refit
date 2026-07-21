// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Provides request parsing helpers for the Refit source generator.</summary>
internal static partial class Parser
{
    /// <summary>Provides equality contracts for parsed path-parameter locations.</summary>
    internal readonly partial struct PathParameterLocations
    {
        /// <summary>Determines whether two placeholder sets share the same backing occurrences.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true"/> when both wrap the same occurrence array.</returns>
        public static bool operator ==(PathParameterLocations left, PathParameterLocations right) => left.Equals(right);

        /// <summary>Determines whether two placeholder sets wrap different backing occurrences.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true"/> when the values differ.</returns>
        public static bool operator !=(PathParameterLocations left, PathParameterLocations right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(PathParameterLocations other) => ReferenceEquals(_occurrences, other._occurrences);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PathParameterLocations other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _occurrences?.GetHashCode() ?? 0;
    }
}
