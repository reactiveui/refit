// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the names and prefix available for a query object property.</summary>
/// <param name="ClrName">The declared CLR property name, formatted by the key formatter unless aliased.</param>
/// <param name="ExplicitName">The <c>[AliasAs]</c> name, which always wins and bypasses the key formatter, or null.</param>
/// <param name="SerializerName">The System.Text.Json <c>[JsonPropertyName]</c>, honored only when
/// <c>RefitSettings.HonorContentSerializerPropertyNamesInQuery</c> is set (mirroring the reflection builder, which resolves the same name through the
/// configured content serializer), or null.</param>
/// <param name="PrefixSegment">The compile-time <c>prefix + delimiter</c> prepended to the key, or null. This combines the enclosing parameter's
/// <c>[Query(Prefix)]</c> with the property's own.</param>
internal readonly record struct QueryPropertyNamesModel(
    string ClrName,
    string? ExplicitName,
    string? SerializerName,
    string? PrefixSegment);
