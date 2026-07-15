// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>A URL parameter formatter that renders a <see cref="Temperature"/> with a trailing <c>deg</c> suffix,
/// registered per type so no hand-rolled type switch is needed.</summary>
public sealed class TemperatureUrlParameterFormatter : IUrlParameterFormatter
{
    /// <inheritdoc/>
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) =>
        value is Temperature temperature ? $"{temperature.Celsius}deg" : value?.ToString();
}
