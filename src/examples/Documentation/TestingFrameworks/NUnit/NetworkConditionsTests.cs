// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Refit.Testing;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>Testing a slow or broken network, without a real slow or broken network.</summary>
[TestFixture]
public sealed class NetworkConditionsTests
{
    // Problem: what happens when the server is slow? We can test that without really waiting.
    [Test]
    public async Task SlowServer_DelaysTheReplyWithoutWaiting()
    {
        FakeTimeProvider clock = new();
        NetworkBehavior behavior = new() { Delay = TimeSpan.FromSeconds(2), Variance = 0, FailurePercent = 0 };
        using StubHttp http = new(behavior) { { Route.Get("/people/1"), Reply.Json("""{"id":1,"name":"Ada"}""") } };
        http.TimeProvider = clock;
        using HttpClient httpClient = TestClient.Create(http);

        Task<HttpResponseMessage> pendingReply = httpClient.GetAsync(new Uri("https://api.example.com/people/1"));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.That(pendingReply.IsCompleted, Is.False); // Only one of the two simulated seconds has passed.

        clock.Advance(TimeSpan.FromSeconds(1));
        using HttpResponseMessage response = await pendingReply;
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // Problem: what happens when the network just fails, not just returns an error status?
    [Test]
    public void BrokenNetwork_ThrowsAConnectionError()
    {
        NetworkBehavior behavior = new() { Delay = TimeSpan.Zero, FailurePercent = 1, FailureFactory = static () => new HttpRequestException("Connection reset.") };
        using StubHttp http = new(behavior) { { Route.Get("/people/1"), Reply.Json("""{"id":1,"name":"Ada"}""") } };
        using HttpClient httpClient = TestClient.Create(http);

        HttpRequestException? error = Assert.ThrowsAsync<HttpRequestException>(
            () => httpClient.GetAsync(new Uri("https://api.example.com/people/1")));

        Assert.That(error?.Message, Is.EqualTo("Connection reset."));
    }
}
