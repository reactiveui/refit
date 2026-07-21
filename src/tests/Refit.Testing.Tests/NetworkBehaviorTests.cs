// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>Unit tests for <see cref="NetworkBehavior"/> fault-injection applied through <see cref="StubHttp"/>.</summary>
public sealed class NetworkBehaviorTests
{
    /// <summary>The endpoint stubbed by these tests.</summary>
    private const string Endpoint = "https://api/thing";

    /// <summary>Verifies a 100% failure rate throws the simulated network failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FailureRateAlwaysThrows()
    {
        var behavior = new NetworkBehavior { Delay = TimeSpan.Zero, FailurePercent = 1D, ErrorPercent = 0D };
        var handler = new StubHttp(behavior)
        {
            {
                Route.Any(Endpoint),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        await Assert.That(async () => _ = await SendAsync(handler, Endpoint)).ThrowsExactly<HttpRequestException>();
    }

    /// <summary>Verifies a 100% error rate returns the configured error status instead of the stubbed response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorRateReturnsErrorStatus()
    {
        var behavior = new NetworkBehavior
        {
            Delay = TimeSpan.Zero,
            FailurePercent = 0D,
            ErrorPercent = 1D,
            ErrorStatusCode = HttpStatusCode.ServiceUnavailable,
        };
        var handler = new StubHttp(behavior)
        {
            {
                Route.Any(Endpoint),
                Reply.Json("{}")
            },
        };

        var response = await SendAsync(handler, Endpoint);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>Verifies no injected faults yields the stubbed response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NoFaultsYieldStubbedResponse()
    {
        var behavior = new NetworkBehavior { Delay = TimeSpan.Zero, FailurePercent = 0D, ErrorPercent = 0D };
        var handler = new StubHttp(behavior)
        {
            {
                Route.Any(Endpoint),
                Reply.Text("ok")
            },
        };

        var response = await SendAsync(handler, Endpoint);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await response.Content.ReadAsStringAsync()).IsEqualTo("ok");
    }

    /// <summary>Verifies the same seed reproduces the same delay sequence.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SeededDelaysAreReproducible()
    {
        const int samples = 5;
        const int seed = 123;
        const double delayVariance = 0.5D;
        var a = new NetworkBehavior(seed) { Delay = TimeSpan.FromSeconds(1), Variance = delayVariance };
        var b = new NetworkBehavior(seed) { Delay = TimeSpan.FromSeconds(1), Variance = delayVariance };

        for (var i = 0; i < samples; i++)
        {
            await Assert.That(a.NextDelay()).IsEqualTo(b.NextDelay());
        }
    }

    /// <summary>Verifies the failure/error probabilities honor the 0 and 1 boundaries.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ProbabilityBoundariesAreHonored()
    {
        var always = new NetworkBehavior { FailurePercent = 1D, ErrorPercent = 1D };
        var never = new NetworkBehavior { FailurePercent = 0D, ErrorPercent = 0D };

        await Assert.That(always.NextIsFailure()).IsTrue();
        await Assert.That(always.NextIsError()).IsTrue();
        await Assert.That(never.NextIsFailure()).IsFalse();
        await Assert.That(never.NextIsError()).IsFalse();
    }

    /// <summary>Sends a GET request through a handler and returns the response.</summary>
    /// <param name="handler">The handler under test.</param>
    /// <param name="url">The request URL.</param>
    /// <returns>The response produced by the handler.</returns>
    private static async Task<HttpResponseMessage> SendAsync(StubHttp handler, string url)
    {
        using var client = HttpClientTestFactory.Create(handler);
        return await client.GetAsync(new Uri(url));
    }
}
