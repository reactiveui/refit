// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>Verifies single-page read-ahead: it overlaps fetching with consumption and stays bounded and cancellable.</summary>
public partial class PagedEnumerableTests
{
    /// <summary>Verifies without read-ahead the next page is not requested while the consumer holds the current one.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_Off_RequestsTheNextPageOnlyWhenAsked()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        await using var items = CursorItems(server.CreateClient()).GetAsyncEnumerator();
        await Assert.That(await items.MoveNextAsync()).IsTrue();

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies read-ahead requests the next page while the consumer still holds the current one.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_RequestsTheNextPageWhileTheConsumerHoldsTheCurrentOne()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        await using var items = CursorItems(server.CreateClient()).WithPrefetch().GetAsyncEnumerator();
        await Assert.That(await items.MoveNextAsync()).IsTrue();

        await Assert.That(() => server.Requests.Count).WaitsFor(static count => count.IsEqualTo(RequestsThroughSecondPage), AwaitTimeout);
    }

    /// <summary>Verifies read-ahead yields the same items in the same order as sequential fetching.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_YieldsTheSameItemsAsSequentialFetching()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(CursorItems(server.CreateClient()).WithPrefetch());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount);
    }

    /// <summary>Verifies read-ahead never requests more pages than the page limit allows.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_NeverExceedsThePageLimit()
    {
        var server = new PagedApiHandler(ManyPagesItems, ManyPagesSize);

        var items = await CollectAsync(CursorItems(server.CreateClient()).WithPrefetch().WithMaxPages(PageLimit));

        await Assert.That(Ids(items)).IsEqualTo("1,2,3,4");
        await Assert.That(server.Requests.Count).IsEqualTo(PageLimit);
    }

    /// <summary>Verifies stopping cancels the read-ahead request in flight and requests nothing further.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_StoppingCancelsTheReadAheadInFlight()
    {
        var server = new PagedApiHandler(ManyPagesItems, ManyPagesSize);
        var inFlight = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnRequest = BlockSecondPageUntilCancelled(inFlight, aborted);

        var items = CursorItems(server.CreateClient()).WithPrefetch().GetAsyncEnumerator();
        await Assert.That(await items.MoveNextAsync()).IsTrue();
        await Assert.That(inFlight.Task).CompletesWithin(AwaitTimeout);
        await items.DisposeAsync();

        await Assert.That(aborted.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Verifies stopping disposes a read-ahead page that had already arrived.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_StoppingDisposesAReadAheadPageThatArrived()
    {
        var source = new DisposablePageSource(DisposablePageCount, PageSize);

        var pages = source.Sequence().WithPrefetch().AsPages().GetAsyncEnumerator();
        await Assert.That(await pages.MoveNextAsync()).IsTrue();
        await Assert.That(() => source.Created.Count).WaitsFor(static count => count.IsEqualTo(RequestsThroughSecondPage), AwaitTimeout);
        await pages.DisposeAsync();

        await Assert.That(source.Created.Count).IsEqualTo(RequestsThroughSecondPage);
        await Assert.That(source.Created.All(static created => created.IsDisposed)).IsTrue();
    }

    /// <summary>Verifies a read-ahead failure surfaces only when the consumer reaches the failed page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_ReadAheadFailureSurfacesWhenTheConsumerReachesThatPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { StatusForPage = static page => page == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK };

        List<PagedItem> seen = [];
        await Assert.That(async () =>
        {
            await foreach (var item in OffsetItems(server.CreateClient()).WithPrefetch())
            {
                seen.Add(item);
            }
        }).Throws<ApiException>();

        await Assert.That(Ids(seen)).IsEqualTo("1,2,3");
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Verifies a continuation selector failure with read-ahead on surfaces after the page's items.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Prefetch_On_ContinuationSelectorFailureSurfacesAfterThePageItems()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var items = PagedEnumerable.FromCursor(
            server.CreateClient().GetCursorPageAsync,
            static page => page.Items,
            static page => page.NextCursor == "c1" ? throw new InvalidOperationException("bad continuation") : page.NextCursor)
            .WithPrefetch();

        List<PagedItem> seen = [];
        await Assert.That(async () =>
        {
            await foreach (var item in items)
            {
                seen.Add(item);
            }
        }).Throws<InvalidOperationException>();

        await Assert.That(Ids(seen)).IsEqualTo("1,2,3");
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }
}
