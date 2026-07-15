// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Joins the client interface's shared route prefix onto each method's relative path for <see cref="RestMethodInfoInternal"/>.</summary>
internal partial class RestMethodInfoInternal
{
    /// <summary>Prepends the client interface's shared route prefix to a method's relative path.</summary>
    /// <param name="prefix">The shared route prefix, or an empty/whitespace string for a no-op.</param>
    /// <param name="path">The method's relative path template.</param>
    /// <returns>The path with the prefix prepended, joined by exactly one slash; the path unchanged when the prefix is empty.</returns>
    /// <remarks>Kept byte-for-byte identical to the source generator's <c>Parser.CombinePathPrefix</c> so both request
    /// paths stay at parity.</remarks>
    internal static string CombineWithPathPrefix(string prefix, string path)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return path;
        }

        var trimmedPrefix = prefix.TrimEnd('/');
        if (trimmedPrefix.Length == 0)
        {
            return path;
        }

        var trimmedPath = path.TrimStart('/');
        return trimmedPath.Length == 0
            ? trimmedPrefix
            : trimmedPrefix + "/" + trimmedPath;
    }
}
