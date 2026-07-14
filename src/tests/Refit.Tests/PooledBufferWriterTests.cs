// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using Refit.Buffers;

namespace Refit.Tests;

/// <summary>Tests for <see cref="PooledBufferWriter"/> and its detached stream.</summary>
[SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Performance", "PSH1313:Blocking call should be awaited", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Performance", "PSH1314:Use the Memory-based ReadAsync overload", Justification = "These tests intentionally exercise the byte-array Stream overrides.")]
public class PooledBufferWriterTests
{
    /// <summary>A sample byte marker with the value two.</summary>
    private const byte MarkerByteTwo = 2;

    /// <summary>A sample byte marker with the value three.</summary>
    private const byte MarkerByteThree = 3;

    /// <summary>A sample byte marker with the value ten.</summary>
    private const byte MarkerByteTen = 10;

    /// <summary>A sample byte marker with the value twenty.</summary>
    private const byte MarkerByteTwenty = 20;

    /// <summary>The number of bytes in a two-byte sample buffer.</summary>
    private const int TwoByteCount = 2;

    /// <summary>The number of bytes in a three-byte sample buffer.</summary>
    private const int ThreeByteCount = 3;

    /// <summary>Verifies written bytes are preserved when the writer grows beyond its initial rented buffer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetMemoryGrowsAndPreservesWrittenBytes()
    {
        using var writer = new PooledBufferWriter();
        var first = writer.GetSpan(PooledBufferWriter.DefaultSize);
        first[0] = 1;
        first[PooledBufferWriter.DefaultSize - 1] = MarkerByteTwo;
        writer.Advance(PooledBufferWriter.DefaultSize);

        var second = writer.GetSpan(1);
        second[0] = MarkerByteThree;
        writer.Advance(1);

        await using var stream = writer.DetachStream();
        var buffer = new byte[PooledBufferWriter.DefaultSize + 1];

        var read = stream.Read(buffer, 0, buffer.Length);

        await Assert.That(read).IsEqualTo(buffer.Length);
        await Assert.That(buffer[0]).IsEqualTo((byte)1);
        await Assert.That(buffer[PooledBufferWriter.DefaultSize - 1]).IsEqualTo(MarkerByteTwo);
        await Assert.That(buffer[PooledBufferWriter.DefaultSize]).IsEqualTo(MarkerByteThree);
    }

    /// <summary>Verifies zero-sized span and memory requests still reserve at least one byte.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetMemoryAndSpanWithZeroSizeHintReturnWritableBuffers()
    {
        var (memoryLength, spanLength) = GetZeroSizeHintBufferLengths();

        await Assert.That(memoryLength).IsGreaterThanOrEqualTo(1);
        await Assert.That(spanLength).IsGreaterThanOrEqualTo(1);
    }

