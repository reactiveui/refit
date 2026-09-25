// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>
/// End-to-end tests for <see cref="Reply.Stream(StreamSource)"/>, <see cref="Reply.JsonLines{T}"/> and
/// <see cref="Reply.ServerSentEvents{T}"/> through a real Refit client streaming <c>IAsyncEnumerable&lt;User&gt;</c>,
/// covering deterministic release-by-release delivery, cancellation, disposal and connection failures.
/// </summary>
public sealed class StreamingReplyTests
{
    /// <summary>The base address the sample client sends requests to.</summary>
    private const string BaseUrl = "https://api.test";

    /// <summary>The route template mirroring <see cref="IUserApi.StreamUsersAsync"/>.</summary>
    private const string StreamTemplate = "/users/stream";

    /// <summary>The identifier of the first sample user.</summary>
    private const int FirstId = 1;

    /// <summary>The identifier of the second sample user.</summary>
    private const int SecondId = 2;

    /// <summary>The identifier of the third sample user.</summary>
    private const int ThirdId = 3;

    /// <summary>A two-item sample sequence reused by the reusable-route test.</summary>
    private static readonly User[] TwoUsers = [new(FirstId, "a"), new(SecondId, "b")];

    /// <summary>A three-item sample sequence reused by the server-sent-events test.</summary>
    private static readonly User[] ThreeUsers = [new(FirstId, "a"), new(SecondId, "b"), new(ThirdId, "c")];

    /// <summary>The expected identifiers for <see cref="TwoUsers"/>.</summary>
    private static readonly int[] TwoIds = [FirstId, SecondId];

    /// <summary>The expected identifiers for <see cref="ThreeUsers"/>.</summary>
    private static readonly int[] ThreeIds = [FirstId, SecondId, ThirdId];

    /// <summary>Verifies items arrive one at a time, only after each is released, for every streaming format.</summary>
    /// <param name="format">The streaming framing under test.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(StreamingContentFormat.JsonLines)]
    [Arguments(StreamingContentFormat.ServerSentEvents)]
    [Arguments(StreamingContentFormat.JsonArray)]
    public async Task ItemsArriveOnlyAfterRelease(StreamingContentFormat format)
    {
        var source = new StreamSource(format);
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.Stream(source) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        await using var enumerator = api.StreamUsersAsync(CancellationToken.None).GetAsyncEnumerator();

        source.Release(new User(FirstId, "a"));
        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
        await Assert.That(enumerator.Current.Id).IsEqualTo(FirstId);

        var second = enumerator.MoveNextAsync();
        await Task.Yield();
        await Assert.That(second.IsCompleted).IsFalse();

        source.Release(new User(SecondId, "b"));
        await Assert.That(await second).IsTrue();
        await Assert.That(enumerator.Current.Id).IsEqualTo(SecondId);

        source.Complete();
        await Assert.That(await enumerator.MoveNextAsync()).IsFalse();
    }

    /// <summary>Verifies <see cref="Reply.JsonLines{T}"/> answers a reusable route with a fresh copy of the items on every request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonLinesReusableRouteAnswersEachRequestFresh()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = StreamTemplate, Reusable = true }, Reply.JsonLines(TwoUsers) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        var first = await CollectAsync(api.StreamUsersAsync(CancellationToken.None));
        var second = await CollectAsync(api.StreamUsersAsync(CancellationToken.None));

