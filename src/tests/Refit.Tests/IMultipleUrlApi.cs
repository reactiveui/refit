// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An invalid API that declares more than one <c>[Url]</c> parameter.</summary>
public interface IMultipleUrlApi
{
    /// <summary>Declares two <c>[Url]</c> parameters, which is not allowed.</summary>
    /// <param name="first">The first absolute request URL.</param>
    /// <param name="second">The second absolute request URL.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> Invalid([Url] string first, [Url] string second);
}
