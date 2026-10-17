// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Compile-time metadata describing how generated code formats one URL value without reflection.</summary>
/// <param name="Kind">The reflection-free rendering strategy for the value.</param>
/// <param name="Format">The compile-time format from <c>[Query(Format = ...)]</c>, or null when none applies.</param>
/// <param name="TypeName">The fully-qualified non-nullable value type, used to emit enum formatting helpers.</param>
/// <param name="IsNullableValueType">Whether the value is a nullable value type requiring a <c>.Value</c> unwrap.</param>
/// <param name="EnumMembers">The compile-time-resolved enum members for <see cref="InlineFormatKind.Enum"/>, or null.</param>
internal sealed record InlineValueFormatModel(
    InlineFormatKind Kind,
    string? Format,
    string TypeName,
    bool IsNullableValueType,
    EnumFormatMemberModel[]? EnumMembers)
{
    /// <summary>Gets a value indicating whether the value is a non-nullable, unformatted integer whose
    /// <c>ISpanFormattable</c> output is inherently URL-safe (digits and <c>-</c>), so generated path code can format it
    /// straight into a stack buffer with no intermediate string and no escaping. Only set for net6+ consumers.</summary>
    internal bool IsUrlSafeSpanFormattable { get; init; }

    /// <summary>Gets a value indicating whether the value is a non-nullable <c>ISpanFormattable</c> that generated path
    /// code can format into a stack buffer and escape span-to-string without the intermediate formatted string. Only set
    /// for net10+ consumers, where <c>Uri.EscapeDataString(ReadOnlySpan&lt;char&gt;)</c> exists.</summary>
    internal bool IsSpanFormattableEscapable { get; init; }

    /// <summary>Compares all parsed values, including array contents.</summary>
    /// <param name = "other">The model to compare.</param>
    /// <returns>Whether the models describe the same input.</returns>
    public bool Equals(InlineValueFormatModel? other) =>
        other is not null
        && EqualityComparer<InlineFormatKind>.Default.Equals(
            Kind,
            other.Kind)
        && EqualityComparer<string?>.Default.Equals(
            Format,
            other.Format)
        && EqualityComparer<string>.Default.Equals(
            TypeName,
            other.TypeName)
        && EqualityComparer<bool>.Default.Equals(
            IsNullableValueType,
            other.IsNullableValueType)
        && ArraySequenceComparer<EnumFormatMemberModel>.Instance.Equals(
            EnumMembers,
            other.EnumMembers)
        && EqualityComparer<bool>.Default.Equals(
            IsUrlSafeSpanFormattable,
            other.IsUrlSafeSpanFormattable)
        && EqualityComparer<bool>.Default.Equals(IsSpanFormattableEscapable, other.IsSpanFormattableEscapable);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Kind));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Format));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(TypeName));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(IsNullableValueType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(EnumMembers));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(IsUrlSafeSpanFormattable));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(IsSpanFormattableEscapable));
        return hash;
    }
}
