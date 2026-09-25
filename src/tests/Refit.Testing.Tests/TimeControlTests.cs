// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Time.Testing;

namespace Refit.Testing.Tests;

/// <summary>
/// Deterministic tests for <see cref="StubHttp.TimeProvider"/> driving <see cref="NetworkBehavior"/> delays and
/// <see cref="StubHttp.VerifyAllCalledAsync(TimeSpan)"/> timeouts through a <see cref="FakeTimeProvider"/>, with no
/// real-time waits.
/// </summary>
public sealed class TimeControlTests
{
    /// <summary>The endpoint stubbed by these tests.</summary>
    private const string Endpoint = "https://api/thing";

    /// <summary>The simulated network delay applied by <see cref="NetworkDelayWaitsForFakeClockAdvance"/>.</summary>
    private static readonly TimeSpan NetworkDelay = TimeSpan.FromSeconds(5);

    /// <summary>A verification timeout comfortably longer than any test setup delay.</summary>
    private static readonly TimeSpan AmpleTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The verification timeout advanced past in <see cref="VerifyAllCalledFaultsOnlyAfterTimeoutAdvance"/>.</summary>
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Verifies a network delay does not complete the send until the fake clock is advanced past it.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NetworkDelayWaitsForFakeClockAdvance()
    {
        var fake = new FakeTimeProvider();
        var behavior = new NetworkBehavior { Delay = NetworkDelay, Variance = 0D, FailurePercent = 0D, ErrorPercent = 0D };
        var handler = new StubHttp(behavior) { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) }, };
        handler.TimeProvider = fake;
        using var client = HttpClientTestFactory.Create(handler);

        var send = client.GetAsync(new Uri(Endpoint));

        await Task.Yield();
        await Assert.That(send.IsCompleted).IsFalse();

        fake.Advance(NetworkDelay);
        var response = await send;

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Verifies <see cref="StubHttp.VerifyAllCalledAsync(TimeSpan)"/> succeeds once the request lands before the timeout elapses.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task VerifyAllCalledSucceedsWhenRequestArrivesBeforeTimeout()
    {
        var fake = new FakeTimeProvider();
        var handler = new StubHttp { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) }, };
        handler.TimeProvider = fake;
        using var client = HttpClientTestFactory.Create(handler);

        _ = await client.GetAsync(new Uri(Endpoint));

        await handler.VerifyAllCalledAsync(AmpleTimeout);
    }

    /// <summary>Verifies <see cref="StubHttp.VerifyAllCalledAsync(TimeSpan)"/> only faults once the fake clock is advanced past the timeout.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task VerifyAllCalledFaultsOnlyAfterTimeoutAdvance()
    {
        var fake = new FakeTimeProvider();
        var handler = new StubHttp { { Route.Get("https://api/never"), Reply.Status(HttpStatusCode.OK) }, };
        handler.TimeProvider = fake;

        var verify = handler.VerifyAllCalledAsync(ShortTimeout);

        await Task.Yield();
        await Assert.That(verify.IsCompleted).IsFalse();

        fake.Advance(ShortTimeout);

        await Assert.That(async () => await verify).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies assigning a null <see cref="StubHttp.TimeProvider"/> throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullTimeProviderThrows()
    {
        var handler = new StubHttp();

        await Assert.That(() => handler.TimeProvider = null!).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies <see cref="StubHttp.TimeProvider"/> defaults to <see cref="TimeProvider.System"/> and reflects an assigned value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TimeProviderGetterReflectsDefaultAndAssignedValue()
    {
        var handler = new StubHttp();

        await Assert.That(handler.TimeProvider).IsSameReferenceAs(TimeProvider.System);

        var fake = new FakeTimeProvider();
        handler.TimeProvider = fake;

        await Assert.That(handler.TimeProvider).IsSameReferenceAs(fake);
    }

    /// <summary>Verifies assigning a null <see cref="StubHttp.RequestCapture"/> throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullRequestCaptureThrows()
    {
        var handler = new StubHttp();

        await Assert.That(() => handler.RequestCapture = null!).Throws<ArgumentNullException>();
    }
}
