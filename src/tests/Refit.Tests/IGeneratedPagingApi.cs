// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Refit interface over the paged fixture API whose methods return the paged sequence directly.</summary>
internal interface IGeneratedPagingApi
{
    /// <summary>Lists every item, following the cursor each page names.</summary>
    /// <param name="cursor">The cursor of the first page to request, or <see langword="null"/> to start at the beginning.</param>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/cursor/items")]
    [Paged(Next = nameof(CursorPage.NextCursor))]
    PagedEnumerable<CursorPage, PagedItem> CursorItems([PageToken] string? cursor = null);

    /// <summary>Lists every item, following the cursor each page names, with a page size the fixture server ignores.</summary>
    /// <param name="cursor">The cursor of the first page to request.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/cursor/items")]
    [Paged(Next = nameof(CursorPage.NextCursor))]
    PagedEnumerable<CursorPage, PagedItem> CursorItems([PageToken] string? cursor, int pageSize);

    /// <summary>Lists every item by offset, ending when the offset reaches the total the page reports.</summary>
    /// <param name="limit">The most items on a page.</param>
    /// <param name="offset">The offset of the first page.</param>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/offset/items")]
    [Paged(Items = "Content.Items", Total = "Content.Total")]
    PagedEnumerable<ApiResponse<OffsetPage>, PagedItem> OffsetItems(int limit, [PageToken] int offset = 0);

    /// <summary>Lists every item by offset, following the offset each page names.</summary>
    /// <param name="limit">The most items on a page.</param>
    /// <param name="offset">The offset of the first page.</param>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/nextoffset/items")]
    [Paged(Next = nameof(NextOffsetPage.NextOffset))]
    PagedEnumerable<NextOffsetPage, PagedItem> NextOffsetItems(int limit, [PageToken] int offset = 0);

    /// <summary>Lists every item, with the continuation carried by a header in both directions.</summary>
    /// <param name="continuation">The continuation of the first page to request, or <see langword="null"/> to start at the beginning.</param>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/header/items")]
    [Paged(NextHeader = "x-ms-continuation")]
    PagedEnumerable<ApiResponse<CursorPage>, PagedItem> HeaderItems([PageToken] [Header("x-ms-continuation")] string? continuation = null);

    /// <summary>Lists every item, following links that may only name the client's own origin.</summary>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/link/items")]
    [Paged(Next = "Content.Next", SameOrigin = true)]
    PagedEnumerable<ApiResponse<LinkPage>, PagedItem> LinkItems();

    /// <summary>Lists every item, following links that may only name the fixture API's origin.</summary>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/link/items")]
    [Paged(Next = "Content.Next", Origins = [PagedApiHandler.Origin])]
    PagedEnumerable<ApiResponse<LinkPage>, PagedItem> ListedOriginLinkItems();

    /// <summary>Lists every item, following links to any origin.</summary>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/link/items")]
    [Paged(Next = "Content.Next", AnyOrigin = true)]
    PagedEnumerable<ApiResponse<LinkPage>, PagedItem> AnyOriginLinkItems();

    /// <summary>Lists every item, following the <c>Link</c> header of a bare array response.</summary>
    /// <returns>The lazy sequence of pages and items.</returns>
    [Get("/linkheader/items")]
    [Paged(NextHeader = "Link", SameOrigin = true)]
    PagedEnumerable<ApiResponse<List<PagedItem>>, PagedItem> LinkHeaderItems();
}