    /// <summary>Verifies invalid advances throw the expected argument exceptions.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AdvanceValidatesCount()
    {
        using var writer = new PooledBufferWriter();

        await Assert.That(() => writer.Advance(-1)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => writer.Advance(PooledBufferWriter.DefaultSize + 1))
            .ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => writer.GetMemory(-1)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => writer.GetSpan(-1)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies a detached stream reads only the bytes advanced into the writer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamReadByteStopsAtUsedLength()
    {
        using var writer = new PooledBufferWriter();
        var span = writer.GetSpan(TwoByteCount);
        span[0] = MarkerByteTen;
        span[1] = MarkerByteTwenty;
        writer.Advance(TwoByteCount);

        await using var stream = writer.DetachStream();

        await Assert.That(stream.ReadByte()).IsEqualTo(MarkerByteTen);
        await Assert.That(stream.ReadByte()).IsEqualTo(MarkerByteTwenty);
        await Assert.That(stream.ReadByte()).IsEqualTo(-1);
    }

    /// <summary>Verifies the detached stream validates read ranges before copying.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamReadValidatesArguments()
    {
        using var writer = CreateWriter(1, MarkerByteTwo, MarkerByteThree);
        await using var stream = writer.DetachStream();
        var buffer = new byte[2];

        await Assert.That(() => stream.Read(buffer, -1, 1)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => stream.Read(buffer, 0, -1)).ThrowsExactly<ArgumentOutOfRangeException>();
        await Assert.That(() => stream.Read(buffer, 1, TwoByteCount)).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies detached stream metadata and partial reads.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamReportsLengthPositionAndPartialReads()
    {
        using var writer = CreateWriter(1, MarkerByteTwo, MarkerByteThree);
        await using var stream = writer.DetachStream();
        var buffer = new byte[2];

        stream.Flush();
        var firstRead = stream.Read(buffer, 0, buffer.Length);
        var secondRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var thirdRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        await stream.FlushAsync();

        await Assert.That(stream.Length).IsEqualTo(ThreeByteCount);
        await Assert.That(firstRead).IsEqualTo(TwoByteCount);
        await Assert.That(secondRead).IsEqualTo(1);
        await Assert.That(thirdRead).IsEqualTo(0);
        await Assert.That(stream.Position).IsEqualTo(ThreeByteCount);
        await Assert.That(buffer[0]).IsEqualTo(MarkerByteThree);
    }

    /// <summary>Verifies unsupported stream operations throw <see cref="NotSupportedException"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamUnsupportedOperationsThrow()
    {
        using var writer = CreateWriter(1, MarkerByteTwo, MarkerByteThree);
        await using var stream = writer.DetachStream();

        await Assert.That(stream.CanRead).IsTrue();
        await Assert.That(stream.CanSeek).IsFalse();
        await Assert.That(stream.CanWrite).IsFalse();
        await Assert.That(() => stream.Position = 1).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.Seek(0, SeekOrigin.Begin)).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.SetLength(0)).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.Write([1], 0, 1)).ThrowsExactly<NotSupportedException>();
    }

    /// <summary>Verifies detached stream async methods observe cancellation without reading.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamAsyncMethodsHonorCancellation()
    {
        using var writer = CreateWriter(1, MarkerByteTwo, MarkerByteThree);
        await using var stream = writer.DetachStream();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        const int copyBufferSize = 81_920;

        await Assert.That(() => stream.FlushAsync(cancellationTokenSource.Token))
            .ThrowsExactly<TaskCanceledException>();
        await Assert.That(() => stream.ReadAsync(new byte[ThreeByteCount], 0, ThreeByteCount, cancellationTokenSource.Token))
            .ThrowsExactly<TaskCanceledException>();
        await Assert.That(() => stream.CopyToAsync(Stream.Null, copyBufferSize, cancellationTokenSource.Token))
            .ThrowsExactly<OperationCanceledException>();
#if NET6_0_OR_GREATER
        await Assert.That(() => stream.ReadAsync(new byte[ThreeByteCount].AsMemory(), cancellationTokenSource.Token).AsTask())
            .ThrowsExactly<TaskCanceledException>();
#endif
    }

    /// <summary>Verifies a disposed detached stream rejects further reads.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamThrowsAfterDispose()
    {
        using var writer = CreateWriter(1);
        var stream = writer.DetachStream();
        await stream.DisposeAsync();

        await Assert.That(stream.ReadByte).ThrowsExactly<ObjectDisposedException>();
        await Assert.That(() => stream.Read(new byte[1], 0, 1)).ThrowsExactly<ObjectDisposedException>();
    }

    /// <summary>Verifies disposed detached stream async paths surface object-disposed failures.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamAsyncMethodsThrowAfterDispose()
    {
        using var writer = CreateWriter(1);
        var stream = writer.DetachStream();
        await stream.DisposeAsync();

        await Assert.That(() => stream.CopyToAsync(Stream.Null)).ThrowsExactly<ObjectDisposedException>();
        await Assert.That(() => stream.ReadAsync(new byte[1], 0, 1))
            .ThrowsExactly<ObjectDisposedException>();
#if NET6_0_OR_GREATER
        await Assert.That(() => stream.ReadAsync(new byte[1].AsMemory()).AsTask())
            .ThrowsExactly<ObjectDisposedException>();
#endif
    }

#if NET6_0_OR_GREATER
    /// <summary>Verifies span-based detached stream reads consume only available bytes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DetachedStreamSpanReadStopsAtLength()
    {
        using var writer = CreateWriter(1, MarkerByteTwo, MarkerByteThree);
        await using var stream = writer.DetachStream();
        var buffer = new byte[4];
        const int thirdElementIndex = 2;
        const int fourthElementIndex = 3;

        var firstRead = stream.Read(buffer.AsSpan(0, TwoByteCount));
        var secondRead = await stream.ReadAsync(buffer.AsMemory(thirdElementIndex, TwoByteCount));
        var thirdRead = stream.Read(buffer);

        await Assert.That(firstRead).IsEqualTo(TwoByteCount);
        await Assert.That(secondRead).IsEqualTo(1);
        await Assert.That(thirdRead).IsEqualTo(0);
        await Assert.That(buffer[0]).IsEqualTo((byte)1);
        await Assert.That(buffer[1]).IsEqualTo(MarkerByteTwo);
        await Assert.That(buffer[thirdElementIndex]).IsEqualTo(MarkerByteThree);
        await Assert.That(buffer[fourthElementIndex]).IsEqualTo((byte)0);
    }
#endif

    /// <summary>Creates a writer containing the provided bytes.</summary>
    /// <param name="values">The bytes to write.</param>
    /// <returns>A writer advanced by the number of provided bytes.</returns>
    private static PooledBufferWriter CreateWriter(params byte[] values)
    {
        var writer = new PooledBufferWriter();
        values.CopyTo(writer.GetSpan(values.Length));
        writer.Advance(values.Length);
        return writer;
    }

    /// <summary>Gets buffer lengths from zero-size requests without carrying spans across async state-machine boundaries.</summary>
    /// <returns>The memory and span lengths.</returns>
    private static (int MemoryLength, int SpanLength) GetZeroSizeHintBufferLengths()
    {
        using var writer = new PooledBufferWriter();

        var memory = writer.GetMemory();
        memory.Span[0] = 1;
        var span = writer.GetSpan();
        span[0] = MarkerByteTwo;

        return (memory.Length, span.Length);
    }
}
