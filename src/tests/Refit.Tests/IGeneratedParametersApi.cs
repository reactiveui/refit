// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit API surface used to exercise generated URL parameter formatting.</summary>
public interface IGeneratedParametersApi
{
    /// <summary>Gets a value using a simple string path parameter.</summary>
    /// <param name="value">The path parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/{value}")]
    Task<string> GetPath(string value);

    /// <summary>Gets a value using a simple string query parameter.</summary>
    /// <param name="queryKey">The query parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/?q={queryKey}")]
    Task<string> GetQuery(string queryKey);

    /// <summary>Gets a value using a simple string query parameter.</summary>
    /// <param name="queryKey">The query parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/?q={q}")]
    Task<string> GetQueryAlias([AliasAs("q")]string queryKey);

    /// <summary>Gets something where multiple parameters appear within a single URL segment.</summary>
    /// <param name="id">The identifier appearing in the segment.</param>
    /// <param name="width">The width value appearing in the segment.</param>
    /// <param name="height">The height value appearing in the segment.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/{id}/{width}x{height}/foo")]
    Task<string> FetchSomethingWithMultipleParametersPerSegment(int id, int width, int height);

    /// <summary>Gets something where multiple parameters appear within a single URL segment.</summary>
    /// <param name="id">The identifier appearing in the segment.</param>
    /// <param name="width">The width value appearing in the segment.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/{id}/{width}x{width}/foo")]
    Task<string> FetchSomethingWithMultipleRepeatedParametersPerSegment(int id, int width);

    /// <summary>Gets a value using a nullable parameter.</summary>
    /// <param name="value">The parameter value.</param>
    /// <returns>The response body.</returns>
    [Get("/a/{value}/b")]
    Task<string> GetNullableParam(int? value);
}
