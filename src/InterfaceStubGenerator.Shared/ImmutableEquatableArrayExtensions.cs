// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Extension methods for creating <see cref="ImmutableEquatableArray{T}"/> instances.</summary>
internal static class ImmutableEquatableArrayExtensions
{
    /// <summary>Extensions for converting a sequence into an <see cref="ImmutableEquatableArray{T}"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The sequence of values to convert.</param>
    extension<T>(IEnumerable<T>? values)
        where T : IEquatable<T>
    {
        /// <summary>Creates an immutable equatable array from a sequence of values.</summary>
        /// <returns>An immutable equatable array containing the values.</returns>
        public ImmutableEquatableArray<T> ToImmutableEquatableArray() =>
            values is null ? ImmutableEquatableArray.Empty<T>() : new(values);
    }
}
