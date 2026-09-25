// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>
/// Tests for <see cref="Reply.From(Func{HttpRequestMessage, CancellationToken, Task{HttpResponseMessage}})"/>: the
/// responder receives the send's own cancellation token, and a body being read through it (directly, or through a
/// <see cref="RequestCapture.Bounded(int)"/> capture) observes cancellation while it is still being copied.
/// </summary>
public sealed class ReplyFromCancellableTests
{
    /// <summary>The base address used by these tests.</summary>
    private const string BaseUrl = "https://api.test";

    /// <summary>The route template used by these tests.</summary>
    private const string UploadTemplate = "/upload";

    /// <summary>The byte limit used by the bounded-capture test.</summary>
    private const int BoundedCaptureLimit = 4096;

    /// <summary>The serializer used to build the streamed request bodies.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new();

    /// <summary>The timeout used by every wait, so a regression fails the test instead of hanging.</summary>
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies the token passed to a cancellable responder is driven by the send's own token: <see cref="HttpClient"/>
    /// links the caller's token with its own timeout before handing it to the message handler, so the responder does
    /// not receive <see cref="CancellationToken.None"/>, but a live, cancelable token tied to this send.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResponderReceivesTheSendsCancellationToken()
    {
        CancellationToken? observed = null;
        var handler = new StubHttp
        {
            {
                Route.Post(UploadTemplate),
                Reply.From((_, cancellationToken) =>
                {
                    observed = cancellationToken;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                })
            },
        };
        using var client = HttpClientTestFactory.Create(handler);
        using var cts = new CancellationTokenSource();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{UploadTemplate}");

        _ = await client.SendAsync(request, cts.Token);

        await Assert.That(observed).IsNotNull();
        await Assert.That(observed!.Value.CanBeCanceled).IsTrue();
        await Assert.That(observed!.Value).IsNotEqualTo(CancellationToken.None);
    }

    /// <summary>
    /// Verifies a cancellable responder that copies a streaming body observes cancellation while the copy is in
    /// progress, mid-body, rather than only before or after it.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CancellationDuringResponderBodyCopyIsObserved()
    {
        var firstItemReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new StubHttp
        {
            {
                Route.Post(UploadTemplate),
                Reply.From(static async (request, cancellationToken) =>
                {
                    await using var target = new MemoryStream();
                    await request.Content!.CopyToAsync(target, cancellationToken);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                })
            },
        };
        handler.RequestCapture = RequestCapture.None;
        using var client = HttpClientTestFactory.Create(handler);
        using var cts = new CancellationTokenSource();
        var content = new JsonLinesContent<User>(GatedItemsAsync(firstItemReleased, cts), Serializer);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{UploadTemplate}") { Content = content };

        var send = client.SendAsync(request, cts.Token);
        try
        {
            await firstItemReleased.Task.WaitAsync(WaitTimeout);
            await cts.CancelAsync();

            await Assert.That(async () => await send).Throws<OperationCanceledException>();
        }
        finally
        {
            content.Dispose();
        }
    }

    /// <summary>Verifies a bounded capture forwards the send's cancellation token so a streaming body read through it observes cancellation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedCaptureForwardsCancellationToTheBody()
    {
        var firstItemReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new StubHttp
        {
            {
                Route.Post(UploadTemplate),
                Reply.From(static async (request, cancellationToken) =>
                {
                    await using var target = new MemoryStream();
                    await request.Content!.CopyToAsync(target, cancellationToken);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                })
            },
        };
        handler.RequestCapture = RequestCapture.Bounded(BoundedCaptureLimit);
        using var client = HttpClientTestFactory.Create(handler);
        using var cts = new CancellationTokenSource();
        var content = new JsonLinesContent<User>(GatedItemsAsync(firstItemReleased, cts), Serializer);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{UploadTemplate}") { Content = content };

        var send = client.SendAsync(request, cts.Token);
        try
        {
            await firstItemReleased.Task.WaitAsync(WaitTimeout);
            await cts.CancelAsync();

            await Assert.That(async () => await send).Throws<OperationCanceledException>();
        }
        finally
        {
            content.Dispose();
        }
    }

    /// <summary>Produces one user, then blocks until cancelled, signalling the test once the first item has been released.</summary>
    /// <param name="firstItemReleased">Completed once the first item has been yielded.</param>
    /// <param name="cts">The token source whose cancellation stops the sequence.</param>
    /// <returns>The asynchronous sequence.</returns>
    private static async IAsyncEnumerable<User> GatedItemsAsync(TaskCompletionSource firstItemReleased, CancellationTokenSource cts)
    {
        yield return new(1, "a");
        _ = firstItemReleased.TrySetResult();
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
}
