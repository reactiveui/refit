// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>One public readable property flattened out of a query object into a query pair.</summary>
/// <param name="ClrName">The declared CLR property name, formatted by the key formatter unless aliased.</param>
/// <param name="ExplicitName">The <c>[AliasAs]</c> name, which always wins and bypasses the key formatter, or null.</param>
/// <param name="SerializerName">The System.Text.Json <c>[JsonPropertyName]</c>, honored only when
/// <c>RefitSettings.HonorContentSerializerPropertyNamesInQuery</c> is set (mirroring the reflection builder, which
/// resolves the same name through the configured content serializer), or null.</param>
/// <param name="PrefixSegment">The compile-time <c>prefix + delimiter</c> prepended to the key, or null.
/// This combines the enclosing parameter's <c>[Query(Prefix)]</c> with the property's own.</param>
/// <param name="SerializeNull">Whether a null value emits a bare <c>key=</c> instead of being omitted.</param>
/// <param name="CanBeNull">Whether the property value requires a null check before formatting.</param>
/// <param name="PropertyFormat">The property's <c>[Query(Format)]</c>, applied by the form-url-encoded
/// parameter formatter before the URL parameter formatter runs, or null.</param>
/// <param name="ValueFormat">The reflection-free rendering strategy for the value, or for each element when
/// <paramref name="Collection"/> is set.</param>
/// <param name="Collection">The collection descriptor when the property is a collection of simple elements, or null
/// for a scalar property. When set, <paramref name="ValueFormat"/> describes each element and
/// <paramref name="CanBeNull"/> describes the collection reference.</param>
/// <param name="Nested">The flattened properties of a nested concrete class/struct property, or null. When set, this
/// property contributes no value of its own; its children compose their keys under this property's key. Its own
/// <paramref name="PrefixSegment"/> holds only the property-level <c>[Query(Prefix)]</c>, without any parameter prefix.</param>
/// <param name="NestedThroughValue">Whether a nested property is a nullable value type (<c>Nullable&lt;T&gt;</c>), so its
/// children are accessed through <c>.Value</c> after the null check rather than off the value directly.</param>
/// <param name="Dictionary">The dictionary descriptor when the property is an <c>IDictionary&lt;simple, simple&gt;</c>, or
/// null. When set the property's entries expand under this property's key, one <c>key.entryKey=value</c> pair each.</param>
internal sealed record QueryObjectPropertyModel(
    string ClrName,
    string? ExplicitName,
    string? SerializerName,
    string? PrefixSegment,
    bool SerializeNull,
    bool CanBeNull,
    string? PropertyFormat,
    InlineValueFormatModel ValueFormat,
    QueryObjectCollectionModel? Collection = null,
    ImmutableEquatableArray<QueryObjectPropertyModel>? Nested = null,
    bool NestedThroughValue = false,
    QueryDictionaryModel? Dictionary = null);
