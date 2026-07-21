// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Helper methods for creating <c>ImmutableEquatableArray</c> instances.</summary>
internal static class ImmutableEquatableArrayFactory
{
    /// <summary>Gets an empty immutable equatable array.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An empty array instance.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter is intentionally specified explicitly by callers.")]
    internal static ImmutableEquatableArray<T> Empty<T>()
        where T : IEquatable<T> => ImmutableEquatableArray<T>.Empty;

    /// <summary>Wraps an array without another copy.</summary>
    /// <param name="values">The values to wrap.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An immutable equatable array over <paramref name="values"/>.</returns>
    internal static ImmutableEquatableArray<T> FromArray<T>(T[] values)
        where T : IEquatable<T> =>
        values.Length == 0 ? Empty<T>() : new(values);

    /// <summary>Copies a list into the immutable equatable array backing storage.</summary>
    /// <param name="values">The values to copy.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An immutable equatable array containing <paramref name="values"/>.</returns>
    internal static ImmutableEquatableArray<T> FromList<T>(List<T> values)
        where T : IEquatable<T>
    {
        if (values.Count == 0)
        {
            return Empty<T>();
        }

        var array = new T[values.Count];
        values.CopyTo(array);
        return new(array);
    }
}
