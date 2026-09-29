// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotPackageSmoke;

/// <summary>Assertions for the smoke scenarios.</summary>
internal static class Check
{
    /// <summary>Throws when <paramref name="actual"/> differs from <paramref name="expected"/>.</summary>
    /// <typeparam name="T">The compared type.</typeparam>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The observed value.</param>
    /// <param name="what">What was compared, for the failure message.</param>
    /// <exception cref="InvalidOperationException">The values differ.</exception>
    internal static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{what}: expected '{expected}' but was '{actual}'.");
        }
    }
}
