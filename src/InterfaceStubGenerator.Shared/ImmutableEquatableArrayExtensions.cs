// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Extension methods for creating <see cref="ImmutableEquatableArray{T}"/> instances.</summary>
internal static class ImmutableEquatableArrayExtensions
{
    /// <summary>Extensions for converting a list into an <see cref="ImmutableEquatableArray{T}"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The values to convert.</param>
    extension<T>(List<T>? values)
        where T : IEquatable<T>
    {
        /// <summary>Creates an immutable equatable array from a list.</summary>
        /// <returns>An immutable equatable array containing the list values.</returns>
        public ImmutableEquatableArray<T> ToImmutableEquatableArray() =>
            values is null
                ? ImmutableEquatableArrayFactory.Empty<T>()
                : ImmutableEquatableArrayFactory.FromList(values);
    }
}
