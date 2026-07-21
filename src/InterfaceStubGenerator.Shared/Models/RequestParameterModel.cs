// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Parsed request-binding metadata for one method parameter.</summary>
/// <param name="Name">The parameter metadata name.</param>
/// <param name="Type">The fully-qualified parameter type.</param>
/// <param name="Locations">The parameter's location in the URL template string, when this is a path parameter.</param>
/// <param name="Attributes">The parameter's attributes.</param>
/// <param name="Kind">The generated request binding kind.</param>
/// <param name="CanBeNull">Whether generated code must null-check the parameter before dereferencing.</param>
/// <param name="HeaderName">The request header name, when this is a header parameter.</param>
/// <param name="PropertyKey">The request property key, when this is a property parameter.</param>
/// <param name="BodySerializationMethod">The Refit body serialization method name, when this is a body parameter.</param>
/// <param name="BodyBufferMode">The body buffering mode, when this is a body parameter.</param>
internal readonly record struct RequestParameterModel(
    string Name,
    string Type,
    ImmutableEquatableArray<Range>? Locations,
    ImmutableEquatableArray<ParameterAttributeModel> Attributes,
    RequestParameterKind Kind,
    bool CanBeNull,
    string HeaderName,
    string PropertyKey,
    string BodySerializationMethod,
    BodyBufferMode BodyBufferMode)
{
    /// <summary>
    /// Gets the reflection-free form field descriptors for a URL-encoded body, or <see langword="null"/> when the
    /// body type is not eligible and the reflection-based form path must be used.
    /// </summary>
    internal ImmutableEquatableArray<FormFieldModel>? FormFields { get; init; }

    /// <summary>
    /// Gets the query-binding metadata when this parameter feeds the query string — set for
    /// <see cref="RequestParameterKind.Query"/> parameters and for <see cref="RequestParameterKind.Property"/>
    /// parameters that also carry <c>[Query]</c>.
    /// </summary>
    internal QueryParameterModel? Query { get; init; }

    /// <summary>Gets the reflection-free rendering strategy for a path parameter value, or <see langword="null"/> for non-path parameters.</summary>
    internal InlineValueFormatModel? ValueFormat { get; init; }

    /// <summary>Gets a value indicating whether a path parameter value passes through verbatim because the parameter carries <c>[Encoded]</c>.</summary>
    internal bool PreEncoded { get; init; }

    /// <summary>Gets the literal prefix prepended to a header value, or <see langword="null"/>. Set for an
    /// <c>[Authorize]</c> parameter, whose <c>Authorization</c> header value is <c>"{scheme} " + value</c>.</summary>
    internal string? HeaderValuePrefix { get; init; }

    /// <summary>Gets a value indicating whether a path parameter binds a round-trip <c>{**param}</c> catch-all whose
    /// value is split on <c>/</c> with each segment formatted and escaped, preserving the separators.</summary>
    internal bool IsRoundTrip { get; init; }

    /// <summary>Gets the dotted <c>{param.Prop}</c> path placeholder bindings for an object path parameter, or
    /// <see langword="null"/>. When set, the parameter contributes no direct path value; each binding fills its own
    /// placeholder with a formatted property value.</summary>
    internal ImmutableEquatableArray<PathObjectBindingModel>? PathObjectBindings { get; init; }

    /// <summary>Gets the multipart part descriptor when this parameter contributes a <c>[Multipart]</c> form part —
    /// set for <see cref="RequestParameterKind.MultipartPart"/> parameters and <see langword="null"/> otherwise.</summary>
    internal MultipartPartModel? MultipartPart { get; init; }
}
