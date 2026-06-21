// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if NET8_0_OR_GREATER
using System.Buffers;
#endif
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Provides shared string helpers with modern runtime fast paths.</summary>
internal static class StringHelpers
{
#if NET8_0_OR_GREATER
    /// <summary>Characters that must be removed from HTTP header names and values.</summary>
    private static readonly SearchValues<char> _lineBreakCharacters = SearchValues.Create("\r\n");
#else
    /// <summary>Characters that must be removed from HTTP header names and values.</summary>
    private static readonly char[] _lineBreakCharacters = ['\r', '\n'];
#endif

    /// <summary>Determines whether the value contains CR or LF characters.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> if the value contains CR or LF characters.</returns>
    internal static bool ContainsCrOrLf(string value) =>
#if NET8_0_OR_GREATER
        value.AsSpan().ContainsAny(_lineBreakCharacters);
#else
        value.IndexOfAny(_lineBreakCharacters) >= 0;
#endif

    /// <summary>Escapes a string for a URI data component.</summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value.</returns>
    internal static string EscapeDataString(string value) => Uri.EscapeDataString(value);

    /// <summary>Escapes a string slice for a URI data component.</summary>
    /// <param name="value">The value containing the slice.</param>
    /// <param name="start">The slice start index.</param>
    /// <param name="length">The slice length.</param>
    /// <returns>The escaped value.</returns>
    [SuppressMessage(
        "Performance",
        "CA1846:Prefer AsSpan over Substring",
        Justification = "The span overload is only available on targets newer than net9.0.")]
    internal static string EscapeDataString(string value, int start, int length) =>
#if NET10_0_OR_GREATER
        Uri.EscapeDataString(value.AsSpan(start, length));
#else
        Uri.EscapeDataString(value.Substring(start, length));
#endif

    /// <summary>Removes CR and LF characters from an HTTP header name or value.</summary>
    /// <param name="value">The header name or value.</param>
    /// <returns>The sanitized value.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2930:\"IDisposables\" should be disposed",
        Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
    internal static string RemoveCrOrLf(string value)
    {
        var firstMatch = IndexOfCrOrLf(value);
        if (firstMatch < 0)
        {
            return value;
        }

        var builder = new ValueStringBuilder(stackalloc char[256]);
        builder.Append(value.AsSpan(0, firstMatch));
        for (var i = firstMatch + 1; i < value.Length; i++)
        {
            var character = value[i];
            if (character is not ('\r' or '\n'))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    /// <summary>Finds the first CR or LF character in the value.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>The first CR or LF index, or -1 when none is present.</returns>
    private static int IndexOfCrOrLf(string value) =>
#if NET8_0_OR_GREATER
        value.AsSpan().IndexOfAny(_lineBreakCharacters);
#else
        value.IndexOfAny(_lineBreakCharacters);
#endif
}
