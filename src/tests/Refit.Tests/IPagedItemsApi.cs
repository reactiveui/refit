// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Refit interface over the paged fixture API, one ordinary call per page.</summary>
public interface IPagedItemsApi
{
    /// <summary>Gets the cursor-addressed page that follows <paramref name="cursor"/>, or the first page when it is <see langword="null"/>.</summary>
    /// <param name="cursor">The cursor naming the page.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The page.</returns>
    [Get("/cursor/items")]
    Task<CursorPage> GetCursorPageAsync([Query] string? cursor, CancellationToken cancellationToken);

    /// <summary>Gets the offset-addressed page.</summary>
    /// <param name="offset">The index of the first item.</param>
    /// <param name="limit">The most items on the page.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The response holding the page.</returns>
    [Get("/offset/items")]
    Task<ApiResponse<OffsetPage>> GetOffsetPageAsync([Query] int offset, [Query] int limit, CancellationToken cancellationToken);

    /// <summary>Gets the first page of the link-addressed listing.</summary>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The response holding the page.</returns>
    [Get("/link/items")]
    Task<ApiResponse<LinkPage>> GetFirstLinkPageAsync(CancellationToken cancellationToken);

    /// <summary>Gets the link-addressed page at an absolute URL.</summary>
    /// <param name="url">The absolute URL of the page.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The response holding the page.</returns>
    [Get("")]
    Task<ApiResponse<LinkPage>> GetLinkPageAsync([Url] Uri url, CancellationToken cancellationToken);
}
