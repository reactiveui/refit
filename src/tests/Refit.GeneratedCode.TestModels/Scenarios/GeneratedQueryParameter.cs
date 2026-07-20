// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>Represents a generated query parameter for use in Refit API calls.</summary>
    [DebuggerDisplay("{Value}")]
    public readonly struct GeneratedQueryParameter : IEquatable<GeneratedQueryParameter>
    {
        /// <summary>Initializes a new instance of the <see cref="GeneratedQueryParameter"/> struct with the specified value.</summary>
        /// <param name="value">The value of the query parameter.</param>
        public GeneratedQueryParameter(string value)
        {
            Value = value;
        }

        /// <summary>Gets the value of the query parameter.</summary>
        public string Value { get; }

        /// <inheritdoc/>
        public static bool operator ==(GeneratedQueryParameter left, GeneratedQueryParameter right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(GeneratedQueryParameter left, GeneratedQueryParameter right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public bool Equals(GeneratedQueryParameter other) => Value == other.Value;

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is GeneratedQueryParameter other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => Value;
    }
}
