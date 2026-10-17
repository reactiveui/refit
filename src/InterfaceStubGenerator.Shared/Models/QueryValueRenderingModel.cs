// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes how scalar or collection query values are formatted.</summary>
/// <param name="TreatAsString">Whether the raw value is stringified via <c>ToString()</c> before formatting, mirroring <c>[Query(TreatAsString = true)]</c>
/// and an explicitly empty <c>Format</c>.</param>
/// <param name="PreEncoded">Whether values pass through verbatim because the parameter carries <c>[Encoded]</c>.</param>
/// <param name="CollectionFormatValue">The explicit <c>CollectionFormat</c> underlying value, or null to use the settings default.</param>
/// <param name="ElementCanBeNull">Whether collection elements require a null check before formatting.</param>
/// <param name="ValueFormat">The reflection-free rendering strategy for the value or collection element.</param>
internal readonly record struct QueryValueRenderingModel(
    bool TreatAsString,
    bool PreEncoded,
    int? CollectionFormatValue,
    bool ElementCanBeNull,
    InlineValueFormatModel ValueFormat);
