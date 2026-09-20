// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies cancellation between pages and while a page is being fetched.</summary>
public partial class PagedEnumerableTests
{
    /// <summary>Verifies a token cancelled before enumeration starts sends no request.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cancellation_BeforeEnumeration_SendsNoRequest()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.That(async () =>
        {
            await foreach (var item in CursorItems(server.CreateClient()).WithCancellation(cancellation.Token))
            {
                _ = item;
            }
        }).Throws<OperationCanceledException>();

        await Assert.That(server.Requests.Count).IsEqualTo(0);
    }

    /// <summary>Verifies cancelling between items stops before the next item, so no further page is fetched.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cancellation_BetweenItems_StopsWithoutFetchingAnotherPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        using var cancellation = new CancellationTokenSource();

        List<PagedItem> seen = [];
        await Assert.That(async () =>
        {
            await foreach (var item in CursorItems(server.CreateClient()).WithCancellation(cancellation.Token))
            {
                seen.Add(item);
                await cancellation.CancelAsync();
            }
        }).Throws<OperationCanceledException>();

        await Assert.That(Ids(seen)).IsEqualTo("1");
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies cancelling between pages stops before the next page is requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cancellation_BetweenPages_StopsBeforeTheNextRequest()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        using var cancellation = new CancellationTokenSource();

        await Assert.That(async () =>
        {
            await foreach (var page in OffsetItems(server.CreateClient()).AsPages().WithCancellation(cancellation.Token))
            {
                _ = page;
                await cancellation.CancelAsync();
            }
        }).Throws<OperationCanceledException>();

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies cancelling while a page request is in flight aborts that request.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cancellation_DuringAPage_AbortsTheRequestInFlight()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var inFlight = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnRequest = BlockSecondPageUntilCancelled(inFlight, aborted);
        using var cancellation = new CancellationTokenSource();

        var enumeration = Task.Run(async () =>
        {
            await foreach (var item in CursorItems(server.CreateClient()).WithCancellation(cancellation.Token))
            {
                _ = item;
            }
        });
        await inFlight.Task;
        await cancellation.CancelAsync();

        await Assert.That(async () => await enumeration).Throws<OperationCanceledException>();
        await Assert.That(aborted.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Builds a server step that holds the request for the second cursor page until its token is cancelled.</summary>
    /// <param name="inFlight">Completed when the held request arrives.</param>
    /// <param name="aborted">Completed when the held request observes cancellation.</param>
    /// <returns>The server step.</returns>
    private static Func<HttpRequestMessage, CancellationToken, Task> BlockSecondPageUntilCancelled(TaskCompletionSource inFlight, TaskCompletionSource aborted) =>
        async (request, cancellationToken) =>
        {
            if (!request.RequestUri!.Query.Contains("c1", StringComparison.Ordinal))
            {
                return;
            }

            inFlight.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                aborted.SetResult();
                throw;
            }
        };
}
