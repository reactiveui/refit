// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Fixture used to verify <see cref="UrlResolutionMode.Rfc3986"/> base-address resolution.</summary>
public interface IRfcUrlResolutionApi
{
    /// <summary>Relative path without a leading slash: appended to the base address path under RFC 3986.</summary>
    /// <returns>The response body.</returns>
    [Get("values")]
    Task<string> GetValuesRelative();

    /// <summary>Relative path with a dynamic segment and no leading slash.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>The response body.</returns>
    [Get("users/{id}")]
    Task<string> GetUser(int id);

    /// <summary>Relative path with a leading slash: replaces the base address path under RFC 3986.</summary>
    /// <returns>The response body.</returns>
    [Get("/values")]
    Task<string> GetValuesAbsolute();

    /// <summary>Relative path without a leading slash that also carries a query string.</summary>
    /// <param name="page">The page number added as a query parameter.</param>
    /// <returns>The response body.</returns>
    [Get("values?active=true")]
    Task<string> GetValuesWithQuery(int page);
}
