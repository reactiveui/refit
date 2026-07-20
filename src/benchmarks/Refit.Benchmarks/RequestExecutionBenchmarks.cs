// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the request execution scaffolding shared by generated and reflection sends: the
/// cancellation-token linking in <c>GeneratedRequestRunner</c> and the timeout linking, per-call timeout stashing, and
/// streaming-format detection in <c>RequestExecutionHelpers</c>. Linked sources allocated by the helpers are disposed
/// inside each benchmark.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class RequestExecutionBenchmarks
{
    /// <summary>A representative per-call timeout in milliseconds.</summary>
    private const int TimeoutMilliseconds = 5_000;

    /// <summary>The per-call timeout stashed onto and read back from a request.</summary>
    private readonly int _timeoutMilliseconds = TimeoutMilliseconds;

    /// <summary>A cancellable source whose token seeds the linking benchmarks.</summary>
    private CancellationTokenSource _sourceA = null!;

    /// <summary>A second cancellable source whose token seeds the both-cancellable linking benchmark.</summary>
    private CancellationTokenSource _sourceB = null!;

    /// <summary>Response content advertising a server-sent-events media type.</summary>
    private HttpContent _sseContent = null!;

    /// <summary>Response content advertising a JSON Lines media type.</summary>
    private HttpContent _jsonLinesContent = null!;

    /// <summary>Response content advertising a plain JSON media type.</summary>
    private HttpContent _jsonContent = null!;

    /// <summary>Builds the cancellation sources and typed response content before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _sourceA = new();
        _sourceB = new();
        _sseContent = TypedContent("text/event-stream");
        _jsonLinesContent = TypedContent("application/jsonl");
        _jsonContent = TypedContent("application/json");
    }

    /// <summary>Disposes the cancellation sources and content.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _sourceA.Dispose();
        _sourceB.Dispose();
        _sseContent.Dispose();
        _jsonLinesContent.Dispose();
        _jsonContent.Dispose();
    }

    /// <summary>Resolves the effective token when both inputs can cancel (allocates and disposes a linked source).</summary>
    /// <returns>1 when the resolved token can cancel; otherwise 0.</returns>
    [Benchmark]
    [BenchmarkCategory("Cancellation")]
    public int ResolveTokenBoth()
    {
        var (token, linked) = GeneratedRequestRunner.ResolveRequestCancellationToken(_sourceA.Token, _sourceB.Token);
        linked?.Dispose();
        return token.CanBeCanceled ? 1 : 0;
    }

    /// <summary>Resolves the effective token when only one input can cancel (no source allocated).</summary>
    /// <returns>1 when the resolved token can cancel; otherwise 0.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cancellation")]
    public int ResolveTokenSingle()
    {
        var (token, linked) = GeneratedRequestRunner.ResolveRequestCancellationToken(_sourceA.Token, default);
        linked?.Dispose();
        return token.CanBeCanceled ? 1 : 0;
    }

    /// <summary>Layers a per-call timeout onto the effective token (allocates and disposes a linked source).</summary>
    /// <returns>1 when the resolved token can cancel; otherwise 0.</returns>
    [Benchmark]
    [BenchmarkCategory("Timeout")]
    public int CreateTimeoutTokenWith()
    {
        var (token, timeoutSource) = RequestExecutionHelpers.CreateTimeoutToken(TimeoutMilliseconds, _sourceA.Token);
        timeoutSource?.Dispose();
        return token.CanBeCanceled ? 1 : 0;
    }

    /// <summary>Passes through the effective token when no timeout applies (no source allocated).</summary>
    /// <returns>1 when the resolved token can cancel; otherwise 0.</returns>
    [Benchmark]
    [BenchmarkCategory("Timeout")]
    public int CreateTimeoutTokenNone()
    {
        var (token, timeoutSource) = RequestExecutionHelpers.CreateTimeoutToken(0, _sourceA.Token);
        timeoutSource?.Dispose();
        return token.CanBeCanceled ? 1 : 0;
    }

    /// <summary>Stashes and reads back a per-call timeout on a fresh request's options.</summary>
    /// <returns>The round-tripped timeout in milliseconds.</returns>
    [Benchmark]
    [BenchmarkCategory("Timeout")]
    public int StashAndReadTimeout()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/resource", UriKind.Relative));
        GeneratedRequestRunner.SetRequestTimeout(request, _timeoutMilliseconds);
        return GeneratedRequestRunner.GetRequestTimeout(request);
    }

    /// <summary>Detects the streaming frame format of a server-sent-events response.</summary>
    /// <returns>The detected format as an integer.</returns>
    [Benchmark]
    [BenchmarkCategory("StreamingFormat")]
    public int DetectServerSentEvents() => (int)RequestExecutionHelpers.DetectStreamingFormat(_sseContent);

    /// <summary>Detects the streaming frame format of a JSON Lines response.</summary>
    /// <returns>The detected format as an integer.</returns>
    [Benchmark]
    [BenchmarkCategory("StreamingFormat")]
    public int DetectJsonLines() => (int)RequestExecutionHelpers.DetectStreamingFormat(_jsonLinesContent);

    /// <summary>Detects the streaming frame format of a plain JSON response (the array default).</summary>
    /// <returns>The detected format as an integer.</returns>
    [Benchmark]
    [BenchmarkCategory("StreamingFormat")]
    public int DetectJsonArray() => (int)RequestExecutionHelpers.DetectStreamingFormat(_jsonContent);

    /// <summary>Creates empty content advertising the given media type.</summary>
    /// <param name="mediaType">The media type to advertise.</param>
    /// <returns>The typed content.</returns>
    private static ByteArrayContent TypedContent(string mediaType) =>
        new([]) { Headers = { ContentType = new(mediaType) } };
}
