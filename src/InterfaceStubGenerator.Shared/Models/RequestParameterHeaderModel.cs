// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes RequestParameterHeaderModel metadata.</summary>
/// <param name="HeaderName">The request header name when the parameter contributes a dynamic header, independently of its primary binding kind.</param>
/// <param name="HeaderValuePrefix">the literal prefix prepended to a header value, or <see langword="null"/>. Set for an <c>[Authorize]</c> parameter,
/// whose <c>Authorization</c> header value is <c>"{scheme} " + value</c>.</param>
internal readonly record struct RequestParameterHeaderModel(
    string HeaderName,
    string? HeaderValuePrefix);
