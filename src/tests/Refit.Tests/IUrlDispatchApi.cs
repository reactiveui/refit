// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose methods dispatch to an absolute URL supplied per call by a <c>[Url]</c> parameter.</summary>
public interface IUrlDispatchApi
{
    /// <summary>Follows an absolute or relative redirect target.</summary>
    /// <param name="url">The redirect target.</param>
    /// <param name="token">An additional query value.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> GetRelative([Url(AllowRelative = true)] string url, [Query(Name = "access_token")] string token);

    /// <summary>Builds a relative request without adding query parameters.</summary>
    /// <param name="url">The URI reference.</param>
    /// <returns>The request.</returns>
    [Get("")]
    Task<HttpRequestMessage> BuildRelative([Url(AllowRelative = true)] Uri url);

    /// <summary>Builds a relative request with converter-supplied query keys.</summary>
    /// <param name="url">The URI reference.</param>
    /// <param name="filter">The query values.</param>
    /// <returns>The request.</returns>
    [Get("")]
    Task<HttpRequestMessage> BuildRelativeWithConverter(
        [Url(AllowRelative = true)] Uri url,
        [QueryConverter(typeof(DictionaryObjectQueryConverter))]
        IDictionary<string, object> filter);

    /// <summary>Dispatches to the absolute URL supplied by the <c>[Url]</c> string parameter.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> GetAbsolute([Url] string url);

    /// <summary>Dispatches to an absolute URL and appends a <c>[Query]</c> parameter to it.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="token">A query value appended to the absolute URL.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> GetAbsoluteWithQuery([Url] string url, [Query] string token);

    /// <summary>Dispatches to the absolute URL supplied by the <c>[Url]</c> <see cref="Uri"/> parameter.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <returns>The response body.</returns>
    [Get("")]
    Task<string> GetAbsoluteFromUri([Url] Uri url);

    /// <summary>Posts a body to the absolute URL supplied by the <c>[Url]</c> <see cref="Uri"/> parameter.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="body">The request body, bound implicitly rather than to the URL.</param>
    /// <returns>The response body.</returns>
    [Post("")]
    Task<string> PostAbsolute([Url] Uri url, BodyPayload body);
}
