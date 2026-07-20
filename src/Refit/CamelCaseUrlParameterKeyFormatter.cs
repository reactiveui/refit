// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>Provides an implementation of <see cref="IUrlParameterKeyFormatter"/> that formats URL parameter keys in camelCase.</summary>
public class CamelCaseUrlParameterKeyFormatter : IUrlParameterKeyFormatter
{
    /// <summary>Formats the specified key.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The camelCase form of the key.</returns>
    public string Format(string key)
    {
#if NETCOREAPP
        return string.IsNullOrEmpty(key) || !char.IsUpper(key[0])
            ? key
            : string.Create(
                key.Length,
                key,
                static (chars, name) =>
                {
                    name.CopyTo(chars);
                    FixCasing(chars);
                });
#else
        if (string.IsNullOrEmpty(key) || !char.IsUpper(key[0]))
        {
            return key;
        }

        char[] chars = key.ToCharArray();
        FixCasing(chars);
        return new(chars);
#endif
    }

    /// <summary>Lowercases the leading uppercase run of characters in place.</summary>
    /// <param name="chars">The characters to adjust.</param>
    internal static void FixCasing(Span<char> chars)
    {
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
            {
                break;
            }

            var hasNext = i + 1 < chars.Length;

            // Stop when next char is already lowercase.
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }
    }
}
