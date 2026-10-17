// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes how a parameter fills route placeholders.</summary>
/// <param name="Locations">The parameter's location in the URL template string, when this is a path parameter.</param>
/// <param name="ValueFormat">the reflection-free rendering strategy for a path parameter value, or <see langword="null"/> for non-path parameters.</param>
/// <param name="PreEncoded">a value indicating whether a path parameter value passes through verbatim because the parameter carries
/// <c>[Encoded]</c>.</param>
/// <param name="IsRoundTrip">a value indicating whether a path parameter binds a round-trip <c>{**param}</c> catch-all whose value is split on <c>/</c>
/// with each segment formatted and escaped, preserving the separators.</param>
/// <param name="PathObjectBindings">the dotted <c>{param.Prop}</c> path placeholder bindings for an object path parameter, or <see langword="null"/>. When
/// set, the parameter contributes no direct path value; each binding fills its own placeholder with a formatted property value.</param>
internal readonly record struct RequestParameterPathModel(
    Range[]? Locations,
    InlineValueFormatModel? ValueFormat,
    bool PreEncoded,
    bool IsRoundTrip,
    PathObjectBindingModel[]? PathObjectBindings)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(RequestParameterPathModel other) =>
        ArraySequenceComparer<Range>.Instance.Equals(
            Locations,
            other.Locations)
        && EqualityComparer<InlineValueFormatModel?>.Default.Equals(
            ValueFormat,
            other.ValueFormat)
        && EqualityComparer<bool>.Default.Equals(
            PreEncoded,
            other.PreEncoded)
        && EqualityComparer<bool>.Default.Equals(
            IsRoundTrip,
            other.IsRoundTrip)
        && ArraySequenceComparer<PathObjectBindingModel>.Instance.Equals(PathObjectBindings, other.PathObjectBindings);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Locations));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ValueFormat));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(PreEncoded));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(IsRoundTrip));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(PathObjectBindings));
        return hash;
    }
}
