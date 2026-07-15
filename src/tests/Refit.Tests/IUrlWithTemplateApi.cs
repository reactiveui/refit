// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An invalid API that combines a <c>[Url]</c> parameter with a non-empty path template.</summary>
public interface IUrlWithTemplateApi
{
    /// <summary>Declares a path template alongside a <c>[Url]</c> parameter, which is not allowed.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <returns>The response body.</returns>
    [Get("/foo")]
    Task<string> Invalid([Url] string url);
}
