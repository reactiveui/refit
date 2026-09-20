// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the sequence fetches only what the consumer asks for.</summary>
public partial class PagedEnumerableTests
{
    /// <summary>The number of items on the many-page listing used to prove a page limit.</summary>
    private const int ManyPagesItems = 20;

    /// <summary>The page size of the many-page listing.</summary>
    private const int ManyPagesSize = 2;

    /// <summary>Verifies building and configuring a sequence sends no request.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Sequence_BeforeEnumeration_RequestsNothing()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = CursorItems(server.CreateClient()).WithPrefetch().WithMaxPages(PageLimit);
        _ = items.AsPages();
        _ = items.ToObservable();

        await Assert.That(server.Requests.Count).IsEqualTo(0);
    }

    /// <summary>Verifies stopping in the middle of a page fetches no further page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Items_StopMidPage_FetchesNoFurtherPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        await foreach (var item in CursorItems(server.CreateClient()))
        {
            if (item.Id == MidPageItemId)
            {
                break;
            }
        }

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies stopping after the last item of a page fetches no further page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Items_StopAtPageBoundary_FetchesNoFurtherPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        await foreach (var item in CursorItems(server.CreateClient()))
        {
            if (item.Id == PageSize)
            {
                break;
            }
        }

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies stopping while consuming pages fetches no page beyond the one stopped on.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Pages_StopAfterSecondPage_FetchesNoThirdPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var seen = 0;
        await foreach (var page in OffsetItems(server.CreateClient()).AsPages())
        {
            _ = page;
            seen++;
            if (seen == StopAfterPages)
            {
                break;
            }
        }

        await Assert.That(server.Requests.Count).IsEqualTo(StopAfterPages);
    }

    /// <summary>Verifies the page limit bounds how many pages are fetched, however many the API has.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithMaxPages_BoundsThePagesFetched()
    {
        var server = new PagedApiHandler(ManyPagesItems, ManyPagesSize);

        var items = await CollectAsync(CursorItems(server.CreateClient()).WithMaxPages(PageLimit));

        await Assert.That(Ids(items)).IsEqualTo("1,2,3,4");
        await Assert.That(server.Requests.Count).IsEqualTo(PageLimit);
    }

    /// <summary>Verifies a page limit of one fetches only the first page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithMaxPages_One_FetchesOnlyTheFirstPage()
    {
        var server = new PagedApiHandler(ManyPagesItems, ManyPagesSize);

        var items = await CollectAsync(CursorItems(server.CreateClient()).WithMaxPages(1));

        await Assert.That(Ids(items)).IsEqualTo("1,2");
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies a continuation selector that fails on the last permitted page is never consulted for a page that will not be fetched.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithMaxPages_FailingContinuationOnLastPermittedPage_DoesNotSurface()
    {
        var server = new PagedApiHandler(ManyPagesItems, ManyPagesSize);
        var items = PagedEnumerable.FromCursor(
            server.CreateClient().GetCursorPageAsync,
            static page => page.Items,
            static _ => throw new InvalidOperationException("never needed"))
            .WithMaxPages(1);

        var collected = await CollectAsync(items);

        await Assert.That(Ids(collected)).IsEqualTo("1,2");
    }

    /// <summary>Verifies the page limit is validated.</summary>
    /// <param name="pages">The invalid limit.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task WithMaxPages_NotPositive_Throws(int pages)
    {
        var items = CursorItems(new PagedApiHandler(TotalItems, PageSize).CreateClient());

        await Assert.That(() => items.WithMaxPages(pages)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies the sequence describes its configuration for the debugger.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToString_DescribesPrefetchAndPageLimit()
    {
        var items = CursorItems(new PagedApiHandler(TotalItems, PageSize).CreateClient());

        using (Assert.Multiple())
        {
            await Assert.That(items.ToString()).IsEqualTo($"PagedEnumerable: Prefetch = False, MaxPages = {int.MaxValue}");
            await Assert.That(items.WithPrefetch().WithMaxPages(PageLimit).ToString()).IsEqualTo($"PagedEnumerable: Prefetch = True, MaxPages = {PageLimit}");
        }
    }

    /// <summary>Verifies configuring a sequence leaves the original untouched.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Configuration_ReturnsNewSequenceAndLeavesTheOriginalUnbounded()
    {
        var server = new PagedApiHandler(ManyPagesItems, ManyPagesSize);
        var original = CursorItems(server.CreateClient());

        _ = original.WithMaxPages(1);
        var items = await CollectAsync(original);

        await Assert.That(items.Count).IsEqualTo(ManyPagesItems);
    }
}
