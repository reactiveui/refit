// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Shows how provider timing headers affect the next HTTP attempt.</summary>
internal static class ServerRetryHints
{
    /// <summary>Retries a throttled read, then returns the final error when a later wait exceeds the budget.</summary>
    /// <param name="cancellationToken">Cancels the sends and the waits between attempts.</param>
    /// <returns>Completion after the response and attempt count have been checked.</returns>
    internal static async Task RunAsync(CancellationToken cancellationToken)
    {
        ReadProviderHeaders(cancellationToken);
        await CheckRetryAfterAsync(cancellationToken);
        await ReplayQueuedWriteAsync(cancellationToken);
        using StubHttp stub = new();
        stub.Add(Route.Get("/status"), RetryReply.Header("x-ms-retry-after-ms", "0"));
        stub.Add(Route.Get("/status"), RetryReply.After(TimeSpan.FromSeconds(90), HttpStatusCode.ServiceUnavailable));

        HttpRetryOptions options = new()
        {
            MaxRetries = 3,
            TimeProvider = TimeProvider.System,
            MaxDelay = TimeSpan.FromSeconds(5),
            JitterMode = RetryJitterMode.Full,
            ServerDelayJitterRatio = 0.1,
            DelayProvider = RetryDelayProviders.Azure,
            RetryHttpRequestExceptions = false,
        };
        using HttpClient client = new(stub.CreateRetryHandler(options)) { BaseAddress = new Uri("https://people.example") };
        using HttpResponseMessage response = await client.GetAsync(new Uri("/status", UriKind.Relative), cancellationToken);
        SampleCheck.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        SampleCheck.Equal(2, stub.Requests.Count);
        await stub.VerifyAllCalledAsync();
    }

    /// <summary>Retries a bounded POST when the remote service honors the operation's idempotency key.</summary>
    /// <param name="cancellationToken">Cancels the send and body reads.</param>
    /// <returns>Completion after checking the same body and key reached both attempts.</returns>
    private static async Task ReplayQueuedWriteAsync(CancellationToken cancellationToken)
    {
        const string Key = "dispatch-17";
        const string Body = "ready";
        HttpRetryOptions options = new()
        {
            MaxRetries = 1,
            Backoff = { BaseDelay = TimeSpan.Zero, UseJitter = true, JitterMode = RetryJitterMode.Symmetric, JitterRatio = 0.1 },
            Replay = { RetryUnsafeMethods = true, MaxRequestContentBufferSize = 128 },
            Responses = { ShouldRetry = static response => response.StatusCode == HttpStatusCode.Conflict },
        };
        using StubHttp stub = new();
        RouteMatcher route = new() { Method = HttpMethod.Post, Template = "/dispatch", Headers = [("Idempotency-Key", Key)], Body = Body };
        stub.Add(route, Reply.Status(HttpStatusCode.Conflict));
        stub.Add(route, Reply.Status(HttpStatusCode.Accepted));
        using HttpClient client = new(stub.CreateRetryHandler(options));
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri("https://people.example/dispatch")) { Content = new StringContent(Body) };
        request.Headers.Add("Idempotency-Key", Key);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        SampleCheck.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await stub.VerifyAllCalledAsync();
    }

    /// <summary>Reads absolute retry dates and exercises the retry handler's standard policy.</summary>
    /// <param name="cancellationToken">Cancels the HTTP requests.</param>
    /// <returns>Completion after the standard policy recovers from throttling.</returns>
    private static async Task CheckRetryAfterAsync(CancellationToken cancellationToken)
    {
        const string Path = "/maintenance";
        DateTimeOffset deadline = DateTimeOffset.UnixEpoch.AddHours(1);
        using StubHttp stub = new()
        {
            { Route.Get(Path), RetryReply.After(deadline) },
            { Route.Get(Path), RetryReply.After(deadline, HttpStatusCode.ServiceUnavailable) },
            { Route.Get(Path), RetryReply.Header("x-ms-retry-after-ms", "25", HttpStatusCode.ServiceUnavailable) },
            { Route.Get(Path), RetryReply.After(TimeSpan.Zero) },
            { Route.Get(Path), Reply.Text("ready") },
        };
        Uri address = new("https://people.example/maintenance");
        using HttpClient raw = new(stub, disposeHandler: false);
        foreach (HttpStatusCode status in new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable })
        {
            using HttpResponseMessage paused = await raw.GetAsync(address, cancellationToken);
            SampleCheck.Equal(status, paused.StatusCode);
            SampleCheck.Equal(deadline, paused.Headers.RetryAfter?.Date);
        }

        using HttpResponseMessage provider = await raw.GetAsync(address, cancellationToken);
        SampleCheck.Equal(TimeSpan.FromMilliseconds(25), RetryDelayProviders.Azure(provider, deadline, cancellationToken));
        using HttpClient retry = new(stub.CreateRetryHandler());
        SampleCheck.Equal("ready", await retry.GetStringAsync(address, cancellationToken));
        await stub.VerifyAllCalledAsync();
    }

    /// <summary>Reads relative and absolute retry deadlines without waiting or sending another request.</summary>
    /// <param name="cancellationToken">Cancels header processing.</param>
    private static void ReadProviderHeaders(CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        using HttpResponseMessage overloaded = new(HttpStatusCode.TooManyRequests);
        overloaded.Headers.Add("x-amz-retry-after", "250");
        overloaded.Headers.Add("X-RateLimit-Reset", "10");
        overloaded.Headers.Add("X-RateLimit-Remaining", "0");
        SampleCheck.Equal(TimeSpan.FromMilliseconds(250), RetryDelayProviders.Aws(overloaded, now, cancellationToken));
        SampleCheck.Equal(TimeSpan.FromSeconds(10), RetryDelayProviders.GitHub(overloaded, now, cancellationToken));

        using HttpResponseMessage throttled = new(HttpStatusCode.TooManyRequests);
        throttled.Headers.Add("X-RateLimit-Reset", "1970-01-01T00:00:20Z");
        SampleCheck.Equal(TimeSpan.FromSeconds(20), RetryDelayProviders.Jira(throttled, now, cancellationToken));

        const string QueueHeader = "X-Queue-Retry";
        foreach ((RetryHeaderFormat format, string value) in new[]
        {
            (RetryHeaderFormat.Seconds, "2"),
            (RetryHeaderFormat.Milliseconds, "2000"),
            (RetryHeaderFormat.UnixTimeSeconds, "2"),
            (RetryHeaderFormat.Iso8601Date, "1970-01-01T00:00:02Z"),
            (RetryHeaderFormat.RetryAfter, "Thu, 01 Jan 1970 00:00:02 GMT"),
        })
        {
            _ = throttled.Headers.TryAddWithoutValidation(QueueHeader, value);
            TimeSpan? delay = RetryDelayProviders.ReadHeader(throttled, QueueHeader, format, now, cancellationToken)
                ?? RetryDelayProviders.RetryAfter(throttled, now, cancellationToken);
            SampleCheck.Equal(TimeSpan.FromSeconds(2), delay);
            _ = throttled.Headers.Remove(QueueHeader);
        }
    }
}
