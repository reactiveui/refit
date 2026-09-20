// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>Verifies the <see cref="IObservable{T}"/> forms of a paged sequence.</summary>
public partial class PagedEnumerableTests
{
    /// <summary>Verifies the item observable emits every item in order, completes, and tolerates repeated disposal.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToObservable_EmitsEveryItemThenCompletes()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var observer = new RecordingObserver<PagedItem>();

        var subscription = CursorItems(server.CreateClient()).ToObservable().Subscribe(observer);
        await Assert.That(observer.Terminated.Task).CompletesWithin(AwaitTimeout);
        subscription.Dispose();
        subscription.Dispose();

        await Assert.That(Ids(observer.Snapshot())).IsEqualTo(AllIds);
        await Assert.That(observer.Completed).IsTrue();
        await Assert.That(observer.Error).IsNull();
    }

    /// <summary>Verifies the observable is cold: nothing is requested before subscribing, and each subscription starts over.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToObservable_IsColdAndStartsOverForEverySubscription()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var observable = CursorItems(server.CreateClient()).ToObservable();
        await Assert.That(server.Requests.Count).IsEqualTo(0);

        var first = new RecordingObserver<PagedItem>();
        using var firstSubscription = observable.Subscribe(first);
        await Assert.That(first.Terminated.Task).CompletesWithin(AwaitTimeout);
        var second = new RecordingObserver<PagedItem>();
        using var secondSubscription = observable.Subscribe(second);
        await Assert.That(second.Terminated.Task).CompletesWithin(AwaitTimeout);

        await Assert.That(Ids(second.Snapshot())).IsEqualTo(Ids(first.Snapshot()));
        await Assert.That(server.Requests.Count).IsEqualTo(PageCount * EnumerationsCompared);
    }

    /// <summary>Verifies disposing a subscription while a page request is in flight aborts it and delivers no terminal notification.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToObservable_DisposeDuringAPage_AbortsTheRequestInFlight()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var inFlight = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.OnRequest = BlockSecondPageUntilCancelled(inFlight, aborted);
        var observer = new RecordingObserver<PagedItem>();

        var subscription = CursorItems(server.CreateClient()).ToObservable().Subscribe(observer);
        await Assert.That(inFlight.Task).CompletesWithin(AwaitTimeout);
        subscription.Dispose();

        await Assert.That(aborted.Task).CompletesWithin(AwaitTimeout);
        await Assert.That(server.Requests.Count).IsEqualTo(RequestsThroughSecondPage);
        await Assert.That(observer.Completed).IsFalse();
        await Assert.That(observer.Error).IsNull();
    }

    /// <summary>Verifies a failing page reaches the observer as an error after the items that preceded it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToObservable_FailingPage_ReachesOnErrorAfterEarlierItems()
    {
        var server = new PagedApiHandler(TotalItems, PageSize) { StatusForPage = static page => page == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK };
        var observer = new RecordingObserver<PagedItem>();

        using var subscription = OffsetItems(server.CreateClient()).ToObservable().Subscribe(observer);
        await Assert.That(observer.Terminated.Task).CompletesWithin(AwaitTimeout);

        await Assert.That(Ids(observer.Snapshot())).IsEqualTo("1,2,3");
        await Assert.That(observer.Error).IsTypeOf<ApiException>();
        await Assert.That(observer.Completed).IsFalse();
    }

    /// <summary>Verifies the page observable emits the API's own page objects.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToPageObservable_EmitsTheApiPages()
    {
        var server = new PagedApiHandler(TotalItems, PageSize);
        var observer = new RecordingObserver<CursorPage>();

        using var subscription = CursorItems(server.CreateClient()).ToPageObservable().Subscribe(observer);
        await Assert.That(observer.Terminated.Task).CompletesWithin(AwaitTimeout);

        await Assert.That(string.Join(",", observer.Snapshot().ConvertAll(static page => page.NextCursor ?? "none"))).IsEqualTo("c1,c2,none");
        await Assert.That(observer.Completed).IsTrue();
    }

    /// <summary>Verifies subscribing a null observer is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToObservable_NullObserver_Throws()
    {
        var observable = CursorItems(new PagedApiHandler(TotalItems, PageSize).CreateClient()).ToObservable();

        await Assert.That(() => observable.Subscribe(null!)).Throws<ArgumentNullException>();
    }
}
