// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Provides value hashing and array comparison for parsed generator models.</summary>
internal static class GeneratorModelEquality
{
    /// <summary>Compares a required model array, treating a defaulted model's null storage as empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns>Whether the arrays contain equal sequences.</returns>
    internal static bool SequenceEquals<T>(T[]? left, T[]? right) =>
        left is null || right is null
            ? (left?.Length ?? 0) == (right?.Length ?? 0)
            : ArraySequenceComparer<T>.Instance.Equals(left, right);

    /// <summary>Hashes a model array, including absent optional arrays and defaulted required arrays.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The array to hash.</param>
    /// <returns>The sequence hash.</returns>
    internal static int GetSequenceHashCode<T>(T[]? values) =>
        values is null ? 0 : ArraySequenceComparer<T>.Instance.GetHashCode(values);

    /// <summary>Hashes a scalar or nested model value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to hash.</param>
    /// <returns>The value hash, or zero for null.</returns>
    internal static int GetHashCode<T>(T value) =>
        value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(value);

    /// <summary>Combines the hashes of two model members.</summary>
    /// <param name="left">The accumulated hash.</param>
    /// <param name="right">The next member hash.</param>
    /// <returns>The combined hash.</returns>
    internal static int Combine(int left, int right) =>
        ((int)(((uint)left << 5) | ((uint)left >> 27)) + left) ^ right;
}
