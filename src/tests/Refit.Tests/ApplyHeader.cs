// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Applies a header name and value to a request, optionally validating the value. The seam that lets one
/// shared scenario body exercise both the reflection request builder and the source-generated request runner.</summary>
/// <param name="request">The request to modify.</param>
/// <param name="name">The header name.</param>
/// <param name="value">The header value, or <see langword="null"/> to remove the header.</param>
/// <param name="validateHeaders">Whether the value is validated as it is applied.</param>
internal delegate void ApplyHeader(HttpRequestMessage request, string name, string? value, bool validateHeaders);
