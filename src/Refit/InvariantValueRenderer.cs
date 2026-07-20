// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace Refit;

/// <summary>Renders a non-null parameter value to its string form under the invariant culture.</summary>
/// <remarks>Shared by the default URL and form-url-encoded parameter formatters. It reproduces the observable result
/// of <c>string.Format(CultureInfo.InvariantCulture, "{0}"/"{0:format}", value)</c> without the composite-format
/// parsing and the intermediate result copy: a string is returned verbatim, and a formattable value is rendered
/// directly through <see cref="IFormattable.ToString(string, IFormatProvider)"/>.</remarks>
internal static class InvariantValueRenderer
{
    /// <summary>Renders the value using the invariant culture and an optional format string.</summary>
    /// <param name="value">The non-null value to render.</param>
    /// <param name="formatString">The format string, or <see langword="null"/>/whitespace for no format.</param>
    /// <returns>The rendered string.</returns>
    internal static string Render(object value, string? formatString)
    {
        // A string is emitted verbatim: composite formatting has no alignment component here, and a format specifier
        // is ignored for a non-IFormattable argument, so the original string is returned unchanged.
        if (value is string text)
        {
            return text;
        }

        var format = string.IsNullOrWhiteSpace(formatString) ? null : formatString;
        return value is IFormattable formattable
            ? formattable.ToString(format, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }
}
