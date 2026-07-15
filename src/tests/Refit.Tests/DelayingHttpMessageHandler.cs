// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit.Tests;

/// <summary>A test handler that waits for a fixed delay (honoring cancellation) before returning canned content, used to
/// exercise per-call <see cref="TimeoutAttribute"/> deadlines.</summary>
/// <param name="delay">The delay before responding. Use <see cref="Timeout.InfiniteTimeSpan"/> to block until the request
/// is canceled, or <see cref="TimeSpan.Zero"/> to respond immediately.</param>
public sealed class DelayingHttpMessageHandler(TimeSpan delay) : HttpMessageHandler
{
    /// <summary>Gets the number of requests the handler has received.</summary>
    public int RequestCount { get; private set; }

    /// <summary>Waits for the configured delay, honoring cancellation, then returns a successful response.</summary>
    /// <param name="request">The HTTP request message being sent.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A successful HTTP response with canned content.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        return new(HttpStatusCode.OK) { Content = new StringContent("ok") };
    }
}
