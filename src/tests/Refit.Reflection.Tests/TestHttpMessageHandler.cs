// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Refit.Reflection.Tests;

/// <summary>A test message handler capturing the request and returning canned content.</summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    /// <summary>Initializes a new instance of the <see cref="TestHttpMessageHandler"/> class.</summary>
    /// <param name="content">The canned response content body.</param>
    [SuppressMessage(
        "Design",
        "SST2309:An externally visible member declares an optional parameter, so callers bake in the default",
        Justification = "Test handler ctor convenience default; preserves existing call sites.")]
    public TestHttpMessageHandler(string content = "test")
    {
        Content = new StringContent(content);
        ContentFactory = () => Content;
    }

    /// <summary>Gets the last request message received.</summary>
    public HttpRequestMessage? RequestMessage { get; private set; }

    /// <summary>Gets or sets the number of messages sent.</summary>
    public int MessagesSent { get; set; }

    /// <summary>Gets or sets the response content.</summary>
    public HttpContent Content { get; set; }

    /// <summary>Gets or sets the factory used to produce response content.</summary>
    public Func<HttpContent> ContentFactory { get; set; }

    /// <summary>Gets or sets the cancellation token captured from the last request.</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>Gets or sets the content read from the last request.</summary>
    public string? SendContent { get; set; }

    /// <summary>Captures the request and returns the configured response.</summary>
    /// <param name="request">The HTTP request message being sent.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The configured HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestMessage = request;
        if (request.Content is not null)
        {
            SendContent = await request
                .Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        CancellationToken = cancellationToken;
        MessagesSent++;

        return new(HttpStatusCode.OK) { Content = ContentFactory() };
    }
}
