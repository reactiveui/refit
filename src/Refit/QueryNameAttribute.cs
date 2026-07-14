// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Renders the parameter value as a valueless query flag — a bare <c>?name</c> segment with no <c>=value</c> —
/// the equivalent of Retrofit's <c>@QueryName</c>. A collection produces one flag per element
/// (<c>?a&amp;b&amp;c</c>); a <see langword="null"/> argument (or element) is omitted. The value is rendered
/// through <see cref="RefitSettings.UrlParameterFormatter"/> like any other query value, and is URL-encoded
/// unless the parameter also carries <see cref="EncodedAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class QueryNameAttribute : Attribute;
