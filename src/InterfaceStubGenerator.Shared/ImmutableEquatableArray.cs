// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Helper methods for creating <c>ImmutableEquatableArray</c> instances.</summary>
internal static class ImmutableEquatableArray
{
    /// <summary>Gets an empty immutable equatable array.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>An empty array instance.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter is intentionally specified explicitly by callers.")]
    public static ImmutableEquatableArray<T> Empty<T>()
        where T : IEquatable<T> => ImmutableEquatableArray<T>.Empty;
}
