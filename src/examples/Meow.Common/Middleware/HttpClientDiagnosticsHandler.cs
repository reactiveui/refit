// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics;
using Serilog;

namespace HttpClientDiagnostics;

/// <summary>A delegating handler that logs request and response details along with timing information.</summary>
[DebuggerStepThrough]
public class HttpClientDiagnosticsHandler : DelegatingHandler
{
    /// <summary>Initializes a new instance of the <see cref="HttpClientDiagnosticsHandler"/> class with an inner handler.</summary>
    /// <param name="innerHandler">The inner handler that processes the request.</param>
    public HttpClientDiagnosticsHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HttpClientDiagnosticsHandler"/> class.</summary>
    public HttpClientDiagnosticsHandler()
    {
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var totalStartTimestamp = Stopwatch.GetTimestamp();

        Log.Debug("Request: {Request}", request);
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Log.Debug("Request Content: {Content}", content);
        }

        var responseStartTimestamp = Stopwatch.GetTimestamp();
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        Log.Debug("Response: {Response}", response);
        if (response.Content is not null)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Log.Debug("Response Content: {Content}", content);
        }

        Log.Debug(
            "Response elapsed time: {ElapsedMilliseconds} ms",
            (long)Stopwatch.GetElapsedTime(responseStartTimestamp).TotalMilliseconds);

        Log.Debug(
            "Total elapsed time: {ElapsedMilliseconds} ms",
            (long)Stopwatch.GetElapsedTime(totalStartTimestamp).TotalMilliseconds);

        return response;
    }
}
