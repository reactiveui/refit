// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;

namespace Refit.Testing.Tests;

/// <summary>Checks retry intervals with explicitly advanced clocks.</summary>
public partial class HttpRetryHandlerTests
{
    /// <summary>Verifies exponential backoff is capped and has a deterministic clock.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_ExponentialBackoffIsCapped(CancellationToken cancellationToken)
    {
        var clock = new ObservedRetryTimeProvider();
        var stub = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = Endpoint, Reusable = true }, Reply.Status(HttpStatusCode.ServiceUnavailable) }, };
        var options = ImmediateOptions();
        options.MaxRetries = ThreeAttempts;
        options.BaseDelay = RetryInterval;
        options.MaxDelay = RetryInterval + RetryInterval;
        options.TimeProvider = clock;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var send = client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(await clock.AdvanceNextAsync(cancellationToken)).IsEqualTo(RetryInterval);
        await Assert.That(await clock.AdvanceNextAsync(cancellationToken)).IsEqualTo(RetryInterval + RetryInterval);
        await Assert.That(await clock.AdvanceNextAsync(cancellationToken)).IsEqualTo(RetryInterval + RetryInterval);
        using var response = await send;

        await Assert.That(stub.Requests.Count).IsEqualTo(options.MaxRetries + 1);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>Verifies full jitter remains within the configured fallback interval.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_JitterStaysWithinBackoff(CancellationToken cancellationToken)
    {
        var clock = new ObservedRetryTimeProvider();
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.ServiceUnavailable) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) } };
        var options = ImmediateOptions();
        options.BaseDelay = RetryInterval;
        options.UseJitter = true;
        options.TimeProvider = clock;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var send = client.GetAsync(new Uri(Endpoint), cancellationToken);

        // Advance to the entire cap, including the valid zero-jitter case that schedules no timer.
        clock.Advance(RetryInterval);
        using var response = await send;

        await Assert.That(stub.Requests.Count).IsEqualTo(TwoAttempts);
        await Assert.That(clock.ScheduledIntervals.TrueForAll(static interval => interval <= RetryInterval && interval >= TimeSpan.Zero)).IsTrue();
    }

    /// <summary>Verifies GitHub forbidden responses retry only with a valid exhaustion hint.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_GitHubQuotaHintMakesForbiddenRetryable(CancellationToken cancellationToken)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var stub = new StubHttp
        {
            {
                Route.Get(Endpoint), Reply.From(static _ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
                response.Headers.Add("x-ratelimit-remaining", "0");
                response.Headers.Add("x-ratelimit-reset", "5");
                return response;
            })
            },
            { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) },
        };
        var options = ImmediateOptions();
        options.TimeProvider = clock;
        options.DelayProvider = RetryDelayProviders.GitHub;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var send = client.GetAsync(new Uri(Endpoint), cancellationToken);
        await Assert.That(send.IsCompleted).IsFalse();
        clock.Advance(RetryInterval);
        using var response = await send;

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>A fake clock that signals when a retry timer is fully registered.</summary>
    private sealed class ObservedRetryTimeProvider : FakeTimeProvider
    {
        /// <summary>The intervals awaiting explicit advancement.</summary>
        private readonly Channel<TimeSpan> _scheduled = Channel.CreateUnbounded<TimeSpan>();

        /// <summary>Gets the observed scheduled intervals.</summary>
        public List<TimeSpan> ScheduledIntervals { get; } = [];

        /// <inheritdoc/>
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            ScheduledIntervals.Add(dueTime);
            _ = _scheduled.Writer.TryWrite(dueTime);
            return timer;
        }

        /// <summary>Waits for a registered timer and advances exactly to its deadline.</summary>
        /// <param name="cancellationToken">The test cancellation token.</param>
        /// <returns>The elapsed interval.</returns>
        public async Task<TimeSpan> AdvanceNextAsync(CancellationToken cancellationToken)
        {
            var interval = await _scheduled.Reader.ReadAsync(cancellationToken);
            Advance(interval);
            return interval;
        }
    }
}
