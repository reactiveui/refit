// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Observes whether JSON content has been buffered before reaching the transport.</summary>
internal sealed class BufferingHandler : HttpMessageHandler
{
    /// <summary>The reply returned after observing the request.</summary>
    internal const string ReplyText = "accepted";

    /// <summary>Gets the length known before the handler reads any content.</summary>
    internal long? LengthBeforeRead { get; private set; }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LengthBeforeRead = request.Content?.Headers.ContentLength;
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(ReplyText) });
    }
}
