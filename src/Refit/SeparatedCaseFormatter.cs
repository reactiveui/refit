// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;

namespace Refit;

/// <summary>Converts PascalCase/camelCase identifiers into a lower-case, separator-delimited form (e.g. snake_case or kebab-case).</summary>
internal static class SeparatedCaseFormatter
{
    /// <summary>Extra capacity reserved for the separators inserted before upper-case characters.</summary>
    private const int SeparatorCapacityHeadroom = 8;

    /// <summary>Formats the given identifier using the supplied word separator.</summary>
    /// <param name="key">The identifier to format.</param>
    /// <param name="separator">The character inserted between words.</param>
    /// <returns>The lower-case, separator-delimited form of <paramref name="key"/>.</returns>
    public static string Format(string key, char separator)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        var builder = new StringBuilder(key.Length + SeparatorCapacityHeadroom);
        for (var i = 0; i < key.Length; i++)
        {
            var current = key[i];
            if (char.IsUpper(current))
            {
                if (i > 0 && NeedsSeparatorBefore(key, i))
                {
                    _ = builder.Append(separator);
                }

                _ = builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                _ = builder.Append(current);
            }
        }

        return builder.ToString();
    }

    /// <summary>Determines whether a word boundary precedes the uppercase character at the given index.</summary>
    /// <param name="key">The identifier being formatted.</param>
    /// <param name="index">The index of the uppercase character.</param>
    /// <returns><see langword="true"/> when a separator should be inserted before the character.</returns>
    internal static bool NeedsSeparatorBefore(string key, int index)
    {
        var previous = key[index - 1];

        // A new word starts after a lowercase letter or digit, or at the tail of an
        // acronym followed by a lowercase letter (e.g. the 'P' in "JSONParser" -> "json_parser").
        return char.IsLower(previous)
            || char.IsDigit(previous)
            || (char.IsUpper(previous) && index + 1 < key.Length && char.IsLower(key[index + 1]));
    }
}
