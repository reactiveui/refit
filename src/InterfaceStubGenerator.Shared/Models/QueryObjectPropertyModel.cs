// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the parsed metadata for QueryObjectPropertyModel.</summary>
/// <param name="Names">The Names metadata.</param>
/// <param name="SerializeNull">The SerializeNull metadata.</param>
/// <param name="CanBeNull">The CanBeNull metadata.</param>
/// <param name="PropertyFormat">The PropertyFormat metadata.</param>
/// <param name="ValueFormat">The ValueFormat metadata.</param>
/// <param name="Collection">The Collection metadata.</param>
/// <param name="Nested">The Nested metadata.</param>
/// <param name="NestedThroughValue">The NestedThroughValue metadata.</param>
/// <param name="Dictionary">The Dictionary metadata.</param>
internal sealed record QueryObjectPropertyModel(
    QueryPropertyNamesModel Names,
    bool SerializeNull,
    bool CanBeNull,
    string? PropertyFormat,
    InlineValueFormatModel ValueFormat,
    QueryObjectCollectionModel? Collection = null,
    QueryObjectPropertyModel[]? Nested = null,
    bool NestedThroughValue = false,
    QueryDictionaryModel? Dictionary = null)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(QueryObjectPropertyModel? other) =>
        other is not null
        && EqualityComparer<QueryPropertyNamesModel>.Default.Equals(
            Names,
            other.Names)
        && EqualityComparer<bool>.Default.Equals(
            SerializeNull,
            other.SerializeNull)
        && EqualityComparer<bool>.Default.Equals(
            CanBeNull,
            other.CanBeNull)
        && EqualityComparer<string?>.Default.Equals(
            PropertyFormat,
            other.PropertyFormat)
        && EqualityComparer<InlineValueFormatModel>.Default.Equals(
            ValueFormat,
            other.ValueFormat)
        && EqualityComparer<QueryObjectCollectionModel?>.Default.Equals(
            Collection,
            other.Collection)
        && ArraySequenceComparer<QueryObjectPropertyModel>.Instance.Equals(
            Nested,
            other.Nested)
        && EqualityComparer<bool>.Default.Equals(
            NestedThroughValue,
            other.NestedThroughValue)
        && EqualityComparer<QueryDictionaryModel?>.Default.Equals(Dictionary, other.Dictionary);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Names));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(SerializeNull));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(CanBeNull));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(PropertyFormat));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(ValueFormat));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Collection));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Nested));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(NestedThroughValue));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Dictionary));
        return hash;
    }
}
