// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes a flattened query-object property whose value is a collection of simple elements.</summary>
/// <param name="CollectionFormatValue">The property's own <c>[Query(CollectionFormat)]</c> value, or null when it
/// inherits the enclosing parameter's collection format and then <c>RefitSettings.CollectionFormat</c>.</param>
/// <param name="ElementCanBeNull">Whether an element requires a null check before formatting.</param>
/// <param name="PropertyTypeName">The fully-qualified declared collection type, used as the attribute provider and
/// type when the custom formatter renders each element. Mirrors the reflection builder, which passes
/// <c>propertyInfo.PropertyType</c> for the element pass.</param>
/// <remarks>
/// The element rendering strategy is carried by the enclosing <see cref="QueryObjectPropertyModel.ValueFormat"/>.
/// A collection property carrying <c>[Query(Format)]</c> is not represented here: the reflection builder stringifies
/// the whole collection through the form formatter instead, so those keep using the reflection request builder.
/// </remarks>
internal sealed record QueryObjectCollectionModel(
    int? CollectionFormatValue,
    bool ElementCanBeNull,
    string PropertyTypeName);
