// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;

namespace Refit.Tests;

/// <summary>Captures the request URI and multipart parts of an in-flight request before they are disposed.</summary>
public sealed class MultipartCapturingHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Gets the request URI captured from the last request.</summary>
    public Uri? RequestUri { get; private set; }

    /// <summary>Gets the multipart parts captured from the last request.</summary>
    public List<CapturedMultipartPart> Parts { get; } = [];

    /// <summary>Captures the request and returns an empty successful response.</summary>
    /// <param name="request">The HTTP request message being sent.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A successful response.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUri = request.RequestUri;

        if (request.Content is MultipartFormDataContent multipart)
        {
            foreach (var part in multipart)
            {
                var name = part.Headers.ContentDisposition?.Name;
                var body = await part.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                Parts.Add(new(name, body));
            }
        }

        return new(HttpStatusCode.OK) { Content = new StringContent("test") };
    }
}
