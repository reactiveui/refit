// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Provides Url parameter formatting.</summary>
public interface IUrlParameterFormatter
{
    /// <summary>Formats the specified value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="attributeProvider">The attribute provider.</param>
    /// <param name="type">Container class type.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    [RequiresUnreferencedCode(
        "Formatting enum values may reflect over runtime enum fields to read EnumMember metadata. Use the Refit source generator for trimmed/AOT apps.")]
    string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type);
}
