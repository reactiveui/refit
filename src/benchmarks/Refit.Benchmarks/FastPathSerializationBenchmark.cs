// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>Compares sync JSON serialization: source-gen fast-path vs metadata walker vs Refit's default options (not fast-path eligible).</summary>
[MemoryDiagnoser]
public class FastPathSerializationBenchmark
{
    /// <summary>The small payload item count.</summary>
    private const int SmallItemCount = 10;

    /// <summary>The large payload item count.</summary>
    private const int LargeItemCount = 1000;

    /// <summary>The payload serialized by each benchmark.</summary>
    private List<FastItem> _items = null!;

    /// <summary>Refit's default (not fast-path eligible) options.</summary>
    private JsonSerializerOptions _defaultOptions = null!;

    /// <summary>Gets or sets the number of items in the payload.</summary>
    [Params(SmallItemCount, LargeItemCount)]
    public int Count { get; set; }

    /// <summary>Builds the payload and the reflection options before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _items = new(Count);
        for (var i = 0; i < Count; i++)
        {
            _items.Add(new() { Id = i, Name = "name" });
        }

        _defaultOptions = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();
    }

    /// <summary>Serializes using the source-generated fast-path writer.</summary>
    /// <returns>The serialized length.</returns>
    [Benchmark(Baseline = true)]
    public int FastPath() => JsonSerializer.Serialize(_items, FastPathSerializationContext.Default.ListFastItem).Length;

    /// <summary>Serializes using the source-generated metadata walker.</summary>
    /// <returns>The serialized length.</returns>
    [Benchmark]
    public int Metadata() => JsonSerializer.Serialize(_items, MetadataSerializationContext.Default.ListFastItem).Length;

    /// <summary>Serializes using Refit's default reflection-based options (converters + NumberHandling, no fast-path).</summary>
    /// <returns>The serialized length.</returns>
    [Benchmark]
    public int RefitDefault() => JsonSerializer.Serialize(_items, _defaultOptions).Length;
}
