// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Refit.Tests;

/// <summary>Tests for the scope-aware authorization header value provider registered via <c>AddAuthorizationHeaderValueProvider</c> (#1679).</summary>
public partial class HttpClientFactoryExtensionsTests
{
    /// <summary>The number of concurrent requests driven through the scoped provider.</summary>
    private const int ConcurrentRequestCount = 2;

    /// <summary>The ambient token carried by the first concurrent request.</summary>
    private const string FirstRequestToken = "token-a";

    /// <summary>The ambient token carried by the second concurrent request.</summary>
    private const string SecondRequestToken = "token-b";

    /// <summary>The request URI targeted by the scoped-authorization requests.</summary>
    private const string ScopedRequestUri = "https://scoped.example";

    /// <summary>The upper bound on how long a request waits for its concurrent peer to arrive.</summary>
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Verifies the scoped authorization provider resolves a fresh per-request token and disposes each request scope (#1679).</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddAuthorizationHeaderValueProviderResolvesPerRequestTokenAndDisposesScopes()
    {
        var arrivals = 0;
        var bothArrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var echoHandler = new EchoAuthorizationHandler(async () =>
        {
            if (Interlocked.Increment(ref arrivals) == ConcurrentRequestCount)
            {
                bothArrived.SetResult();
            }

            await bothArrived.Task.WaitAsync(GateTimeout);
        });

        var disposalTracker = new ScopeDisposalTracker();
        var services = new ServiceCollection();
        _ = services.AddSingleton<AmbientToken>();
        _ = services.AddSingleton(disposalTracker);
        _ = services.AddScoped<ScopedTokenProvider>();

        var builder = services.AddRefitClient<IFooWithOtherAttribute>(
            new RefitSettings { HttpMessageHandlerFactory = () => echoHandler });
        _ = builder.AddAuthorizationHeaderValueProvider(
            static (serviceProvider, _, _) =>
                new ValueTask<string>(serviceProvider.GetRequiredService<ScopedTokenProvider>().Token));

        var serviceProvider = services.BuildServiceProvider();
        var ambient = serviceProvider.GetRequiredService<AmbientToken>();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Each call sets an ambient token in its own async flow, mirroring how IHttpContextAccessor carries
        // per-request state; the handler's fresh scope must observe its own request's token, never the other's.
        async Task<string> CallWithTokenAsync(string token)
        {
            ambient.Write(token);
            var client = factory.CreateClient(builder.Name);
            using var response = await client.GetAsync(new Uri(ScopedRequestUri));
            return await response.Content.ReadAsStringAsync();
        }

        var results = await Task.WhenAll(
            CallWithTokenAsync(FirstRequestToken),
            CallWithTokenAsync(SecondRequestToken));

        await Assert.That(results[0]).IsEqualTo(FirstRequestToken);
        await Assert.That(results[1]).IsEqualTo(SecondRequestToken);
        await Assert.That(disposalTracker.DisposedCount).IsEqualTo(ConcurrentRequestCount);
    }

    /// <summary>Carries a per-request token through ambient <see cref="AsyncLocal{T}"/> state, standing in for a host-registered accessor.</summary>
    private sealed class AmbientToken
    {
        /// <summary>The ambient token value flowing with the current asynchronous request.</summary>
        private readonly AsyncLocal<string?> _current = new();

        /// <summary>Reads the ambient token value for the current asynchronous flow.</summary>
        /// <returns>The ambient token, or null when none is set.</returns>
        public string? Read() => _current.Value;

        /// <summary>Sets the ambient token value for the current asynchronous flow.</summary>
        /// <param name="value">The token to flow with the current request.</param>
        public void Write(string? value) => _current.Value = value;
    }

    /// <summary>Counts how many per-request scopes the handler has disposed.</summary>
    private sealed class ScopeDisposalTracker
    {
        /// <summary>The number of scopes disposed so far.</summary>
        private int _disposedCount;

        /// <summary>Gets the number of scopes disposed so far.</summary>
        public int DisposedCount => Volatile.Read(ref _disposedCount);

        /// <summary>Records that a scope has been disposed.</summary>
        public void MarkDisposed() => Interlocked.Increment(ref _disposedCount);
    }

    /// <summary>A scoped service that reads the ambient token and reports its own disposal to verify per-request scope lifetime.</summary>
    /// <param name="ambient">The ambient token source read for each request.</param>
    /// <param name="tracker">The tracker notified when this scoped instance is disposed.</param>
    private sealed class ScopedTokenProvider(AmbientToken ambient, ScopeDisposalTracker tracker) : IDisposable
    {
        /// <summary>Gets the token for the current request.</summary>
        public string Token => ambient.Read() ?? string.Empty;

        /// <inheritdoc/>
        public void Dispose() => tracker.MarkDisposed();
    }

    /// <summary>Terminal handler that echoes the received authorization parameter after both concurrent requests arrive.</summary>
    /// <param name="onRequest">A gate awaited after the authorization parameter is captured, used to force concurrent overlap.</param>
    private sealed class EchoAuthorizationHandler(Func<Task> onRequest) : HttpMessageHandler
    {
        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var parameter = request.Headers.Authorization?.Parameter ?? string.Empty;
            await onRequest().ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(parameter) };
        }
    }
}
