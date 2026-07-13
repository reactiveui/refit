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
    /// <summary>The highest code point that percent-encodes as a single ASCII byte.</summary>
    private const char MaxAsciiChar = (char)0x7F;

    /// <summary>The bit shift selecting the high nibble of a byte for hex encoding.</summary>
    private const int HexShift = 4;

    /// <summary>The mask selecting the low nibble of a byte for hex encoding.</summary>
    private const int HexMask = 0xF;

    /// <summary>The uppercase hex digits used by percent-encoding, matching <see cref="Uri.EscapeDataString(string)"/>.</summary>
    private const string UpperHexDigits = "0123456789ABCDEF";

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

    /// <summary>Determines whether the value starts with the specified character.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="prefix">The character the value is expected to start with.</param>
    /// <returns><see langword="true"/> if the value starts with <paramref name="prefix"/>.</returns>
    /// <remarks>The <see cref="string"/> overload taking a <see cref="char"/> only exists on the modern targets.</remarks>
    internal static bool StartsWith(string value, char prefix) =>
#if NET8_0_OR_GREATER
        value.StartsWith(prefix);
#else
        value.Length > 0 && value[0] == prefix;
#endif

    /// <summary>Determines whether the value contains the specified character.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="character">The character to find.</param>
    /// <returns><see langword="true"/> if the value contains <paramref name="character"/>.</returns>
    /// <remarks>The <see cref="string"/> overload taking a <see cref="char"/> only exists on the modern targets.</remarks>
    internal static bool Contains(string value, char character) =>
#if NET8_0_OR_GREATER
        value.Contains(character);
#else
        value.IndexOf(character) >= 0;
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
    internal static string EscapeDataString(string value, int start, int length) =>
#if NET10_0_OR_GREATER
        Uri.EscapeDataString(value.AsSpan(start, length));
#else
        Uri.EscapeDataString(value[start..(start + length)]);
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

#if NET8_0_OR_GREATER
    /// <summary>Percent-encodes a formatted span per RFC 3986 straight into the target, with no intermediate string.</summary>
    /// <param name="target">The buffer receiving the escaped text.</param>
    /// <param name="span">The invariant-formatted value to escape.</param>
    /// <remarks>Span-formattable values render as ASCII under the invariant culture, which this escapes in place. A
    /// non-ASCII character (never produced by the routed value types) defers to the framework escaper so the UTF-8
    /// percent-encoding matches <see cref="Uri.EscapeDataString(string)"/> exactly.</remarks>
    internal static void AppendUriDataEscaped(ref ValueStringBuilder target, scoped ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (c > MaxAsciiChar)
            {
#if NET10_0_OR_GREATER
                target.Append(Uri.EscapeDataString(span));
#else
                target.Append(Uri.EscapeDataString(span.ToString()));
#endif
                return;
            }
        }

        foreach (var c in span)
        {
            if (IsUriUnreserved(c))
            {
                target.Append(c);
            }
            else
            {
                target.Append('%');
                target.Append(UpperHexDigits[c >> HexShift]);
                target.Append(UpperHexDigits[c & HexMask]);
            }
        }
    }
#endif

    /// <summary>Finds the first CR or LF character in the value.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>The first CR or LF index, or -1 when none is present.</returns>
    private static int IndexOfCrOrLf(string value) =>
#if NET8_0_OR_GREATER
        value.AsSpan().IndexOfAny(_lineBreakCharacters);
#else
        value.IndexOfAny(_lineBreakCharacters);
#endif

#if NET8_0_OR_GREATER
    /// <summary>Determines whether a character is RFC 3986 unreserved and needs no percent-encoding.</summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true"/> for an unreserved character.</returns>
    private static bool IsUriUnreserved(char c) =>
        c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.' or '_' or '~';
#endif
}
