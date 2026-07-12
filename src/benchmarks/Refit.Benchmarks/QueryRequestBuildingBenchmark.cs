// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Refit.Benchmarks;

/// <summary>
/// Compares generated inline query request building against the reflection request builder for the same
/// method shapes, with allocation tracking. The generated rows go through the source-generated client;
/// the reflection rows invoke the cached reflection request delegate the fallback path uses.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class QueryRequestBuildingBenchmark
{
    /// <summary>The base host address used for requests.</summary>
    private const string Host = "https://api.example.test";

    /// <summary>The sample page number.</summary>
    private const int SamplePage = 3;

    /// <summary>The sample page size.</summary>
    private const int SamplePageSize = 25;

    /// <summary>The sample collection values.</summary>
    private static readonly int[] _sampleIds = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>The generated Refit client.</summary>
    private IQueryRequestService _generated = null!;

    /// <summary>The HTTP client used by the reflection request delegates.</summary>
    private HttpClient _client = null!;

    /// <summary>The cached reflection delegate for <see cref="IQueryRequestService.SingleQueryAsync"/>.</summary>
    private Func<HttpClient, object[], object?> _reflectionSingleQuery = null!;

    /// <summary>The cached reflection delegate for <see cref="IQueryRequestService.MultiParameterAsync"/>.</summary>
    private Func<HttpClient, object[], object?> _reflectionMultiParameter = null!;

    /// <summary>The cached reflection delegate for <see cref="IQueryRequestService.CsvCollectionAsync"/>.</summary>
    private Func<HttpClient, object[], object?> _reflectionCsvCollection = null!;

    /// <summary>The cached reflection delegate for <see cref="IQueryRequestService.MultiCollectionAsync"/>.</summary>
    private Func<HttpClient, object[], object?> _reflectionMultiCollection = null!;

    /// <summary>Initializes the clients before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        _client = new(new StaticValueHttpResponseHandler("Ok", HttpStatusCode.OK))
        {
            BaseAddress = new(Host),
        };

        _generated = RestService.ForGenerated<IQueryRequestService>(_client, settings);

        var reflectionBuilder = RequestBuilder.ForType<IQueryRequestService>(settings);
        _reflectionSingleQuery = reflectionBuilder.BuildRestResultFuncForMethod(nameof(IQueryRequestService.SingleQueryAsync));
        _reflectionMultiParameter = reflectionBuilder.BuildRestResultFuncForMethod(nameof(IQueryRequestService.MultiParameterAsync));
        _reflectionCsvCollection = reflectionBuilder.BuildRestResultFuncForMethod(nameof(IQueryRequestService.CsvCollectionAsync));
        _reflectionMultiCollection = reflectionBuilder.BuildRestResultFuncForMethod(nameof(IQueryRequestService.MultiCollectionAsync));
    }

    /// <summary>Cleans up the HTTP client.</summary>
    [GlobalCleanup]
    public void Cleanup() => _client.Dispose();

    /// <summary>Benchmarks one generated query parameter.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SingleQuery")]
    public Task<HttpResponseMessage> GeneratedSingleQueryAsync() => _generated.SingleQueryAsync("widgets and gadgets");

    /// <summary>Benchmarks one reflection-built query parameter.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("SingleQuery")]
    public Task<HttpResponseMessage> ReflectionSingleQueryAsync() =>
        (Task<HttpResponseMessage>)_reflectionSingleQuery(_client, ["widgets and gadgets"])!;

    /// <summary>Benchmarks five generated scalar query parameters including an enum.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("MultiParameter")]
    public Task<HttpResponseMessage> GeneratedMultiParameterAsync() =>
        _generated.MultiParameterAsync("widgets", SamplePage, SamplePageSize, true, QuerySort.DateDescending);

    /// <summary>Benchmarks five reflection-built scalar query parameters including an enum.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("MultiParameter")]
    public Task<HttpResponseMessage> ReflectionMultiParameterAsync() =>
        (Task<HttpResponseMessage>)_reflectionMultiParameter(
            _client,
            ["widgets", SamplePage, SamplePageSize, true, QuerySort.DateDescending])!;

    /// <summary>Benchmarks a generated csv-joined collection.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("CsvCollection")]
    public Task<HttpResponseMessage> GeneratedCsvCollectionAsync() => _generated.CsvCollectionAsync(_sampleIds);

    /// <summary>Benchmarks a reflection-built csv-joined collection.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("CsvCollection")]
    public Task<HttpResponseMessage> ReflectionCsvCollectionAsync() =>
        (Task<HttpResponseMessage>)_reflectionCsvCollection(_client, [_sampleIds])!;

    /// <summary>Benchmarks a generated multi-expanded collection.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("MultiCollection")]
    public Task<HttpResponseMessage> GeneratedMultiCollectionAsync() => _generated.MultiCollectionAsync(_sampleIds);

    /// <summary>Benchmarks a reflection-built multi-expanded collection.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("MultiCollection")]
    public Task<HttpResponseMessage> ReflectionMultiCollectionAsync() =>
        (Task<HttpResponseMessage>)_reflectionMultiCollection(_client, [_sampleIds])!;

    /// <summary>Benchmarks a generated valueless query flag (source-generation only).</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("SourceGenOnly")]
    public Task<HttpResponseMessage> GeneratedFlagAsync() => _generated.FlagAsync("ready");

    /// <summary>Benchmarks a generated caller-encoded value (source-generation only).</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("SourceGenOnly")]
    public Task<HttpResponseMessage> GeneratedEncodedAsync() => _generated.EncodedAsync("a%2Fb%2Fc");
}
