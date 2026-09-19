// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;

namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Returns local payloads so dispatch examples never contact a server.</summary>
internal sealed class DispatchHandler : HttpMessageHandler
{
    /// <summary>Gets the number of requests observed by the local handler.</summary>
    internal int RequestCount { get; private set; }

    /// <summary>Gets the last returned response for ownership assertions in the local harness.</summary>
    internal HttpResponseMessage? LastResponse { get; private set; }

    /// <summary>Produces a fresh response for each local request.</summary>
    /// <param name="request">The generated or handwritten request.</param>
    /// <param name="cancellationToken">The effective per-call token.</param>
    /// <returns>A JSON array for streaming or an integer for regular requests.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RequestCount++;
        string payload = request.RequestUri!.AbsolutePath == "/stream" ? "[1,2]" : "12";
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "application/json"), RequestMessage = request, };
        LastResponse = response;
        return Task.FromResult(response);
    }
}
