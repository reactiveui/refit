// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Time.Testing;

namespace Refit.Testing.Tests;

/// <summary>Exercises the real retry handler against deterministic HTTP responses.</summary>
public partial class HttpRetryHandlerTests
{
    /// <summary>The route used by raw HTTP tests.</summary>
    private const string Endpoint = "https://api.test/retry";

    /// <summary>The expected request count after one retry.</summary>
    private const int TwoAttempts = 2;

    /// <summary>The expected request count after two retries.</summary>
    private const int ThreeAttempts = 3;

    /// <summary>The replayed content bytes.</summary>
    private const string Payload = "payload";

    /// <summary>The virtual delay used by timing tests.</summary>
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    /// <summary>Verifies transient statuses are retried and permanent failures are not.</summary>
    /// <param name="status">The first response status.</param>
    /// <param name="expectedAttempts">The expected number of sends.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(HttpStatusCode.RequestTimeout, 2)]
    [Arguments(HttpStatusCode.TooManyRequests, 2)]
    [Arguments(HttpStatusCode.InternalServerError, 2)]
    [Arguments(HttpStatusCode.BadGateway, 2)]
    [Arguments(HttpStatusCode.ServiceUnavailable, 2)]
    [Arguments(HttpStatusCode.GatewayTimeout, 2)]
    [Arguments(HttpStatusCode.Forbidden, 1)]
    [Arguments(HttpStatusCode.Unauthorized, 1)]
    [Arguments(HttpStatusCode.NotFound, 1)]
    [Arguments(HttpStatusCode.NotImplemented, 1)]
    [Arguments(HttpStatusCode.HttpVersionNotSupported, 1)]
    [Arguments(HttpStatusCode.OK, 1)]
    public async Task SendAsync_ClassifiesStatuses(HttpStatusCode status, int expectedAttempts, CancellationToken cancellationToken)
    {
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Status(status) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) } };
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(ImmediateOptions()));

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(stub.Requests.Count).IsEqualTo(expectedAttempts);
    }

    /// <summary>Verifies state-changing methods require opt-in, while safe methods retry by default.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="optIn">Whether unsafe retries are enabled.</param>
    /// <param name="expectedAttempts">The expected number of sends.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("GET", false, 2)]
    [Arguments("HEAD", false, 2)]
    [Arguments("OPTIONS", false, 2)]
    [Arguments("TRACE", false, 2)]
    [Arguments("POST", false, 1)]
    [Arguments("PUT", false, 1)]
    [Arguments("DELETE", false, 1)]
    [Arguments("PATCH", false, 1)]
    [Arguments("POST", true, 2)]
    public async Task SendAsync_RequiresSafeReplay(string method, bool optIn, int expectedAttempts, CancellationToken cancellationToken)
    {
        var stub = new StubHttp { { Route.Any(Endpoint), Reply.Status(HttpStatusCode.ServiceUnavailable) }, { Route.Any(Endpoint), Reply.Status(HttpStatusCode.OK) }, };
        var options = ImmediateOptions();
        options.RetryUnsafeMethods = optIn;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));
        using var request = new HttpRequestMessage(new(method), Endpoint);

        using var response = await client.SendAsync(request, cancellationToken);

        await Assert.That(stub.Requests.Count).IsEqualTo(expectedAttempts);
    }

    /// <summary>Verifies zero retries send the caller's request directly.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_ZeroRetriesBypassesCloning(CancellationToken cancellationToken)
    {
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.ServiceUnavailable) } };
        var options = ImmediateOptions();
        options.MaxRetries = 0;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);

        using var response = await client.SendAsync(request, cancellationToken);

        await Assert.That(stub.Requests.Single()).IsSameReferenceAs(request);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies each attempt uses independent messages with the original headers, options and replayable body.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_ReplaysBodyAndMetadata(CancellationToken cancellationToken)
    {
        var bodies = new List<string>();
        var metadata = new List<bool>();
        var optionKey = new HttpRequestOptionsKey<string>("Refit.Testing.Retry");
        var stub = new StubHttp { RequestCapture = RequestCapture.None };
        var reply = Reply.From(async (request, token) =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync(token));
            metadata.Add(request.Version == HttpVersion.Version20
                && request.VersionPolicy == HttpVersionPolicy.RequestVersionOrLower
                && request.Headers.Contains("X-Request")
                && request.Content.Headers.Contains("X-Content")
                && request.Options.TryGetValue(optionKey, out var value) && value == "metadata");
            return new HttpResponseMessage(bodies.Count == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        });
        stub.Add(Route.Post(Endpoint), reply);
        stub.Add(Route.Post(Endpoint), reply);
        var options = ImmediateOptions();
        options.RetryUnsafeMethods = true;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Content = new StringContent(Payload),
        };
        request.Headers.Add("X-Request", "yes");
        request.Content.Headers.Add("X-Content", "yes");
        request.Options.Set(optionKey, "metadata");

        using var response = await client.SendAsync(request, cancellationToken);

        await Assert.That(bodies).IsEquivalentTo(new List<string> { Payload, Payload });
        await Assert.That(metadata.TrueForAll(static value => value)).IsTrue();
        await Assert.That(stub.Requests[0]).IsNotSameReferenceAs(stub.Requests[1]);
        await Assert.That(stub.Requests[0]).IsNotSameReferenceAs(request);
        await Assert.That(response.RequestMessage).IsSameReferenceAs(request);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies failed responses are disposed before backoff and final responses remain caller-owned.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_DisposesOnlyFailedResponses(CancellationToken cancellationToken)
    {
        var clock = new FakeTimeProvider();
        using var failedContent = new StringContent("failure");
        using var finalContent = new StringContent("success");
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.From(_ => new(HttpStatusCode.ServiceUnavailable) { Content = failedContent }) }, { Route.Get(Endpoint), Reply.Content(finalContent) }, };
        var options = ImmediateOptions();
        options.BaseDelay = RetryInterval;
        options.TimeProvider = clock;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var send = client.GetAsync(new Uri(Endpoint), cancellationToken);
        await Assert.That(send.IsCompleted).IsFalse();
        await Assert.That(async () => await failedContent.ReadAsStringAsync(cancellationToken)).Throws<ObjectDisposedException>();
        clock.Advance(RetryInterval);
        using var response = await send;

        await Assert.That(await response.Content.ReadAsStringAsync(cancellationToken)).IsEqualTo("success");
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies a provider interval replaces backoff and never gets shortened by jitter.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_HonorsServerInterval(CancellationToken cancellationToken)
    {
        var clock = new FakeTimeProvider();
        var stub = new StubHttp { { Route.Get(Endpoint), RetryReply.After(RetryInterval) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) }, };
        var options = ImmediateOptions();
        options.TimeProvider = clock;
        options.UseJitter = true;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var send = client.GetAsync(new Uri(Endpoint), cancellationToken);
        clock.Advance(RetryInterval - TimeSpan.FromTicks(1));
        await Assert.That(stub.Requests.Count).IsEqualTo(1);
        await Assert.That(send.IsCompleted).IsFalse();
        clock.Advance(TimeSpan.FromTicks(1));
        using var response = await send;

        await Assert.That(stub.Requests.Count).IsEqualTo(TwoAttempts);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies excessive or invalid custom delays stop retries rather than waiting less.</summary>
    /// <param name="seconds">The custom server delay.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(-1)]
    [Arguments(31)]
    public async Task SendAsync_RejectsOutOfBudgetHints(int seconds, CancellationToken cancellationToken)
    {
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.TooManyRequests) } };
        var options = ImmediateOptions();
        options.DelayProvider = (_, _, token) =>
        {
            token.ThrowIfCancellationRequested();
            return TimeSpan.FromSeconds(seconds);
        };
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(stub.Requests.Count).IsEqualTo(1);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies a custom response predicate can add or refuse a retryable status.</summary>
    /// <param name="retry">The predicate result.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task SendAsync_UsesCustomPredicate(bool retry, CancellationToken cancellationToken)
    {
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.Conflict) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) } };
        var options = ImmediateOptions();
        options.ShouldRetry = _ => retry;
        options.DelayProvider = null;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(stub.Requests.Count).IsEqualTo(retry ? TwoAttempts : 1);
    }

    /// <summary>Creates an immediate retry configuration for tests without timing assertions.</summary>
    /// <returns>The retry configuration.</returns>
    private static HttpRetryOptions ImmediateOptions() => new() { BaseDelay = TimeSpan.Zero, UseJitter = false, MaxRetries = 1 };
}
