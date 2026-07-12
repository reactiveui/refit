// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Parsed query-binding metadata for one query-bound method parameter.</summary>
/// <param name="Key">The query key: the <c>[AliasAs]</c> name or the declared parameter name, verbatim.</param>
/// <param name="Shape">How the parameter renders into the query string.</param>
/// <param name="TreatAsString">Whether the raw value is stringified via <c>ToString()</c> before formatting,
/// mirroring <c>[Query(TreatAsString = true)]</c> and an explicitly empty <c>Format</c>.</param>
/// <param name="PreEncoded">Whether values pass through verbatim because the parameter carries <c>[Encoded]</c>.</param>
/// <param name="CollectionFormatValue">The explicit <c>CollectionFormat</c> underlying value, or null to use the settings default.</param>
/// <param name="ElementCanBeNull">Whether collection elements require a null check before formatting.</param>
/// <param name="ValueFormat">The reflection-free rendering strategy for the value or collection element.</param>
/// <param name="ObjectProperties">The flattened properties when <paramref name="Shape"/> is
/// <see cref="QueryParameterShape.Object"/>; otherwise null.</param>
/// <param name="Dictionary">The key metadata when <paramref name="Shape"/> is
/// <see cref="QueryParameterShape.Dictionary"/>; otherwise null. <paramref name="ValueFormat"/> renders the values.</param>
/// <param name="Converter">The converter metadata when <paramref name="Shape"/> is
/// <see cref="QueryParameterShape.Converter"/>; otherwise null.</param>
/// <param name="NestingDelimiter">The delimiter joining nested object keys (the parameter's <c>[Query]</c> delimiter,
/// default <c>"."</c>), used when <paramref name="ObjectProperties"/> contains nested properties.</param>
internal sealed record QueryParameterModel(
    string Key,
    QueryParameterShape Shape,
    bool TreatAsString,
    bool PreEncoded,
    int? CollectionFormatValue,
    bool ElementCanBeNull,
    InlineValueFormatModel ValueFormat,
    ImmutableEquatableArray<QueryObjectPropertyModel>? ObjectProperties = null,
    QueryDictionaryModel? Dictionary = null,
    QueryConverterModel? Converter = null,
    string NestingDelimiter = ".");
