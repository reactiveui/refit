// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Testing.Tests;

/// <summary>
/// Scenario tests showing the fault-injection surface driving the paths it exists for: a retry policy fed by
/// transient stubbed faults, and a client timeout fed by <see cref="NetworkBehavior.Delay"/>.
/// </summary>
public sealed class RetryAndTimeoutTests
{
    /// <summary>The base address the sample client sends requests to.</summary>
    private const string BaseUrl = "https://api.test";

    /// <summary>The route template mirroring <see cref="IUserApi.GetUser"/>.</summary>
    private const string UserTemplate = "/users/{id}";

    /// <summary>The login carried by the stubbed user.</summary>
    private const string SampleLogin = "octocat";

    /// <summary>A sample user identifier exercised by these tests.</summary>
    private const int SampleUserId = 7;

    /// <summary>The number of attempts the retry handler makes before giving up.</summary>
    private const int MaxAttempts = 3;

    /// <summary>The number of requests a single retried-once call is expected to make.</summary>
    private const int RetriedOnceRequestCount = 2;

    /// <summary>Verifies a retry policy recovers when a one-shot route replies with a transient error status.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RetryRecoversFromTransientErrorStatus()
    {
        // One-shot routes match in declared order, so the first attempt gets the 503 and the retry gets the user.
        var handler = new StubHttp
        {
            {
                Route.Get(UserTemplate),
                Reply.Status(HttpStatusCode.ServiceUnavailable)
            },
            {
                Route.Get(UserTemplate),
                Reply.With(new User(SampleUserId, SampleLogin))
            },
        };

        var api = CreateRetryingClient(handler);
        var user = await api.GetUser(SampleUserId);

        await Assert.That(user.Login).IsEqualTo(SampleLogin);
        await Assert.That(handler.Requests.Count).IsEqualTo(RetriedOnceRequestCount);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a retry policy recovers when the first attempt faults with a simulated network failure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RetryRecoversFromSimulatedNetworkFailure()
    {
        // The same exception NetworkBehavior injects at its configured failure rate, forced onto the first attempt.
        var failure = new NetworkBehavior().CreateFailure();
        var handler = new StubHttp
        {
            {
                Route.Get(UserTemplate),
                Reply.From(HttpResponseMessage (_) => throw failure)
            },
            {
                Route.Get(UserTemplate),
                Reply.With(new User(SampleUserId, SampleLogin))
            },
        };

        var api = CreateRetryingClient(handler);
        var user = await api.GetUser(SampleUserId);

        await Assert.That(user.Login).IsEqualTo(SampleLogin);
        await Assert.That(handler.Requests.Count).IsEqualTo(RetriedOnceRequestCount);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a retry policy that exhausts its attempts surfaces the final error status as an <see cref="ApiException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExhaustedRetriesSurfaceApiException()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = UserTemplate, Reusable = true },
                Reply.Status(HttpStatusCode.ServiceUnavailable)
            },
        };

        var api = CreateRetryingClient(handler);

        var error = await Assert.That(async () => _ = await api.GetUser(SampleUserId)).ThrowsExactly<ApiException>();

        await Assert.That(error!.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That(handler.Requests.Count).IsEqualTo(MaxAttempts);
    }

    /// <summary>
    /// Verifies an injected delay longer than the client timeout aborts the request, and that the resulting
    /// cancellation is surfaced through Refit's transport-exception factory as an <see cref="ApiRequestException"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestSlowerThanClientTimeoutThrowsApiRequestException()
    {
        const int injectedDelaySeconds = 10;
        const int clientTimeoutMilliseconds = 50;
        var handler = CreateDelayedHandler(TimeSpan.FromSeconds(injectedDelaySeconds));

        using var client = new HttpClient(handler)
        {
            BaseAddress = new(BaseUrl),
            Timeout = TimeSpan.FromMilliseconds(clientTimeoutMilliseconds),
        };
        var api = RestService.For<IUserApi>(client);

        var error = await Assert.That(async () => _ = await api.GetUser(SampleUserId)).ThrowsExactly<ApiRequestException>();

        await Assert.That(error!.InnerException).IsTypeOf<TaskCanceledException>();
    }

    /// <summary>Verifies a delay inside the client timeout still yields the stubbed response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestFasterThanClientTimeoutSucceeds()
    {
        const int clientTimeoutSeconds = 30;
        var handler = CreateDelayedHandler(TimeSpan.FromMilliseconds(1));

        using var client = new HttpClient(handler)
        {
            BaseAddress = new(BaseUrl),
            Timeout = TimeSpan.FromSeconds(clientTimeoutSeconds),
        };
        var api = RestService.For<IUserApi>(client);

        var user = await api.GetUser(SampleUserId);

        await Assert.That(user.Login).IsEqualTo(SampleLogin);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Creates a stub that answers the user route after a fixed, fault-free injected delay.</summary>
    /// <param name="delay">The latency to inject before replying.</param>
    /// <returns>The configured stub handler.</returns>
    private static StubHttp CreateDelayedHandler(TimeSpan delay)
    {
        var behavior = new NetworkBehavior
        {
            Delay = delay,
            Variance = 0d,
            FailurePercent = 0d,
            ErrorPercent = 0d,
        };

        return new(behavior)
        {
            {
                Route.Get(UserTemplate),
                Reply.With(new User(SampleUserId, SampleLogin))
            },
        };
    }

    /// <summary>Creates a Refit client whose requests pass through a retrying handler stacked above the stub.</summary>
    /// <param name="handler">The stub handler backing the client.</param>
    /// <returns>A Refit client that retries transient faults.</returns>
    private static IUserApi CreateRetryingClient(StubHttp handler)
    {
        // The stub is wrapped rather than used directly, so the retry policy sits where a Polly handler would.
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => new RetryHandler(handler) };
        return RestService.For<IUserApi>(BaseUrl, settings);
    }

    /// <summary>A minimal retry policy that re-sends on a transport fault or a non-success status, standing in for Polly.</summary>
    /// <remarks>Initializes a new instance of the <see cref="RetryHandler"/> class.</remarks>
    /// <param name="inner">The handler to send through.</param>
    private sealed class RetryHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt < MaxAttempts; attempt++)
            {
                try
                {
                    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

                    response.Dispose();
                }
                catch (HttpRequestException)
                {
                }
            }

            // The final attempt is unguarded: its status or transport fault is what the caller sees.
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
