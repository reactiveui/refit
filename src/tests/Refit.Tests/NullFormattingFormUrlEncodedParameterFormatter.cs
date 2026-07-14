// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A form Url-encoded parameter formatter that always formats values as null.</summary>
public sealed class NullFormattingFormUrlEncodedParameterFormatter : IFormUrlEncodedParameterFormatter
{
    /// <summary>Formats the value as null.</summary>
    /// <param name="value">The value to format.</param>
    /// <param name="formatString">The format string.</param>
    /// <returns>Always <see langword="null"/>.</returns>
    public string? Format(object? value, string? formatString) => null;
}
