// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>A non-default URL parameter formatter that wraps each formatted value in brackets, used to prove the
/// generated slow path reproduces the reflection builder's two formatting passes for collection properties.</summary>
public sealed class BracketUrlParameterFormatter : IUrlParameterFormatter
{
    /// <inheritdoc/>
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) =>
        value is null ? null : $"[{value}]";
}
