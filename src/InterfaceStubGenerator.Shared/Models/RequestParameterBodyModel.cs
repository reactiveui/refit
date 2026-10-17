// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes how a parameter's value becomes request content.</summary>
/// <param name="BodySerializationMethod">The Refit body serialization method name, when this is a body parameter.</param>
/// <param name="BodyBufferMode">The body buffering mode, when this is a body parameter.</param>
/// <param name="FormFields">the reflection-free form field descriptors for a URL-encoded body, or <see langword="null"/> when the body type is not eligible
/// and the reflection-based form path must be used.</param>
/// <param name="Compression">the <c>Refit.RequestCompression</c> member name the body declared, or <see langword="null"/> for a parameter that is not a
/// body. <c>Default</c> means the request takes its coding from the settings.</param>
/// <param name="CompressionLevel">the <c>System.IO.Compression.CompressionLevel</c> member name the body declared, or <see langword="null"/> for a
/// parameter that is not a body.</param>
/// <param name="MultipartPart">The multipart part descriptor when this parameter contributes a <c>[Multipart]</c> form part, set for <see
/// cref="RequestParameterKind.MultipartPart"/> parameters and <see langword="null"/> otherwise.</param>
/// <param name="JsonLinesElementType">the fully-qualified element type a JSON Lines body is written with, or <see langword="null"/> when the body keeps the
/// untyped path that serializes every element as <see cref="object"/>.</param>
/// <param name="IsAsyncJsonLines">a value indicating whether a typed JSON Lines body is an <c>IAsyncEnumerable&lt;T&gt;</c>.</param>
internal readonly record struct RequestParameterBodyModel(
    string BodySerializationMethod,
    BodyBufferMode BodyBufferMode,
    FormFieldModel[]? FormFields,
    string? Compression,
    string? CompressionLevel,
    MultipartPartModel? MultipartPart,
    string? JsonLinesElementType,
    bool IsAsyncJsonLines)
{
    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(RequestParameterBodyModel other) =>
        EqualityComparer<string>.Default.Equals(
            BodySerializationMethod,
            other.BodySerializationMethod)
        && EqualityComparer<BodyBufferMode>.Default.Equals(
            BodyBufferMode,
            other.BodyBufferMode)
        && ArraySequenceComparer<FormFieldModel>.Instance.Equals(
            FormFields,
            other.FormFields)
        && EqualityComparer<string?>.Default.Equals(
            Compression,
            other.Compression)
        && EqualityComparer<string?>.Default.Equals(
            CompressionLevel,
            other.CompressionLevel)
        && EqualityComparer<MultipartPartModel?>.Default.Equals(
            MultipartPart,
            other.MultipartPart)
        && EqualityComparer<string?>.Default.Equals(
            JsonLinesElementType,
            other.JsonLinesElementType)
        && EqualityComparer<bool>.Default.Equals(IsAsyncJsonLines, other.IsAsyncJsonLines);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(BodySerializationMethod));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(BodyBufferMode));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(FormFields));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Compression));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(CompressionLevel));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(MultipartPart));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(JsonLinesElementType));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(IsAsyncJsonLines));
        return hash;
    }
}
