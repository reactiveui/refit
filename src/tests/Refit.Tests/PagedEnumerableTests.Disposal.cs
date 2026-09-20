// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>Verifies every page is disposed, and that an error response is not mistaken for the end of the listing.</summary>
public partial class PagedEnumerableTests
{
    /// <summary>The number of pages the disposable source spans.</summary>
    private const int DisposablePageCount = 3;

    /// <summary>Verifies a page stays alive while the consumer holds it and is disposed as the consumer moves on.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Disposal_PageIsAliveWhileHeldAndDisposedWhenTheConsumerMovesOn()
    {
        var source = new DisposablePageSource(DisposablePageCount, PageSize);

        List<string> disposedWhileHeld = [];
        await foreach (var page in source.Sequence().AsPages())
        {
            _ = page;
            disposedWhileHeld.Add(string.Concat(source.Created.Select(static created => created.IsDisposed ? 'x' : '-')));
        }

        await Assert.That(string.Join("|", disposedWhileHeld)).IsEqualTo("-|x-|xx-");
        await Assert.That(source.Created.All(static created => created.IsDisposed)).IsTrue();
    }

    /// <summary>Verifies stopping while consuming pages disposes the page that was being held.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Disposal_StoppingEarly_DisposesTheHeldPage()
    {
        var source = new DisposablePageSource(DisposablePageCount, PageSize);

        await using (var pages = source.Sequence().AsPages().GetAsyncEnumerator())
        {
            await Assert.That(await pages.MoveNextAsync()).IsTrue();
            await Assert.That(source.Created.All(static created => created.IsDisposed)).IsFalse();
        }

        await Assert.That(source.Created.Count).IsEqualTo(1);
        await Assert.That(source.Created.All(static created => created.IsDisposed)).IsTrue();
    }

    /// <summary>Verifies stopping in the middle of a page of items disposes that page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Disposal_StoppingMidPageOfItems_DisposesThePage()
    {
        var source = new DisposablePageSource(DisposablePageCount, PageSize);

        await foreach (var item in source.Sequence())
        {
            if (item.Id == MidPageItemId)
            {
                break;
            }
        }

        await Assert.That(source.Created.Count).IsEqualTo(1);
        await Assert.That(source.Created.All(static created => created.IsDisposed)).IsTrue();
    }

    /// <summary>Verifies cancelling while holding a page disposes it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Disposal_CancellationWhileHoldingAPage_DisposesIt()
    {
        var source = new DisposablePageSource(DisposablePageCount, PageSize);
        using var cancellation = new CancellationTokenSource();

        await Assert.That(async () =>
        {
            await foreach (var page in source.Sequence().AsPages().WithCancellation(cancellation.Token))
            {
                _ = page;
                await cancellation.CancelAsync();
            }
        }).Throws<OperationCanceledException>();

        await Assert.That(source.Created.Count).IsEqualTo(1);
        await Assert.That(source.Created.All(static created => created.IsDisposed)).IsTrue();
    }

    /// <summary>Verifies a page whose continuation selector failed is still disposed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Disposal_ContinuationSelectorFailure_StillDisposesThePage()
    {
        var source = new DisposablePageSource(DisposablePageCount, PageSize);

        await Assert.That(async () => await CollectAsync(source.Sequence(static (_, _) => throw new InvalidOperationException("bad continuation"))))
            .Throws<InvalidOperationException>();

        await Assert.That(source.Created.All(static created => created.IsDisposed)).IsTrue();
    }

    /// <summary>Verifies an unsuccessful response page throws its error, is disposed, and is not mistaken for the end of the listing.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Failure_UnsuccessfulResponsePage_IsDisposedAndThrows()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://api.example.com/items");
        var response = new DisposalTrackingResponse(HttpStatusCode.InternalServerError) { RequestMessage = request };
        var items = PagedEnumerable.Create(
            0,
            (index, _) => Task.FromResult(new ApiResponse<PagedItem[]>(response, null, new())),
            static page => page.Content,
            static (_, _) => default);

        await Assert.That(async () => await CollectAsync(items)).Throws<InvalidOperationException>();

        await Assert.That(response.IsDisposed).IsTrue();
    }

    /// <summary>Verifies an unsuccessful response from a generated client throws its <see cref="ApiException"/> and fetches nothing further.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Failure_UnsuccessfulResponse_ThrowsItsErrorAndFetchesNothingFurther()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { StatusForPage = static page => page == 1 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK };

        List<PagedItem> seen = [];
        await Assert.That(async () =>
        {
            await foreach (var item in OffsetItems(server.CreateClient()))
            {
                seen.Add(item);
            }
        }).Throws<ApiException>();

        await Assert.That(Ids(seen)).IsEqualTo("1,2,3");
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Verifies an unsuccessful first response throws its error to the consumer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Failure_UnsuccessfulFirstResponse_ThrowsWithoutYieldingAnything()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { StatusForPage = static _ => HttpStatusCode.Unauthorized };

        var failure = await Assert.That(async () => await CollectAsync(OffsetItems(server.CreateClient()))).Throws<ApiException>();

        await Assert.That(failure!.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }
}
