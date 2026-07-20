// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>One readable public property of a query object, together with the attribute-derived facts the reflection
/// request builder would otherwise re-read from metadata on every request.</summary>
/// <param name="Property">The readable public instance property.</param>
/// <param name="IsIgnored">Whether the property carries a serialization-ignore attribute and is always skipped.</param>
/// <param name="QueryAttribute">The property's <see cref="Refit.QueryAttribute"/>, or <see langword="null"/> when absent.</param>
/// <param name="AliasAttribute">The property's <see cref="AliasAsAttribute"/>, or <see langword="null"/> when absent.</param>
internal readonly record struct QueryPropertyMetadata(
    PropertyInfo Property,
    bool IsIgnored,
    QueryAttribute? QueryAttribute,
    AliasAsAttribute? AliasAttribute);
