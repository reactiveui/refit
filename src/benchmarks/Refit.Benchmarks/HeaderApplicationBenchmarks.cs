// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the header application path shared by generated and reflection request building:
/// <c>HttpHeaderApplier.Apply</c> (validated and unvalidated) and the <see cref="GeneratedRequestRunner"/> header
/// entry points that sanitize, replace, and stamp request and content headers. Each benchmark builds a fresh request
/// so header state does not accumulate across invocations; the request allocation is part of the measured cost.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class HeaderApplicationBenchmarks
{
    /// <summary>A representative custom header name applied to a request.</summary>
    private readonly string _customHeaderName = "X-Correlation-Id";

    /// <summary>A representative custom header value applied to a request.</summary>
    private readonly string _customHeaderValue = "6f8b2c1e-3a4d-4e5f-8a9b-0c1d2e3f4a5b";

    /// <summary>A header collection stamped onto a request by the collection benchmark.</summary>
    private readonly Dictionary<string, string> _headerCollection = new()
    {
        ["X-Correlation-Id"] = "6f8b2c1e-3a4d-4e5f-8a9b-0c1d2e3f4a5b",
        ["X-Client-Version"] = "12.3.4",
        ["Accept-Language"] = "en-AU",
    };

    /// <summary>Builds a fresh request without applying any header, isolating the per-op request construction cost
    /// that every other benchmark in this class also pays so the header-application delta can be read directly.</summary>
    /// <returns>The resulting request header count.</returns>
    [Benchmark]
    [BenchmarkCategory("Control")]
    public int BuildRequestOnly()
    {
        // Reading the shared header-name field anchors this control to the instance so BenchmarkDotNet can invoke it
        // like the header benchmarks, while it still measures only the per-op request construction they all pay.
        _ = _customHeaderName;
        using var request = NewRequest();
        return request.Headers.NonValidated.Count;
    }

    /// <summary>Sets one custom request header verbatim (unvalidated).</summary>
    /// <returns>The resulting request header count.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SetHeader")]
    public int SetHeaderUnvalidated()
    {
        using var request = NewRequest();
        GeneratedRequestRunner.SetHeader(request, _customHeaderName, _customHeaderValue, false);
        return request.Headers.NonValidated.Count;
    }

    /// <summary>Sets one custom request header with framework validation.</summary>
    /// <returns>The resulting request header count.</returns>
    [Benchmark]
    [BenchmarkCategory("SetHeader")]
    public int SetHeaderValidated()
    {
        using var request = NewRequest();
        GeneratedRequestRunner.SetHeader(request, _customHeaderName, _customHeaderValue, true);
        return request.Headers.NonValidated.Count;
    }

    /// <summary>Stamps a collection of headers onto a request.</summary>
    /// <returns>The resulting request header count.</returns>
    [Benchmark]
    [BenchmarkCategory("Collection")]
    public int AddHeaderCollection()
    {
        using var request = NewRequest();
        GeneratedRequestRunner.AddHeaderCollection(request, _headerCollection, false);
        return request.Headers.NonValidated.Count;
    }

    /// <summary>Applies a validated header directly through the shared applier.</summary>
    /// <returns>The resulting request header count.</returns>
    [Benchmark]
    [BenchmarkCategory("Apply")]
    public int ApplyValidated()
    {
        using var request = NewRequest();
        HttpHeaderApplier.Apply(request, _customHeaderName, _customHeaderValue, true);
        return request.Headers.NonValidated.Count;
    }

    /// <summary>Applies an unvalidated header directly through the shared applier.</summary>
    /// <returns>The resulting request header count.</returns>
    [Benchmark]
    [BenchmarkCategory("Apply")]
    public int ApplyUnvalidated()
    {
        using var request = NewRequest();
        HttpHeaderApplier.Apply(request, _customHeaderName, _customHeaderValue, false);
        return request.Headers.NonValidated.Count;
    }

    /// <summary>Creates a fresh POST request with empty content for header application.</summary>
    /// <returns>The new request.</returns>
    private static HttpRequestMessage NewRequest() =>
        new(HttpMethod.Post, new Uri("/resource", UriKind.Relative)) { Content = new ByteArrayContent([]) };
}
