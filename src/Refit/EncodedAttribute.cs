// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Marks a parameter value as already URL-encoded so Refit passes it through verbatim instead of escaping it —
/// the equivalent of Retrofit's <c>encoded = true</c>. Applies to path segment values (including round-tripping
/// <c>{**param}</c> segments), query values, and <see cref="QueryNameAttribute"/> flags. The caller becomes
/// responsible for producing valid encoded output; malformed values are sent as-is.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EncodedAttribute : Attribute;
