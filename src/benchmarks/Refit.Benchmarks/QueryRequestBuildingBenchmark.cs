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

    /// <summary>The sample scalar query text.</summary>
    private const string QueryText = "widgets";

    /// <summary>The sample collection values.</summary>
    private static readonly int[] _sampleIds = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>A sample timestamp whose invariant form contains reserved characters (<c>:</c>, <c>+</c>).</summary>
    private static readonly DateTimeOffset _sampleTimestamp = new(2026, 7, 13, 12, 30, 0, TimeSpan.Zero);

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

    /// <summary>The cached reflection delegate for <see cref="IQueryRequestService.TimestampQueryAsync"/>.</summary>
    private Func<HttpClient, object[], object?> _reflectionTimestampQuery = null!;

    /// <summary>The cached reflection delegate for <see cref="IQueryRequestService.TimestampPathAsync"/>.</summary>
    private Func<HttpClient, object[], object?> _reflectionTimestampPath = null!;

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
        _reflectionTimestampQuery = reflectionBuilder.BuildRestResultFuncForMethod(nameof(IQueryRequestService.TimestampQueryAsync));
        _reflectionTimestampPath = reflectionBuilder.BuildRestResultFuncForMethod(nameof(IQueryRequestService.TimestampPathAsync));
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
        _generated.MultiParameterAsync(QueryText, SamplePage, SamplePageSize, true, QuerySort.DateDescending);

    /// <summary>Benchmarks five reflection-built scalar query parameters including an enum.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("MultiParameter")]
    public Task<HttpResponseMessage> ReflectionMultiParameterAsync() =>
        (Task<HttpResponseMessage>)_reflectionMultiParameter(
            _client,
            [QueryText, SamplePage, SamplePageSize, true, QuerySort.DateDescending])!;

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

    /// <summary>Benchmarks a generated span-formattable query value that requires escaping (span-escape path).</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TimestampQuery")]
    public Task<HttpResponseMessage> GeneratedTimestampQueryAsync() => _generated.TimestampQueryAsync(_sampleTimestamp);

    /// <summary>Benchmarks a reflection-built span-formattable query value that requires escaping.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("TimestampQuery")]
    public Task<HttpResponseMessage> ReflectionTimestampQueryAsync() =>
        (Task<HttpResponseMessage>)_reflectionTimestampQuery(_client, [_sampleTimestamp])!;

    /// <summary>Benchmarks a generated span-formattable path value that requires escaping (path span-escape).</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("TimestampPath")]
    public Task<HttpResponseMessage> GeneratedTimestampPathAsync() => _generated.TimestampPathAsync(_sampleTimestamp);

    /// <summary>Benchmarks a reflection-built span-formattable path value that requires escaping.</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("TimestampPath")]
    public Task<HttpResponseMessage> ReflectionTimestampPathAsync() =>
        (Task<HttpResponseMessage>)_reflectionTimestampPath(_client, [_sampleTimestamp])!;

    /// <summary>Benchmarks a generated custom-verb request, exercising the cached verb instance (source-generation only).</summary>
    /// <returns>The HTTP response message.</returns>
    [Benchmark]
    [BenchmarkCategory("SourceGenOnly")]
    public Task<HttpResponseMessage> GeneratedCustomVerbAsync() => _generated.CustomVerbAsync(QueryText);
}
