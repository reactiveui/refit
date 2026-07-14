// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Reflection-free metadata describing how one method parameter becomes a multipart form part.</summary>
/// <remarks>
/// All Roslyn symbols are pre-resolved to display strings and names so the incremental generator cache holds only
/// value-equatable data, matching <see cref="QueryParameterModel"/> and <see cref="RequestParameterModel"/>.
/// </remarks>
/// <param name="Kind">The per-declared-type dispatch arm the part is added through.</param>
/// <param name="FieldName">The multipart form field name (the reflection builder's <c>parameterName</c>): the
/// <c>[AliasAs]</c> name or the declared parameter name.</param>
/// <param name="FileName">The multipart file name (the reflection builder's <c>fileName</c>): the obsolete
/// <c>[AttachmentName]</c> name when present, otherwise the same as <paramref name="FieldName"/>.</param>
/// <param name="IsEnumerable">Whether the declared type is an <c>IEnumerable&lt;T&gt;</c> of a reference-typed element
/// (so it is added as one part per element, mirroring the reflection builder's <c>IEnumerable&lt;object&gt;</c> check).</param>
internal sealed record MultipartPartModel(
    MultipartPartKind Kind,
    string FieldName,
    string FileName,
    bool IsEnumerable);
