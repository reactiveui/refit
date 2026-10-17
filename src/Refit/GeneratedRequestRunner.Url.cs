// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Shared runtime helpers used by source-generated request construction.</summary>
public static partial class GeneratedRequestRunner
{
    /// <summary>Validates an opt-in relative or absolute URL without escaping its path or query.</summary>
    /// <param name="url">The string or URI supplied by a Url parameter.</param>
    /// <returns>The original URL text.</returns>
    /// <exception cref="ArgumentException">The value is null, empty, or not a valid URI reference.</exception>
    public static string RequireRelativeOrAbsoluteUrl(object? url)
    {
        var text = url is Uri uri ? uri.OriginalString : url as string;
        if (text is null or "" || !Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out _))
        {
            throw new ArgumentException("The [Url] parameter must be a non-empty relative or absolute URI.", nameof(url));
        }

        return text;
    }
}
