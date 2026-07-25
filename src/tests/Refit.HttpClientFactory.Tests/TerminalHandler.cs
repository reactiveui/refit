// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;

namespace Refit.HttpClientFactory.Tests;

/// <summary>Terminal handler that records the authorization value it receives.</summary>
internal sealed class TerminalHandler : HttpMessageHandler
{
    /// <summary>Gets the authorization value received with the request.</summary>
    internal AuthenticationHeaderValue? Authorization { get; private set; }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Authorization = request.Headers.Authorization;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
