// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;

namespace Refit.Tests;

/// <summary>Answers every request with one reply and records the request body.</summary>
/// <param name="reply">The text every request receives.</param>
/// <param name="mediaType">The media type the reply declares.</param>
internal sealed class JsonReplyHandler(string reply, string mediaType) : HttpMessageHandler
{
    /// <summary>Initializes a new instance of the <see cref="JsonReplyHandler"/> class that replies with JSON.</summary>
    /// <param name="reply">The JSON text every request receives.</param>
    internal JsonReplyHandler(string reply)
        : this(reply, "application/json")
    {
    }

    /// <summary>Gets the body of the last request, or <see langword="null"/> when it had none.</summary>
    internal string? RequestBody { get; private set; }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new(HttpStatusCode.OK) { Content = new StringContent(reply, Encoding.UTF8, mediaType) };
    }
}
