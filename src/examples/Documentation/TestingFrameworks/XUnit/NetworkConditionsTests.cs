// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.Extensions.Time.Testing;
using Refit.Testing;
using Xunit;

namespace Refit.Documentation.TestingFrameworks;

/// <summary>Testing a slow or broken network, without a real slow or broken network.</summary>
public sealed class NetworkConditionsTests
{
    // Problem: what happens when the server is slow? We can test that without really waiting.
    [Fact]
    public async Task SlowServer_DelaysTheReplyWithoutWaiting()
    {
        FakeTimeProvider clock = new();
        NetworkBehavior behavior = new()
        {
            Delay = TimeSpan.FromSeconds(2),
            Variance = 0,
            FailurePercent = 0, // turn off the default random jitter and random failures so the test is repeatable
        };
        using StubHttp http = new(behavior)
        {
            { Route.Get("/people/1"), Reply.Json("""{"id":1,"name":"Ada"}""") },
        };
        http.TimeProvider = clock;
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());

        Task<Person> pendingPerson = api.GetPersonAsync(1);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(pendingPerson.IsCompleted); // Only one of the two simulated seconds has passed.

        clock.Advance(TimeSpan.FromSeconds(1));
        Person person = await pendingPerson;
        Assert.Equal("Ada", person.Name);
    }

    // Problem: what happens when the network just fails, not just returns an error status?
    [Fact]
    public async Task BrokenNetwork_ThrowsAConnectionError()
    {
        NetworkBehavior behavior = new()
        {
            Delay = TimeSpan.Zero,
            FailurePercent = 1, // 1.0 = every request fails (the value is a probability from 0 to 1)
            FailureFactory = static () => new HttpRequestException("Connection reset."),
        };
        using StubHttp http = new(behavior)
        {
            { Route.Get("/people/1"), Reply.Json("""{"id":1,"name":"Ada"}""") },
        };
        IPeopleApi api = http.CreateGeneratedClient<IPeopleApi>("https://api.example.com", TestSettings.Create());

        // Refit never lets a transport failure escape as the raw HttpRequestException: it wraps it in an
        // ApiRequestException, copying the original exception's message across.
        ApiRequestException error = await Assert.ThrowsAsync<ApiRequestException>(() => api.GetPersonAsync(1));

        Assert.Equal("Connection reset.", error.Message);
    }
}
