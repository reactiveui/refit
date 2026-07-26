// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>Compares generated and reflection request building for path-only and dual path/header parameters.</summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PathHeaderRequestBuildingBenchmarks
{
    /// <summary>The base host address used for requests.</summary>
    private const string Host = "https://api.example.test";

    /// <summary>The identifier passed to every benchmarked request.</summary>
    private const int Identifier = 42;

    /// <summary>The generated client.</summary>
    private IPathHeaderRequestService _generated = null!;

    /// <summary>The HTTP client used by both request builders.</summary>
    private HttpClient _client = null!;

    /// <summary>The cached reflection path-only request delegate.</summary>
    private Func<HttpClient, object[], object?> _reflectionPathOnly = null!;

    /// <summary>The cached reflection path-and-header request delegate.</summary>
    private Func<HttpClient, object[], object?> _reflectionPathAndHeader = null!;

    /// <summary>Creates the generated client and cached reflection delegates.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = new RefitSettings();
        _client = new(new StaticValueHttpResponseHandler("Ok", HttpStatusCode.OK))
        {
            BaseAddress = new(Host),
        };
        _generated = RestService.ForGenerated<IPathHeaderRequestService>(_client, settings);

        var reflectionBuilder = RequestBuilder.ForType<IPathHeaderRequestService>(settings);
        _reflectionPathOnly = reflectionBuilder.BuildRestResultFuncForMethod(
            nameof(IPathHeaderRequestService.PathOnlyAsync));
        _reflectionPathAndHeader = reflectionBuilder.BuildRestResultFuncForMethod(
            nameof(IPathHeaderRequestService.PathAndHeaderAsync));
    }

    /// <summary>Disposes the shared HTTP client.</summary>
    [GlobalCleanup]
    public void Cleanup() => _client.Dispose();

    /// <summary>Builds and sends a generated path-only request.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("PathOnly")]
    public Task<HttpResponseMessage> GeneratedPathOnlyAsync() => _generated.PathOnlyAsync(Identifier);

    /// <summary>Builds and sends a reflection path-only request.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("PathOnly")]
    public Task<HttpResponseMessage> ReflectionPathOnlyAsync() =>
        (Task<HttpResponseMessage>)_reflectionPathOnly(_client, [Identifier])!;

    /// <summary>Builds and sends a generated request whose identifier also contributes a header.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("PathAndHeader")]
    public Task<HttpResponseMessage> GeneratedPathAndHeaderAsync() =>
        _generated.PathAndHeaderAsync(Identifier);

    /// <summary>Builds and sends a reflection request whose identifier also contributes a header.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("PathAndHeader")]
    public Task<HttpResponseMessage> ReflectionPathAndHeaderAsync() =>
        (Task<HttpResponseMessage>)_reflectionPathAndHeader(_client, [Identifier])!;
}
