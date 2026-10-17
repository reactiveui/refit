// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the query entries produced by a method parameter.</summary>
/// <param name="Key">The query key or object prefix.</param>
/// <param name="Shape">The scalar, collection or object shape.</param>
/// <param name="Rendering">The value formatting strategy.</param>
/// <param name="ObjectProperties">The flattened object property bindings, or null.</param>
/// <param name="Dictionary">The dictionary expansion strategy, or null.</param>
/// <param name="Converter">The custom converter binding, or null.</param>
/// <param name="NestingDelimiter">The separator between nested key segments.</param>
internal sealed record QueryParameterModel(
    string Key,
    QueryParameterShape Shape,
    QueryValueRenderingModel Rendering,
    QueryObjectPropertyModel[]? ObjectProperties = null,
    QueryDictionaryModel? Dictionary = null,
    QueryConverterModel? Converter = null,
    string NestingDelimiter = ".")
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(QueryParameterModel? other) =>
        other is not null
        && EqualityComparer<string>.Default.Equals(
            Key,
            other.Key)
        && EqualityComparer<QueryParameterShape>.Default.Equals(
            Shape,
            other.Shape)
        && EqualityComparer<QueryValueRenderingModel>.Default.Equals(
            Rendering,
            other.Rendering)
        && ArraySequenceComparer<QueryObjectPropertyModel>.Instance.Equals(
            ObjectProperties,
            other.ObjectProperties)
        && EqualityComparer<QueryDictionaryModel?>.Default.Equals(
            Dictionary,
            other.Dictionary)
        && EqualityComparer<QueryConverterModel?>.Default.Equals(
            Converter,
            other.Converter)
        && EqualityComparer<string>.Default.Equals(NestingDelimiter, other.NestingDelimiter);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Key));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Shape));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Rendering));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(ObjectProperties));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Dictionary));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Converter));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(NestingDelimiter));
        return hash;
    }
}
