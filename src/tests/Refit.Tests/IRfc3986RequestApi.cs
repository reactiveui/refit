// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Refit API surface exercising RFC 3986 request URI assembly.</summary>
public interface IRfc3986RequestApi
{
    /// <summary>Gets items with no query parameters.</summary>
    /// <returns>The response body.</returns>
    [Get("/items")]
    Task<string> GetWithoutQuery();

    /// <summary>Gets items filtered by a single query parameter.</summary>
    /// <param name="search">The search term appended to the query string.</param>
    /// <returns>The response body.</returns>
    [Get("/items")]
    Task<string> GetWithQuery(string search);
}
