// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes RequestBodyModel metadata.</summary>
/// <param name="IsMultipart">a value indicating whether the method is a <c>[Multipart]</c> request whose form parts are constructed inline. When set, the
/// generated method builds a <c>MultipartFormDataContent</c> as the request body instead of following the normal single-body path.</param>
/// <param name="MultipartBoundary">the multipart boundary text, used only when <see cref="IsMultipart"/> is set. Mirrors the reflection builder's boundary
/// selection: the <c>[Multipart(boundary)]</c> argument, or the attribute default.</param>
internal readonly record struct RequestBodyModel(
    bool IsMultipart,
    string MultipartBoundary);
