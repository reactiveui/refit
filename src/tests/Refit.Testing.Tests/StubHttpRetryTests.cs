// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Time.Testing;

namespace Refit.Testing.Tests;

/// <summary>Demonstrates retries over generated Refit clients without a network or real-time waits.</summary>
public class StubHttpRetryTests
{
    /// <summary>The sample user identifier.</summary>
    private const int UserId = 7;

    /// <summary>The expected request count after recovery.</summary>
    private const int ExpectedAttempts = 2;

    /// <summary>The raw HTTP retry route.</summary>
    private const string Endpoint = "https://api.test/retry";

    /// <summary>The delay supplied by the stub.</summary>
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

    /// <summary>Verifies default retry wrappers share the stub clock and work through generated clients.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateRetryHandler_SharesStubClockForGeneratedClient(CancellationToken cancellationToken)
    {
        var clock = new FakeTimeProvider();
        using var stub = new StubHttp { TimeProvider = clock };
        stub.Add(Route.Get("/users/{id}"), RetryReply.After(RetryInterval));
        stub.Add(Route.Get("/users/{id}"), Reply.With(new User(UserId, "octocat")));
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(), new("https://api.test"));
        var api = RestService.ForGenerated<IUserApi>(client);

        var send = api.GetUser(UserId);
        await Assert.That(send.IsCompleted).IsFalse();
        clock.Advance(RetryInterval);
        var user = await send.WaitAsync(cancellationToken);

        await Assert.That(user.Login).IsEqualTo("octocat");
        await Assert.That(stub.Requests.Count).IsEqualTo(ExpectedAttempts);
        await Assert.That(stub.Requests[0].Options.TryGetValue(new HttpRequestOptionsKey<Type>(HttpRequestMessageOptions.InterfaceType), out var interfaceType)).IsTrue();
        await Assert.That(interfaceType).IsEqualTo(typeof(IUserApi));
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }

    /// <summary>Verifies explicit options keep their own clock rather than the stub clock.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateRetryHandler_RespectsExplicitClock(CancellationToken cancellationToken)
    {
        var clock = new FakeTimeProvider();
        using var stub = new StubHttp { { Route.Get(Endpoint), RetryReply.After(RetryInterval) }, { Route.Get(Endpoint), Reply.Status(HttpStatusCode.OK) }, };
        using var client = HttpClientTestFactory.Create(stub.CreateRetryHandler(new() { TimeProvider = clock }));

        var send = client.GetAsync(new Uri(Endpoint), cancellationToken);
        await Assert.That(send.IsCompleted).IsFalse();
        clock.Advance(RetryInterval);
        using var response = await send;

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await stub.VerifyAllCalledAsync().WaitAsync(cancellationToken);
    }
}
