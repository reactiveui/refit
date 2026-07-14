// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Compile-time metadata for one form-url-encoded field of a strongly-typed body.</summary>
/// <param name="PropertyName">The declared CLR property name read by the generated getter.</param>
/// <param name="ExplicitName">The explicit field name from <c>[AliasAs]</c> or <c>[JsonPropertyName]</c>, or <see langword="null"/>.</param>
/// <param name="PrefixSegment">The precomputed <c>prefix + delimiter</c> prepended to the field name, or <see langword="null"/>.</param>
/// <param name="Format">The <c>[Query]</c> value format, or <see langword="null"/>.</param>
/// <param name="CollectionFormatValue">The explicit collection format underlying value, or <see langword="null"/> to use the settings default.</param>
/// <param name="SerializeNull">Whether a <see langword="null"/> value should be serialized as an empty field.</param>
/// <param name="CanBeNull">Whether the field value can be <see langword="null"/> and therefore needs a null guard.</param>
/// <param name="ValueFormat">The reflection-free scalar value fast-path descriptor, or <see langword="null"/> when the
/// field is not a simple scalar (a collection or complex property) and the body must keep using the descriptor path.</param>
internal sealed record FormFieldModel(
    string PropertyName,
    string? ExplicitName,
    string? PrefixSegment,
    string? Format,
    int? CollectionFormatValue,
    bool SerializeNull,
    bool CanBeNull,
    InlineValueFormatModel? ValueFormat);
