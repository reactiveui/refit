// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;

namespace Refit.Benchmarks;

/// <summary>An HttpMessageHandler that always responds with the contents of a file.</summary>
public class StaticFileHttpResponseHandler : HttpMessageHandler
{
    /// <summary>The status code returned for every response.</summary>
    private readonly HttpStatusCode _responseCode;

    /// <summary>The response body read from the configured file.</summary>
    private readonly string _responsePayload;

    /// <summary>Initializes a new instance of the <see cref="StaticFileHttpResponseHandler"/> class.</summary>
    /// <param name="fileName">The path of the file whose contents are returned as the body.</param>
    /// <param name="responseCode">The status code to return.</param>
    public StaticFileHttpResponseHandler(string fileName, HttpStatusCode responseCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        _responsePayload = File.ReadAllText(fileName);
        _responseCode = responseCode;
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            new HttpResponseMessage(_responseCode)
            {
                RequestMessage = request,
                Content = new StringContent(_responsePayload)
            });
}
