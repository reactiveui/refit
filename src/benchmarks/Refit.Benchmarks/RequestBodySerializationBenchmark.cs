// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;

namespace Refit.Benchmarks;

/// <summary>Benchmarks producing a Refit JSON request body three ways.</summary>
[MemoryDiagnoser]
public class RequestBodySerializationBenchmark
{
    /// <summary>The small payload item count.</summary>
    private const int SmallItemCount = 10;

    /// <summary>The large payload item count.</summary>
    private const int LargeItemCount = 1000;

    /// <summary>The payload serialized by each benchmark.</summary>
    private List<FastItem> _items = null!;

    /// <summary>A serializer with fast-path-eligible options and a source-gen resolver.</summary>
    private SystemTextJsonContentSerializer _fastPath = null!;

    /// <summary>A serializer using Refit's default (not fast-path eligible) options.</summary>
    private SystemTextJsonContentSerializer _default = null!;

    /// <summary>Gets or sets the number of items in the payload.</summary>
    [Params(SmallItemCount, LargeItemCount)]
    public int Count { get; set; }

    /// <summary>Builds the payload and serializers before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _items = new(Count);
        for (var i = 0; i < Count; i++)
        {
            _items.Add(new() { Id = i, Name = "name" });
        }

        var fastPathOptions = SystemTextJsonContentSerializer.GetFastPathJsonSerializerOptions();
        fastPathOptions.TypeInfoResolver = FastPathSerializationContext.Default;
        _fastPath = new(fastPathOptions);
        _default = new();
    }

    /// <summary>Synchronous serialization with fast-path-eligible options (the fast-path engages).</summary>
    /// <returns>The number of bytes produced.</returns>
    [Benchmark(Baseline = true)]
    public Task<long> SyncFastPathAsync() =>
        ProduceAsync(((ISynchronousContentSerializer)_fastPath).ToHttpContentSynchronous(_items));

    /// <summary>Synchronous serialization with Refit's default options (no fast-path, due to converters and NumberHandling).</summary>
    /// <returns>The number of bytes produced.</returns>
    [Benchmark]
    public Task<long> SyncDefaultAsync() =>
        ProduceAsync(((ISynchronousContentSerializer)_default).ToHttpContentSynchronous(_items));

    /// <summary>Asynchronous serialization with Refit's default options (today's default request-body path).</summary>
    /// <returns>The number of bytes produced.</returns>
    [Benchmark]
    public Task<long> AsyncDefaultAsync() => ProduceAsync(_default.ToHttpContent(_items));

    /// <summary>Serializes the content to a buffer and returns the byte count.</summary>
    /// <param name="content">The HTTP content to materialize.</param>
    /// <returns>The number of bytes produced.</returns>
    private static async Task<long> ProduceAsync(HttpContent content)
    {
        using (content)
        {
            await using var stream = new MemoryStream();
            await content.CopyToAsync(stream).ConfigureAwait(false);
            return stream.Length;
        }
    }
}
