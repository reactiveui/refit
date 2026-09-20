// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies that <see cref="PagedEnumerable{TPage, TItem}"/> pages a generated client through cursors, offsets and
/// links while leaving the API's own page DTOs untouched.
/// </summary>
public partial class PagedEnumerableTests
{
    /// <summary>The number of items the fixture server holds; seven items in pages of three span three pages.</summary>
    private const int TotalItems = 7;

    /// <summary>The number of items on a full fixture page.</summary>
    private const int PageSize = 3;

    /// <summary>The number of pages the default fixture server spans.</summary>
    private const int PageCount = 3;

    /// <summary>The identifiers of every item the default fixture server holds, in order.</summary>
    private const string AllIds = "1,2,3,4,5,6,7";

    /// <summary>The page limit used where a test bounds the pages fetched.</summary>
    private const int PageLimit = 2;

    /// <summary>The number of requests the server has seen once the second page has been asked for.</summary>
    private const int RequestsThroughSecondPage = 2;

    /// <summary>The identifier of an item in the middle of the first page.</summary>
    private const int MidPageItemId = 2;

    /// <summary>The number of times a test enumerates the same sequence to compare the results.</summary>
    private const int EnumerationsCompared = 2;

    /// <summary>The number of pages a test consumes before it stops.</summary>
    private const int StopAfterPages = 2;

    /// <summary>The longest a test waits for the server to observe a request or a notification to arrive.</summary>
    private static readonly TimeSpan AwaitTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Gets the origin policy that allows only the fixture API's own origin.</summary>
    private static NextLinkOriginPolicy SameOrigin { get; } = NextLinkOriginPolicy.Allow(new Uri(PagedApiHandler.Origin));

