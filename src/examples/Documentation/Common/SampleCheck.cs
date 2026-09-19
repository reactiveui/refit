// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Throws when a runnable example observes an unexpected result.</summary>
internal static class SampleCheck
{
    /// <summary>Checks values with their default equality comparer.</summary>
    /// <typeparam name="T">The compared value type.</typeparam>
    /// <param name="expected">The value required by the scenario.</param>
    /// <param name="actual">The value observed from the source implementation.</param>
    /// <exception cref="InvalidOperationException">The values differ.</exception>
    internal static void Equal<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            return;
        }

        throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }
}
