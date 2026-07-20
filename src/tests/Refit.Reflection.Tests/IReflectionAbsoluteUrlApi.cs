// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A Refit interface that dispatches to an absolute URL supplied by a <c>[Url]</c> parameter and appends a
/// <c>[Query]</c> parameter to it.</summary>
public interface IReflectionAbsoluteUrlApi
{
    /// <summary>Dispatches to an absolute URL and appends a query parameter to it.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="token">A query value appended to the absolute URL.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> GetAbsoluteWithQuery([Url] string url, [Query] string token);
}