    /// <summary>Verifies a cursor-paged Refit method is walked page by page, yielding every item in order.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cursor_ItemsSpanEveryPageInOrder()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(CursorItems(server.CreateClient()));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount);
        await Assert.That(RequestedQueries(server)).IsEqualTo("|?cursor=c1|?cursor=c2");
    }

    /// <summary>Verifies the pages are the API's own DTO instances, with their own continuation fields intact.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cursor_PagesAreTheApiDtoWithItsFieldsIntact()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var pages = await CollectAsync(CursorItems(server.CreateClient()).AsPages());

        using (Assert.Multiple())
        {
            await Assert.That(pages.Count).IsEqualTo(PageCount);
            await Assert.That(pages[0]).IsTypeOf<CursorPage>();
            await Assert.That(string.Join(",", pages.ConvertAll(static page => page.NextCursor ?? "none"))).IsEqualTo("c1,c2,none");
            await Assert.That(pages[PageCount - 1].Items.Count).IsEqualTo(1);
        }
    }

    /// <summary>Verifies an offset-paged method derives each next offset from the token that fetched the page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Offset_TokenAdvancesByThePageItemCount()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(OffsetItems(server.CreateClient()));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("?offset=0&limit=3|?offset=3&limit=3|?offset=6&limit=3");
    }

    /// <summary>Verifies pages of an offset-paged method are the <see cref="ApiResponse{T}"/> the Refit method returned.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Offset_PagesAreTheApiResponseTheMethodReturned()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        List<int> totals = [];
        await foreach (var page in OffsetItems(server.CreateClient()).AsPages())
        {
            using (Assert.Multiple())
            {
                await Assert.That(page.IsSuccessful).IsTrue();
                await Assert.That(page.RequestMessage.RequestUri!.AbsolutePath).IsEqualTo("/offset/items");
            }

            totals.Add(page.Content!.Total);
        }

        await Assert.That(string.Join(",", totals)).IsEqualTo("7,7,7");
    }

    /// <summary>Verifies a link-paged method follows each link the server returns until none remains.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Link_FollowsEachNextLinkTheServerReturns()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(LinkItems(server.CreateClient(), SameOrigin));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("|?page=1|?page=2");
    }

    /// <summary>Verifies a relative next link resolves against the request URI of the page that carried it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Link_RelativeNextLinkResolvesAgainstTheRequestUri()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { NextLinkForPage = static page => $"/link/items?page={page + 1}" };

        var items = await CollectAsync(LinkItems(server.CreateClient(), SameOrigin));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount);
    }

    /// <summary>Verifies the sequence itself can be consumed by <c>await foreach</c> without going through <c>Items</c>.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AwaitForeach_OverTheSequenceYieldsItems()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        List<PagedItem> items = [];
        await foreach (var item in CursorItems(server.CreateClient()))
        {
            items.Add(item);
        }

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
    }

    /// <summary>Verifies a given first token starts the enumeration part-way through the listing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Create_WithFirstToken_StartsFromThatToken()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var api = server.CreateClient();
        var items = PagedEnumerable.Create(
            PageSize,
            (offset, cancellationToken) => api.GetOffsetPageAsync(offset, PageSize, cancellationToken),
            static response => response.Content?.Items,
            static (response, offset) => NextOffset(response, offset) is { } next ? PageContinuation.To(next) : default);

        var collected = await CollectAsync(items);

        await Assert.That(Ids(collected)).IsEqualTo("4,5,6,7");
        await Assert.That(RequestedQueries(server)).IsEqualTo("?offset=3&limit=3|?offset=6&limit=3");
    }

    /// <summary>Verifies every enumeration starts again from the first page with fresh requests.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Enumerations_EachStartFromTheFirstPageWithFreshRequests()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var items = CursorItems(server.CreateClient());

        var first = await CollectAsync(items);
        var second = await CollectAsync(items);

        await Assert.That(Ids(second)).IsEqualTo(Ids(first));
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount * EnumerationsCompared);
    }

    /// <summary>Verifies a page whose item selector yields nothing contributes no items but does not end the sequence.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Items_PageWithNullItems_IsSkipped()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var items = PagedEnumerable.FromCursor(
            server.CreateClient().GetCursorPageAsync,
            static page => page.NextCursor == "c1" ? null : page.Items,
            static page => page.NextCursor);

        var collected = await CollectAsync(items);

        await Assert.That(Ids(collected)).IsEqualTo("4,5,6,7");
    }

    /// <summary>Verifies a page-fetching call that returns no page ends the sequence.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Pages_NullPage_EndsTheSequence()
    {
        var calls = 0;
        var items = PagedEnumerable.Create(
            (int token, CancellationToken _) =>
            {
                calls++;
                return Task.FromResult(token == 0 ? new CursorPage { Items = [new() { Id = 1 }] } : null);
            },
            static page => page!.Items,
            static (_, token) => PageContinuation.To(token + 1));

        var collected = await CollectAsync(items);

        await Assert.That(collected.Count).IsEqualTo(1);
        await Assert.That(calls).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Verifies an error thrown by the continuation selector surfaces once the page's own items were consumed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Continuation_SelectorFailure_SurfacesAfterThePageItems()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var items = PagedEnumerable.FromCursor(
            server.CreateClient().GetCursorPageAsync,
            static page => page.Items,
            static page => page.NextCursor == "c2" ? throw new InvalidOperationException("bad continuation") : page.NextCursor);

        List<PagedItem> collected = [];
        var failure = await Assert.That(async () =>
        {
            await foreach (var item in items)
            {
                collected.Add(item);
            }
        }).Throws<InvalidOperationException>();

        await Assert.That(failure!.Message).IsEqualTo("bad continuation");
        await Assert.That(Ids(collected)).IsEqualTo("1,2,3,4,5,6");
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Verifies the factories reject a missing argument.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Factories_NullArguments_Throw()
    {
        Func<string?, CancellationToken, Task<CursorPage>> fetch = static (_, _) => Task.FromResult(new CursorPage());
        Func<CursorPage, IEnumerable<PagedItem>?> select = static page => page.Items;
        Func<CursorPage, string?, PageContinuation<string?>> next = static (page, _) => PageContinuation.To(page.NextCursor);
        Func<Uri?, CancellationToken, Task<CursorPage>> fetchLink = static (_, _) => Task.FromResult(new CursorPage());
        Func<CursorPage, Uri?> nextLink = static _ => null;
        Func<string?, CancellationToken, Task<CursorPage>>? noFetch = null;
        Func<CursorPage, IEnumerable<PagedItem>?>? noSelect = null;
        Func<CursorPage, string?, PageContinuation<string?>>? noNext = null;
        Func<CursorPage, Uri?>? noNextLink = null;
        Func<CursorPage, string?>? noNextCursor = null;
        Func<CursorPage, int, int?>? noNextOffset = null;
        NextLinkOriginPolicy? noPolicy = null;
        Func<int, CancellationToken, Task<CursorPage>> fetchOffset = static (_, _) => Task.FromResult(new CursorPage());

        using (Assert.Multiple())
        {
            await Assert.That(() => PagedEnumerable.Create(noFetch!, select, next)).Throws<ArgumentNullException>();
            await Assert.That(() => PagedEnumerable.Create(fetch, noSelect!, next)).Throws<ArgumentNullException>();
            await Assert.That(() => PagedEnumerable.Create(fetch, select, noNext!)).Throws<ArgumentNullException>();
            await Assert.That(() => PagedEnumerable.FromCursor(fetch, select, noNextCursor!)).Throws<ArgumentNullException>();
            await Assert.That(() => PagedEnumerable.FromOffset(fetchOffset, select, noNextOffset!)).Throws<ArgumentNullException>();
            await Assert.That(() => PagedEnumerable.FromLinks(noPolicy!, fetchLink, select, nextLink)).Throws<ArgumentNullException>();
            await Assert.That(() => PagedEnumerable.FromLinks(SameOrigin, fetchLink, select, noNextLink!)).Throws<ArgumentNullException>();
        }
    }

    /// <summary>Verifies a continuation created from a token has a next page, and a null token ends the sequence.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PageContinuation_To_NullTokenEndsTheSequence()
    {
        var more = PageContinuation.To("c1");
        var none = PageContinuation.To<string?>(null);
        var offset = PageContinuation.To(PageSize);

        using (Assert.Multiple())
        {
            await Assert.That(more.HasNext).IsTrue();
            await Assert.That(more.Token).IsEqualTo("c1");
            await Assert.That(none.HasNext).IsFalse();
            await Assert.That(none).IsEqualTo(PageContinuation<string?>.End);
            await Assert.That(offset.HasNext).IsTrue();
            await Assert.That(offset.Token).IsEqualTo(PageSize);
            await Assert.That(PageContinuation<int>.End.HasNext).IsFalse();
        }
    }

    /// <summary>Collects an asynchronous sequence into a list.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to enumerate.</param>
    /// <returns>The elements in order.</returns>
    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        List<T> collected = [];
        await foreach (var element in source)
        {
            collected.Add(element);
        }

        return collected;
    }

    /// <summary>Formats item identifiers as a comma-separated string.</summary>
    /// <param name="items">The items.</param>
    /// <returns>The identifiers in order.</returns>
    private static string Ids(List<PagedItem> items) => string.Join(",", items.ConvertAll(static item => item.Id));

    /// <summary>Formats the query string of every request the server received, separated by bars.</summary>
    /// <param name="server">The fixture server.</param>
    /// <returns>The query strings in arrival order.</returns>
    private static string RequestedQueries(PagedApiHandler server) => string.Join("|", server.Requests.Select(static request => request.Uri.Query));

    /// <summary>Computes the offset of the page after an offset page.</summary>
    /// <param name="response">The response holding the page.</param>
    /// <param name="offset">The offset that fetched the page.</param>
    /// <returns>The next offset, or <see langword="null"/> when the page was the last.</returns>
    private static int? NextOffset(ApiResponse<OffsetPage> response, int offset)
    {
        var next = offset + response.Content!.Items.Count;
        return next < response.Content.Total ? next : null;
    }

    /// <summary>Builds a cursor-paged sequence over the fixture client.</summary>
    /// <param name="api">The generated client.</param>
    /// <returns>A sequence of cursor pages and their items.</returns>
    private static PagedEnumerable<CursorPage, PagedItem> CursorItems(IPagedItemsApi api) =>
        PagedEnumerable.FromCursor(
            api.GetCursorPageAsync,
            static page => page.Items,
            static page => page.NextCursor);

    /// <summary>Builds an offset-paged sequence over the fixture client.</summary>
    /// <param name="api">The generated client.</param>
    /// <returns>A sequence of offset pages and their items.</returns>
    private static PagedEnumerable<ApiResponse<OffsetPage>, PagedItem> OffsetItems(IPagedItemsApi api) =>
        PagedEnumerable.FromOffset(
            (offset, cancellationToken) => api.GetOffsetPageAsync(offset, PageSize, cancellationToken),
            static response => response.Content?.Items,
            NextOffset);

    /// <summary>Builds a link-paged sequence over the fixture client.</summary>
    /// <param name="api">The generated client.</param>
    /// <param name="policy">The policy deciding which next links may be followed.</param>
    /// <returns>A sequence of link pages and their items.</returns>
    private static PagedEnumerable<ApiResponse<LinkPage>, PagedItem> LinkItems(IPagedItemsApi api, NextLinkOriginPolicy policy) =>
        PagedEnumerable.FromLinks(
            policy,
            (url, cancellationToken) => url is null ? api.GetFirstLinkPageAsync(cancellationToken) : api.GetLinkPageAsync(url, cancellationToken),
            static response => response.Content?.Items,
            static response => response.Content?.Next);
}
