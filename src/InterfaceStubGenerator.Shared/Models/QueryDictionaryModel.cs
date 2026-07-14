// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Parsed metadata for a query parameter whose entries come from an <c>IDictionary&lt;TKey, TValue&gt;</c>.</summary>
/// <param name="KeyTypeName">The fully-qualified key type, which is also the attribute provider the reflection
/// request builder passes when formatting a key.</param>
/// <param name="KeyFormat">The reflection-free rendering strategy for a key.</param>
/// <param name="ValueCanBeNull">Whether a value requires a null check; the reflection builder omits null values.</param>
/// <param name="PrefixSegment">The enclosing parameter's compile-time <c>prefix + delimiter</c>, or null.</param>
/// <param name="ValueProperties">The flattened property descriptors of a sealed or value complex value type, so each
/// entry recurses into its value under the <c>entryKey.property</c> key exactly as the reflection builder's nested
/// <c>BuildQueryMap</c> does; null for a simple value, which renders as a single <c>entryKey=value</c> pair.</param>
internal sealed record QueryDictionaryModel(
    string KeyTypeName,
    InlineValueFormatModel KeyFormat,
    bool ValueCanBeNull,
    string? PrefixSegment,
    ImmutableEquatableArray<QueryObjectPropertyModel>? ValueProperties = null);
