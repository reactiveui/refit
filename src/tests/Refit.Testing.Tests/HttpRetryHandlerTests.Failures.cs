// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Time.Testing;

namespace Refit.Testing.Tests;

/// <summary>Checks transport failures, cancellation and configuration boundaries.</summary>
public partial class HttpRetryHandlerTests
{
    /// <summary>The injected transport exception message.</summary>
    private const string TransientFailure = "transient";

    /// <summary>Verifies retries recover from a transport failure using a fresh request.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_RetriesTransportFailure(CancellationToken cancellationToken)
    {
        var failure = new HttpRequestException(TransientFailure);
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.From(HttpResponseMessage (_) => throw failure) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) }, };
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(ImmediateOptions()));

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(stub.Requests.Count).IsEqualTo(TwoAttempts);
        await Assert.That(stub.Requests[0]).IsNotSameReferenceAs(stub.Requests[1]);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies transport failures stop at the configured limit or when disabled.</summary>
    /// <param name="enabled">Whether transport retries are enabled.</param>
    /// <param name="expectedAttempts">The expected number of sends.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 2)]
    public async Task SendAsync_ExhaustedTransportFailurePropagates(bool enabled, int expectedAttempts, CancellationToken cancellationToken)
    {
        var failure = new HttpRequestException(TransientFailure);
        var stub = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = Endpoint, Reusable = true }, Reply.From(HttpResponseMessage (_) => throw failure) } };
        var options = ImmediateOptions();
        options.RetryHttpRequestExceptions = enabled;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var thrown = await Assert.That(async () => await client.GetAsync(new Uri(Endpoint), cancellationToken)).Throws<HttpRequestException>();

        await Assert.That(thrown).IsSameReferenceAs(failure);
        await Assert.That(stub.Requests.Count).IsEqualTo(expectedAttempts);
    }

    /// <summary>Verifies caller cancellation aborts backoff without sending the next request.</summary>
    /// <param name="transportFailure">Whether the first send throws rather than returning a response.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SendAsync_CancellationAbortsWait(bool transportFailure, CancellationToken cancellationToken)
    {
        var clock = new FakeTimeProvider();
        var stub = new StubHttp
        {
            { Route.Get(Endpoint), transportFailure ? Reply.From(static HttpResponseMessage (_) => throw new HttpRequestException(TransientFailure)) : RetryReply.After(RetryInterval) },
        };
        var options = ImmediateOptions();
        options.BaseDelay = RetryInterval;
        options.TimeProvider = clock;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var send = client.GetAsync(new Uri(Endpoint), cancellation.Token);
        await Assert.That(send.IsCompleted).IsFalse();
        await cancellation.CancelAsync();

        await Assert.That(async () => await send).Throws<OperationCanceledException>();
        await Assert.That(stub.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies a cancelled send or unrelated exception is never retried.</summary>
    /// <param name="kind">The injected failure category.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("cancellation")]
    [Arguments("other")]
    [Arguments("cancelledTransport")]
    public async Task SendAsync_DoesNotRetryNonTransientExceptions(string kind, CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stub = new StubHttp
        {
            {
                Route.Get(Endpoint), Reply.From(async (_, _) =>
            {
                if (kind == "cancelledTransport")
                {
                    await cancellation.CancelAsync();
                    throw new HttpRequestException("cancelled");
                }

                throw kind == "cancellation" ? new OperationCanceledException(cancellation.Token) : new InvalidOperationException("permanent");
            })
            },
        };
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(ImmediateOptions()));

        await Assert.That(async () => await client.GetAsync(new Uri(Endpoint), cancellation.Token)).Throws<Exception>();
        await Assert.That(stub.Requests.Count).IsEqualTo(1);
    }

    /// <summary>Verifies a failed callback disposes the response before propagating its error.</summary>
    /// <param name="predicate">Whether the predicate rather than delay reader fails.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task SendAsync_CallbackFailureDisposesResponse(bool predicate, CancellationToken cancellationToken)
    {
        using var content = new StringContent("failure");
        var failure = new InvalidOperationException("callback");
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Content(content) } };
        var options = ImmediateOptions();
        if (predicate)
        {
            options.ShouldRetry = _ => throw failure;
        }
        else
        {
            options.DelayProvider = (_, _, _) => throw failure;
        }

        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));

        var thrown = await Assert.That(async () => await client.GetAsync(new Uri(Endpoint), cancellationToken)).Throws<InvalidOperationException>();

        await Assert.That(thrown).IsSameReferenceAs(failure);
        await Assert.That(async () => await content.ReadAsStringAsync(cancellationToken)).Throws<ObjectDisposedException>();
    }

    /// <summary>Verifies an oversized streamed body fails before the first network send.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SendAsync_RejectsOversizedBodyBeforeSending(CancellationToken cancellationToken)
    {
        var stub = new StubHttp();
        var options = ImmediateOptions();
        options.RetryUnsafeMethods = true;
        options.MaxRequestContentBufferSize = 1;
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = new StringContent("oversized") };

        await Assert.That(async () => await client.SendAsync(request, cancellationToken)).Throws<HttpRequestException>();
        await Assert.That(stub.Requests.Count).IsEqualTo(0);
    }

    /// <summary>Verifies the handler snapshots options instead of reading later mutations.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Constructor_SnapshotsOptions(CancellationToken cancellationToken)
    {
        var options = ImmediateOptions();
        var stub = new StubHttp { { Route.Get(Endpoint), Reply.Status(HttpStatusCode.ServiceUnavailable) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) } };
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(options));
        options.MaxRetries = 0;
        options.ShouldRetry = static _ => false;

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(stub.Requests.Count).IsEqualTo(TwoAttempts);
    }

    /// <summary>Verifies default construction and validation of retry options.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Constructor_ValidatesOptions()
    {
        using var handler = new HttpRetryHandler();
        const int invalidMaxDelayDays = 60;
        using (Assert.Multiple())
        {
            await Assert.That(static () => new HttpRetryHandler(null!)).Throws<ArgumentNullException>();
            await Assert.That(static () => new HttpRetryHandler(new() { MaxRetries = -1 })).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => new HttpRetryHandler(new() { BaseDelay = TimeSpan.FromTicks(-1) })).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => new HttpRetryHandler(new() { MaxDelay = TimeSpan.FromTicks(-1) })).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => new HttpRetryHandler(new() { MaxDelay = TimeSpan.FromDays(invalidMaxDelayDays) })).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => new HttpRetryHandler(new() { BaseDelay = TimeSpan.MaxValue })).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => new HttpRetryHandler(new() { MaxRequestContentBufferSize = 0 })).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => new HttpRetryHandler(new() { TimeProvider = null! })).Throws<ArgumentNullException>();
        }
    }
}
