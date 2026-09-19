// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Documentation;

/// <summary>Uses yes and no for booleans and declines unsupported values.</summary>
internal sealed class YesNoFormatter : IUrlParameterFormatter
{
    /// <summary>Returns yes or no for a boolean, or null for another value.</summary>
    /// <param name="value">The source value consumed by the converter or formatter.</param>
    /// <param name="attributeProvider">The source metadata; boolean formatting does not inspect its attributes.</param>
    /// <param name="type">The declared source type; this formatter chooses from the actual boolean value.</param>
    /// <returns>The yes or no text for a boolean, or null for an unsupported value.</returns>
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type) => value switch
    {
        true => "yes",
        false => "no",
        _ => null,
    };
}
