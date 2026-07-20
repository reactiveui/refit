// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for <see cref="GeneratedQueryStringBuilder"/>, the reflection-free ref-struct that
/// source-generated request construction uses to append query parameters. Each benchmark drives one appender shape
/// (escaped scalar, pre-escaped key, span-formatted value, joined and multi collections, and a valueless flag) and
/// materializes the result so the buffer work is not elided.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
public class QueryStringBuildingBenchmarks
{
    /// <summary>A representative integer value span-formatted into query strings.</summary>
    private const int SampleInteger = 1_234_567;

    /// <summary>A representative timestamp whose invariant form contains reserved characters.</summary>
    private static readonly DateTimeOffset _timestamp = new(2026, 7, 15, 12, 30, 0, TimeSpan.Zero);

    /// <summary>The relative path each benchmark appends its query string to.</summary>
    private readonly string _path = "/search";

    /// <summary>A scalar query value that requires percent-encoding.</summary>
    private readonly string _scalarValue = "widgets and gadgets";

    /// <summary>A representative collection of identifiers appended as a query collection.</summary>
    private readonly int[] _ids = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>Appends several escaped <c>key=value</c> scalar pairs.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int ScalarPairs()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.Add("q", _scalarValue, false);
        builder.Add("page", "3", false);
        builder.Add("size", "25", false);
        return builder.Build().Length;
    }

    /// <summary>Appends a scalar pair whose key the generator already escaped.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int PreEscapedKeyPair()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.AddPreEscapedKey("q", _scalarValue, false);
        return builder.Build().Length;
    }

    /// <summary>Appends an integer value span-formatted straight into the buffer.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int FormattedInt()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.AddFormatted("page", SampleInteger, null, false);
        return builder.Build().Length;
    }

    /// <summary>Appends a span-formatted timestamp that requires in-place escaping.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int FormattedTimestamp()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.AddFormatted("at", _timestamp, null, false);
        return builder.Build().Length;
    }

    /// <summary>Appends a comma-joined collection parameter.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int CsvCollection()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.BeginCollection("ids", CollectionFormat.Csv, false);
        foreach (var id in _ids)
        {
            builder.AddCollectionValueFormatted(id);
        }

        builder.EndCollection();
        return builder.Build().Length;
    }

    /// <summary>Appends a multi-expanded collection parameter (one pair per element).</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int MultiCollection()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.BeginCollection("ids", CollectionFormat.Multi, false);
        foreach (var id in _ids)
        {
            builder.AddCollectionValueFormatted(id);
        }

        builder.EndCollection();
        return builder.Build().Length;
    }

    /// <summary>Appends a valueless query flag.</summary>
    /// <returns>The built path length.</returns>
    [Benchmark]
    public int Flag()
    {
        var builder = new GeneratedQueryStringBuilder(_path);
        builder.AddFlag("ready to ship", false);
        return builder.Build().Length;
    }
}
