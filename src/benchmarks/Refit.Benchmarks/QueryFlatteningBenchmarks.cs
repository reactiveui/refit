// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the query-object flattening engine <c>SystemTextJsonQueryFlattener</c> and its
/// public entry point <see cref="SystemTextJsonQueryConverter{T}"/>, which walk a value's JSON metadata and append
/// each property (scalar, collection, or nested object) to a query string.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
public class QueryFlatteningBenchmarks
{
    /// <summary>The base path the flattened query is appended to.</summary>
    private const string Path = "/search";

    /// <summary>A representative identifier on the flattened payload.</summary>
    private const int SampleId = 42;

    /// <summary>The Refit settings supplying the URL parameter formatter and collection format.</summary>
    private RefitSettings _settings = null!;

    /// <summary>The serializer options supplying the JSON type metadata walked during flattening.</summary>
    private JsonSerializerOptions _options = null!;

    /// <summary>The public converter entry point under test.</summary>
    private SystemTextJsonQueryConverter<QueryFlattenModel> _converter = null!;

    /// <summary>The representative object flattened by each benchmark.</summary>
    private QueryFlattenModel _model = null!;

    /// <summary>Builds the settings, options, and payload before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var serializer = new SystemTextJsonContentSerializer();
        _settings = new(serializer);
        _options = serializer.SerializerOptions;
        _converter = new();
        _model = new()
        {
            Id = SampleId,
            Name = "widgets and gadgets",
            Active = true,
            CreatedAt = new(2026, 7, 15, 12, 30, 0, TimeSpan.Zero),
            Address = new() { City = "Melbourne", Zip = "3000" },
        };
        _model.Tags.Add("alpha");
        _model.Tags.Add("beta");
        _model.Tags.Add("gamma");
    }

    /// <summary>Flattens the object graph directly through the flattening engine.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int FlattenObject()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        SystemTextJsonQueryFlattener.FlattenObject(_model, "filter.", ref builder, _settings, _options, 0);
        return builder.Build().Length;
    }

    /// <summary>Flattens the object graph through the public converter entry point.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int ConverterFlatten()
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        _converter.Flatten(_model, "filter.", ref builder, _settings);
        return builder.Build().Length;
    }
}
