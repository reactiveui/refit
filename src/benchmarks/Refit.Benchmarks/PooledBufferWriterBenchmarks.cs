// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Refit.Buffers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the pooled <c>PooledBufferWriter</c> and its detached read-only stream: writing
/// within the default rented buffer, writing past it to force pooled growth, and detaching the buffer into a
/// <c>PooledMemoryStream</c> that is then read to completion.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
public class PooledBufferWriterBenchmarks
{
    /// <summary>A byte count that fits within the writer's default rented buffer.</summary>
    private const int SmallByteCount = 512;

    /// <summary>A byte count that forces the writer to grow past its default rented buffer.</summary>
    private const int LargeByteCount = 8192;

    /// <summary>The chunk size requested from the writer on each write.</summary>
    private const int ChunkSize = 256;

    /// <summary>The fill byte written into the buffer.</summary>
    private readonly byte _fillByte = (byte)'x';

    /// <summary>A reused destination buffer for draining the detached stream.</summary>
    private readonly byte[] _readBuffer = new byte[LargeByteCount];

    /// <summary>Writes a payload that fits within the default rented buffer, then returns it to the pool.</summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark(Baseline = true)]
    public int WriteSmall()
    {
        using var writer = new PooledBufferWriter();
        Fill(writer, SmallByteCount);
        return SmallByteCount;
    }

    /// <summary>Writes a payload that overflows the default buffer, forcing pooled growth, then returns it to the pool.</summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark]
    public int WriteLargeGrowing()
    {
        using var writer = new PooledBufferWriter();
        Fill(writer, LargeByteCount);
        return LargeByteCount;
    }

    /// <summary>Writes a payload, detaches it into a stream, and drains the stream to completion.</summary>
    /// <returns>The number of bytes read back from the detached stream.</returns>
    [Benchmark]
    public int WriteDetachAndRead()
    {
        using var writer = new PooledBufferWriter();
        Fill(writer, LargeByteCount);
        using var stream = writer.DetachStream();

        var total = 0;
        int read;
        while ((read = stream.Read(_readBuffer, 0, _readBuffer.Length)) > 0)
        {
            total += read;
        }

        return total;
    }

    /// <summary>Writes the given number of fill bytes into the writer in fixed-size chunks.</summary>
    /// <param name="writer">The buffer writer to fill.</param>
    /// <param name="byteCount">The number of bytes to write.</param>
    private void Fill(PooledBufferWriter writer, int byteCount)
    {
        var remaining = byteCount;
        while (remaining > 0)
        {
            var span = writer.GetSpan(ChunkSize);
            var take = Math.Min(span.Length, remaining);
            span[..take].Fill(_fillByte);
            writer.Advance(take);
            remaining -= take;
        }
    }
}
