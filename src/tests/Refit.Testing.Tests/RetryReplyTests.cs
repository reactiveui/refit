// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>Checks fresh retry responses and validation for reusable stubs.</summary>
public class RetryReplyTests
{
    /// <summary>The standard retry header.</summary>
    private const string RetryAfterHeader = "Retry-After";

    /// <summary>The stubbed request URI.</summary>
    private const string Endpoint = "https://api.test/retry";

    /// <summary>Verifies fractional intervals round up and zero intervals remain valid.</summary>
    /// <param name="milliseconds">The server interval.</param>
    /// <param name="expected">The whole-second header.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(1500, "2")]
    [Arguments(0, "0")]
    public async Task After_RoundsUpSeconds(int milliseconds, string expected, CancellationToken cancellationToken)
    {
        using var stub = new StubHttp { { Route.Get(Endpoint), RetryReply.After(TimeSpan.FromMilliseconds(milliseconds), HttpStatusCode.ServiceUnavailable) } };
        using var client = HttpClientTestFactory.Create(stub);

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(response.Headers.GetValues(RetryAfterHeader).Single()).IsEqualTo(expected);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies date replies use UTC HTTP dates.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task After_FormatsUtcDate(CancellationToken cancellationToken)
    {
        using var stub = new StubHttp { { Route.Get(Endpoint), RetryReply.After(DateTimeOffset.UnixEpoch) } };
        using var client = HttpClientTestFactory.Create(stub);

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(response.Headers.RetryAfter!.Date).IsEqualTo(DateTimeOffset.UnixEpoch);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    /// <summary>Verifies reusable routes create fresh messages after prior responses are disposed.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Header_ProducesFreshResponseOnEveryCall(CancellationToken cancellationToken)
    {
        using var stub = new StubHttp { { new RouteMatcher { Method = HttpMethod.Get, Template = Endpoint, Reusable = true }, RetryReply.Header("x-ms-retry-after-ms", "1500") } };
        using var client = HttpClientTestFactory.Create(stub);
        var first = await client.GetAsync(new Uri(Endpoint), cancellationToken);
        first.Dispose();

        using var second = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(second.Headers.GetValues("x-ms-retry-after-ms").Single()).IsEqualTo("1500");
    }

    /// <summary>Verifies malformed header values can be used to test fallback behavior.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Header_PreservesInvalidHint(CancellationToken cancellationToken)
    {
        using var stub = new StubHttp { { Route.Get(Endpoint), RetryReply.Header(RetryAfterHeader, "invalid") } };
        using var client = HttpClientTestFactory.Create(stub);

        using var response = await client.GetAsync(new Uri(Endpoint), cancellationToken);

        await Assert.That(response.Headers.GetValues(RetryAfterHeader).Single()).IsEqualTo("invalid");
    }

    /// <summary>Verifies invalid template arguments fail before sending.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Factories_ValidateArguments()
    {
        using (Assert.Multiple())
        {
            await Assert.That(static () => RetryReply.After(TimeSpan.FromTicks(-1))).Throws<ArgumentOutOfRangeException>();
            await Assert.That(static () => RetryReply.Header(string.Empty, "1")).Throws<ArgumentException>();
            await Assert.That(static () => RetryReply.Header("X", null!)).Throws<ArgumentNullException>();
        }
    }
}
