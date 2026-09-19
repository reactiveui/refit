// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Waits until cancellation so a per-call deadline can be observed without a service.</summary>
internal sealed class TimeoutHandler : HttpMessageHandler
{
    /// <summary>Waits for the generated effective token to cancel.</summary>
    /// <param name="request">The request whose lifetime remains with the generated call.</param>
    /// <param name="cancellationToken">The token including the declared per-call timeout.</param>
    /// <returns>The canceled local send.</returns>
    /// <exception cref="InvalidOperationException">The infinite local wait completes without cancellation.</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("The timeout handler must be canceled.");
    }
}
