// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies that Refit interface methods declared to return <see cref="PagedEnumerable{TPage, TItem}"/> are generated as
/// lazy sequences that page through cursors, offsets, headers and links while leaving the API's DTOs untouched.
/// </summary>
public partial class GeneratedPagedMethodTests
{
    /// <summary>The number of items the fixture server holds; seven items in pages of three span three pages.</summary>
    private const int TotalItems = 7;

    /// <summary>The number of items on a full fixture page.</summary>
    private const int PageSize = 3;

    /// <summary>The number of pages the default fixture server spans.</summary>
    private const int PageCount = 3;

    /// <summary>The identifiers of every item the default fixture server holds, in order.</summary>
    private const string AllIds = "1,2,3,4,5,6,7";

    /// <summary>The identifiers of the items from the second page on.</summary>
    private const string IdsFromSecondPage = "4,5,6,7";

    /// <summary>The number of requests the server has seen once the second page has been asked for.</summary>
    private const int RequestsThroughSecondPage = 2;

    /// <summary>The longest a test waits for the server to observe a request.</summary>
    private static readonly TimeSpan AwaitTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Verifies a cursor method yields every item in order, sending the cursor each page named.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cursor_ItemsSpanEveryPageInOrder()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().CursorItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("|?cursor=c1|?cursor=c2");
    }

    /// <summary>Verifies the cursor argument is the token of the first page, so an enumeration can resume from a saved cursor.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cursor_ArgumentStartsTheEnumerationPartWayThrough()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().CursorItems("c1"));

        await Assert.That(Ids(items)).IsEqualTo(IdsFromSecondPage);
        await Assert.That(RequestedQueries(server)).IsEqualTo("?cursor=c1|?cursor=c2");
    }

    /// <summary>Verifies the pages are the API's own DTO instances, with their own continuation fields intact.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cursor_PagesAreTheApiDtoWithItsFieldsIntact()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var pages = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().CursorItems().AsPages());

        using (Assert.Multiple())
        {
            await Assert.That(pages.Count).IsEqualTo(PageCount);
            await Assert.That(pages[0]).IsTypeOf<CursorPage>();
            await Assert.That(string.Join(",", pages.ConvertAll(static page => page.NextCursor ?? "none"))).IsEqualTo("c1,c2,none");
        }
    }

    /// <summary>Verifies another parameter of an overloaded method is sent with every page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Overload_SendsItsOtherParametersWithEveryPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().CursorItems(null, PageSize));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("?pageSize=3|?cursor=c1&pageSize=3|?cursor=c2&pageSize=3");
    }

    /// <summary>Verifies an offset method that names the total ends the sequence when the offset reaches it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Offset_TotalEndsTheSequenceWhenTheOffsetReachesIt()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().OffsetItems(PageSize));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("?limit=3&offset=0|?limit=3&offset=3|?limit=3&offset=6");
    }

    /// <summary>Verifies the offset argument is the first offset requested.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Offset_ArgumentStartsTheEnumerationPartWayThrough()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().OffsetItems(PageSize, PageSize));

        await Assert.That(Ids(items)).IsEqualTo(IdsFromSecondPage);
        await Assert.That(RequestedQueries(server)).IsEqualTo("?limit=3&offset=3|?limit=3&offset=6");
    }

    /// <summary>Verifies offset pages are the <see cref="ApiResponse{T}"/> the method returned, each successful and tied to its request.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Offset_PagesAreTheApiResponse()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        List<int> totals = [];
        await foreach (var page in server.CreateClientFor<IGeneratedPagingApi>().OffsetItems(PageSize).AsPages())
        {
            await Assert.That(page.IsSuccessful).IsTrue();
            totals.Add(page.Content!.Total);
        }

        await Assert.That(string.Join(",", totals)).IsEqualTo("7,7,7");
    }

    /// <summary>Verifies a page that names the next offset as a nullable value continues until it names none.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NextOffset_NullableValueContinuesUntilNull()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().NextOffsetItems(PageSize));

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(RequestedQueries(server)).IsEqualTo("?limit=3&offset=0|?limit=3&offset=3|?limit=3&offset=6");
    }

    /// <summary>Verifies a continuation carried by a header is read from each response and sent as a header with the next request.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Header_ContinuationTravelsInBothDirections()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().HeaderItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(string.Join("|", server.Requests.Select(static request => request.Continuation ?? "none"))).IsEqualTo("none|c1|c2");
    }

    /// <summary>Verifies a fetch is never issued before the sequence is enumerated, however many times it is created.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Laziness_CallingTheMethodSendsNothing()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var api = server.CreateClientFor<IGeneratedPagingApi>();

        _ = api.CursorItems();
        _ = api.OffsetItems(PageSize);
        _ = api.LinkItems();

        await Assert.That(server.Requests.Count).IsEqualTo(0);
    }

    /// <summary>Verifies stopping after the first item fetches no further page.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EarlyTermination_FetchesNoAdditionalPage()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        await using (var items = server.CreateClientFor<IGeneratedPagingApi>().CursorItems().GetAsyncEnumerator())
        {
            await Assert.That(await items.MoveNextAsync()).IsTrue();
        }

        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies the page limit bounds the pages fetched from a generated method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithMaxPages_BoundsThePagesFetched()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IGeneratedPagingApi>().CursorItems().WithMaxPages(RequestsThroughSecondPage));

        await Assert.That(Ids(items)).IsEqualTo("1,2,3,4,5,6");
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
    }

    /// <summary>Verifies a token cancelled before enumeration sends no request.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cancellation_BeforeEnumeration_SendsNoRequest()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.That(async () =>
        {
            await foreach (var item in server.CreateClientFor<IGeneratedPagingApi>().CursorItems().WithCancellation(cancellation.Token))
            {
                _ = item;
            }
        }).Throws<OperationCanceledException>();

        await Assert.That(server.Requests.Count).IsEqualTo(0);
    }

    /// <summary>Verifies the enumeration's token reaches the request in flight.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Cancellation_DuringAPage_AbortsTheRequestInFlight()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var inFlight = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnRequest = async (request, cancellationToken) =>
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
        using var cancellation = new CancellationTokenSource();

        var enumeration = Task.Run(async () =>
        {
            await foreach (var item in server.CreateClientFor<IGeneratedPagingApi>().CursorItems().WithCancellation(cancellation.Token))
            {
                _ = item;
            }
        });
        await inFlight.Task;
        await cancellation.CancelAsync();

        await Assert.That(async () => await enumeration).Throws<OperationCanceledException>();
        await Assert.That(aborted.Task).CompletesWithin(AwaitTimeout);
    }

    /// <summary>Verifies a method inherited from a base interface pages like one declared on the interface itself.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InheritedMethod_PagesLikeADeclaredOne()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);

        var items = await CollectAsync(server.CreateClientFor<IDerivedGeneratedPagingApi>().CursorItems());

        await Assert.That(Ids(items)).IsEqualTo(AllIds);
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount);
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
}
