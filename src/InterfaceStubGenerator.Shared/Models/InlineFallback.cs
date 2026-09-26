// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Why a method's request is not generated inline, and which parameter caused it.</summary>
/// <param name="Reason">The first limitation found, or <see cref="InlineFallbackReason.None"/> when the request is generated.</param>
/// <param name="ParameterOrdinal">The zero-based ordinal of the parameter responsible, or -1 when the method itself is.</param>
/// <remarks>An ordinal rather than a <c>Location</c> keeps the request model value-equatable across edits; the analyzer
/// maps it back to the parameter's declaration.</remarks>
internal readonly record struct InlineFallback(InlineFallbackReason Reason, int ParameterOrdinal)
{
    /// <summary>Gets the value for a request that is generated inline.</summary>
    internal static InlineFallback None => default;

    /// <summary>Gets a value indicating whether the request is generated inline.</summary>
    internal bool IsNone => Reason == InlineFallbackReason.None;

    /// <summary>Creates a fallback caused by the method rather than one parameter.</summary>
    /// <param name="reason">The limitation found.</param>
    /// <returns>The method-level fallback.</returns>
    internal static InlineFallback ForMethod(InlineFallbackReason reason) => new(reason, -1);
}
