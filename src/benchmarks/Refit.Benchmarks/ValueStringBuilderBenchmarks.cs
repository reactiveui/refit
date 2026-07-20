// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the stack-allocated, pool-backed <c>ValueStringBuilder</c> used throughout request
/// path and query construction: appending characters, strings, and spans (including the pooled-buffer growth path),
/// reserving a span to fill directly, and inserting a prefix.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
public class ValueStringBuilderBenchmarks
{
    /// <summary>The stack buffer size that comfortably holds a small render.</summary>
    private const int RoomyStackBuffer = 256;

    /// <summary>A small stack buffer that overflows into pooled growth once several words are appended.</summary>
    private const int TightStackBuffer = 16;

    /// <summary>The words appended to build a larger value.</summary>
    private readonly string[] _words = ["alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta"];

    /// <summary>A representative text whose characters and span are appended.</summary>
    private readonly string _text = "a-representative-request-path-segment";

    /// <summary>A prefix inserted at the front of a built value.</summary>
    private readonly string _prefix = "/api/v3";

    /// <summary>Appends several strings within a roomy stack buffer, then materializes the result.</summary>
    /// <returns>The built string length.</returns>
    [Benchmark(Baseline = true)]
    public int AppendStrings()
    {
        var builder = new ValueStringBuilder(stackalloc char[RoomyStackBuffer]);
        foreach (var word in _words)
        {
            builder.Append(word);
        }

        return builder.ToString().Length;
    }

    /// <summary>Appends several strings into a tight buffer, forcing pooled-buffer growth.</summary>
    /// <returns>The built string length.</returns>
    [Benchmark]
    public int AppendStringsGrowing()
    {
        var builder = new ValueStringBuilder(stackalloc char[TightStackBuffer]);
        foreach (var word in _words)
        {
            builder.Append(word);
            builder.Append('-');
        }

        return builder.ToString().Length;
    }

    /// <summary>Appends a text character by character.</summary>
    /// <returns>The built string length.</returns>
    [Benchmark]
    public int AppendChars()
    {
        var builder = new ValueStringBuilder(stackalloc char[RoomyStackBuffer]);
        foreach (var character in _text)
        {
            builder.Append(character);
        }

        return builder.ToString().Length;
    }

    /// <summary>Appends a text span in one shot.</summary>
    /// <returns>The built string length.</returns>
    [Benchmark]
    public int AppendSpan()
    {
        var builder = new ValueStringBuilder(stackalloc char[RoomyStackBuffer]);
        builder.Append(_text.AsSpan());
        return builder.ToString().Length;
    }

    /// <summary>Reserves a span and fills it directly, avoiding an intermediate copy.</summary>
    /// <returns>The built string length.</returns>
    [Benchmark]
    public int ReserveAndFill()
    {
        var builder = new ValueStringBuilder(stackalloc char[RoomyStackBuffer]);
        var span = builder.AppendSpan(_text.Length);
        _text.AsSpan().CopyTo(span);
        return builder.ToString().Length;
    }

    /// <summary>Appends a text then inserts a prefix at the front.</summary>
    /// <returns>The built string length.</returns>
    [Benchmark]
    public int InsertPrefix()
    {
        var builder = new ValueStringBuilder(stackalloc char[RoomyStackBuffer]);
        builder.Append(_text);
        builder.Insert(0, _prefix);
        return builder.ToString().Length;
    }
}
