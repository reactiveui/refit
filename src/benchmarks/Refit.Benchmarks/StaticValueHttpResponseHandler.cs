// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;

namespace Refit.Benchmarks;

/// <summary>A message handler that returns a fixed response string and status code for every request.</summary>
/// <param name="response">The response body to return.</param>
/// <param name="code">The status code to return.</param>
public class StaticValueHttpResponseHandler(string response, HttpStatusCode code) : HttpMessageHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            new HttpResponseMessage(code) { RequestMessage = request, Content = new StringContent(response) });
}
