// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>The cached, settings-independent attribute metadata for a single readable public property on a form
/// source type, read once when the property list is first cached and reused for every request.</summary>
/// <param name="Property">The reflected property.</param>
/// <param name="AliasName">The <see cref="AliasAsAttribute.Name"/> override, or <see langword="null"/> when absent.</param>
/// <param name="Query">The <see cref="QueryAttribute"/> applied to the property, or <see langword="null"/> when absent.</param>
internal readonly record struct FormPropertyMetadata(
    PropertyInfo Property,
    string? AliasName,
    QueryAttribute? Query);