        await Assert.That(first.Select(static u => u.Id)).IsEquivalentTo(TwoIds);
        await Assert.That(second.Select(static u => u.Id)).IsEquivalentTo(TwoIds);
    }

    /// <summary>Verifies <see cref="Reply.ServerSentEvents{T}"/> streams all items in order and then completes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ServerSentEventsStreamsAllItemsInOrder()
    {
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.ServerSentEvents(ThreeUsers) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        var users = await CollectAsync(api.StreamUsersAsync(CancellationToken.None));

        await Assert.That(users.Select(static u => u.Id)).IsEquivalentTo(ThreeIds);
    }

    /// <summary>Verifies cancelling the token during a stalled read throws and closes the source.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellationDuringStalledReadClosesSource()
    {
        var source = new StreamSource();
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.Stream(source) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);
        using var cts = new CancellationTokenSource();

        var enumerator = api.StreamUsersAsync(cts.Token).GetAsyncEnumerator();
        var moveNext = enumerator.MoveNextAsync();

        await Task.Yield();
        await cts.CancelAsync();

        await Assert.That(async () => await moveNext).Throws<OperationCanceledException>();
        await source.Closed;
        await Assert.That(source.IsClosed).IsTrue();
    }

    /// <summary>Verifies breaking out of an <c>await foreach</c> disposes the enumerator and closes the source.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BreakingOutOfForeachClosesSource()
    {
        var source = new StreamSource();
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.Stream(source) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        source.Release(new User(FirstId, "a"));
        source.Release(new User(SecondId, "b"));

        await foreach (var user in api.StreamUsersAsync(CancellationToken.None))
        {
            await Assert.That(user.Id).IsEqualTo(FirstId);
            if (user.Id == FirstId)
            {
                break;
            }
        }

        await Assert.That(source.IsClosed).IsTrue();
    }

    /// <summary>Verifies disposing the enumerator directly closes the source.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisposingEnumeratorClosesSource()
    {
        var source = new StreamSource();
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.Stream(source) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        var enumerator = api.StreamUsersAsync(CancellationToken.None).GetAsyncEnumerator();
        source.Release(new User(FirstId, "a"));
        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();

        await enumerator.DisposeAsync();

        await Assert.That(source.IsClosed).IsTrue();
    }

    /// <summary>Verifies <see cref="StreamSource.Disconnect()"/> delivers the released items and then an I/O failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisconnectAfterTwoItemsThenIOException()
    {
        var source = new StreamSource();
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.Stream(source) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        var enumerator = api.StreamUsersAsync(CancellationToken.None).GetAsyncEnumerator();

        source.Release(new User(FirstId, "a"));
        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();

        source.Release(new User(SecondId, "b"));
        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();

        source.Disconnect();

#if NET8_0_OR_GREATER
        await Assert.That(async () => await enumerator.MoveNextAsync()).ThrowsExactly<HttpIOException>();
#else
        await Assert.That(async () => await enumerator.MoveNextAsync()).Throws<IOException>();
#endif
    }

    /// <summary>Verifies <see cref="StreamSource.Fail(Exception)"/> surfaces the custom exception to the client.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FailSurfacesCustomException()
    {
        var source = new StreamSource();
        var handler = new StubHttp { { Route.Get(StreamTemplate), Reply.Stream(source) } };
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        var enumerator = api.StreamUsersAsync(CancellationToken.None).GetAsyncEnumerator();
        source.Fail(new InvalidTimeZoneException("custom failure"));

        await Assert.That(async () => await enumerator.MoveNextAsync()).ThrowsExactly<InvalidTimeZoneException>();
    }

    /// <summary>Verifies the response headers (status and content type) arrive while the body read is still stalled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeadersArriveWhileBodyStalls()
    {
        const string StalledTemplate = "/stalled";
        var source = new StreamSource();
        var handler = new StubHttp { { Route.Get(StalledTemplate), Reply.Stream(source) } };
        using var client = HttpClientTestFactory.Create(handler, new(BaseUrl));
        using var request = new HttpRequestMessage(HttpMethod.Get, StalledTemplate);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo(JsonLinesContent.JsonLinesMediaType);

        var bodyRead = response.Content.ReadAsStringAsync();
        await Task.Yield();
        await Assert.That(bodyRead.IsCompleted).IsFalse();

        source.Complete();
        await Assert.That(await bodyRead).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies <see cref="Reply.Stream(StreamSource)"/> rejects a null source.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReplyStreamRejectsNullSource() =>
        await Assert.That(static () => Reply.Stream(null!)).Throws<ArgumentNullException>();

    /// <summary>Verifies <see cref="Reply.JsonLines{T}"/> rejects a null item sequence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReplyJsonLinesRejectsNullItems() =>
        await Assert.That(static () => Reply.JsonLines<User>(null!)).Throws<ArgumentNullException>();

    /// <summary>Verifies <see cref="Reply.ServerSentEvents{T}"/> rejects a null item sequence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReplyServerSentEventsRejectsNullItems() =>
        await Assert.That(static () => Reply.ServerSentEvents<User>(null!)).Throws<ArgumentNullException>();

    /// <summary>Collects an asynchronous sequence into a list.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The sequence to collect.</param>
    /// <returns>The collected items, in order.</returns>
    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> items)
    {
        var result = new List<T>();
        await foreach (var item in items)
        {
            result.Add(item);
        }

        return result;
    }
}
