// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Refit.Tests;

/// <summary>
/// Verifies <see cref="JsonLinesContent{T}"/> streams a large asynchronous sequence with bounded lag rather than
/// buffering it: the producer never gets more than one record ahead of what has already been written to the
/// destination stream. Every record serializes to the same number of bytes (a fixed-width identifier), so the
/// exact number of fully-written records at any point can be computed from a running byte count, without depending
/// on how the underlying writer chunks its output.
/// </summary>
public sealed class JsonLinesUploadBoundedMemoryTests
{
    /// <summary>The number of records streamed through the content.</summary>
    private const int RecordCount = 50_000;

    /// <summary>The zero-padded width of every record's identifier, keeping every serialized line the same length.</summary>
    private const int IdWidth = 6;

    /// <summary>
    /// Verifies that, at the moment the producer is about to yield record <c>i</c>, the destination stream has
    /// already received the complete bytes of at least record <c>i - 1</c> (lag at most one record), for every
    /// record in a 50,000-element sequence.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ProducerNeverRunsMoreThanOneRecordAheadOfTheStream()
    {
        var serializer = new SystemTextJsonContentSerializer();
        var lineLength = await SerializedLineLengthAsync(serializer);
        var counting = new CountingStream();
        var lagViolations = new List<int>();

        using var content = new JsonLinesContent<LagRecord>(ProduceAsync(RecordCount, counting, lineLength, lagViolations), serializer);

        await using (var target = new NullTargetStream())
        {
            // Route writes through the counting stream first so the running total observed by the producer reflects
            // exactly what has reached "the network" so far.
            await content.CopyToAsync(new TeeStream(counting, target));
        }

        await Assert.That(lagViolations).IsEmpty();
        await Assert.That(counting.Written).IsEqualTo(ExpectedBytesAfter(RecordCount, lineLength));
    }

    /// <summary>Computes the exact byte length of one serialized record.</summary>
    /// <param name="serializer">The serializer used by the content under test.</param>
    /// <returns>The number of bytes one serialized record occupies (every record has the same length by construction).</returns>
    private static async Task<int> SerializedLineLengthAsync(SystemTextJsonContentSerializer serializer)
    {
        using var sample = serializer.ToHttpContent(new LagRecord(FormatId(0)));
        var bytes = await sample.ReadAsByteArrayAsync();
        return bytes.Length;
    }

    /// <summary>Formats a fixed-width identifier so every record serializes to the same number of bytes.</summary>
    /// <param name="index">The record index.</param>
    /// <returns>The zero-padded identifier.</returns>
    private static string FormatId(int index) => index.ToString($"D{IdWidth}", CultureInfo.InvariantCulture);

    /// <summary>Computes the exact cumulative byte count expected once <paramref name="completed"/> records, separated by single-byte line breaks, have been written.</summary>
    /// <param name="completed">The number of fully-written records.</param>
    /// <param name="lineLength">The byte length of one serialized record.</param>
    /// <returns>The expected cumulative byte count.</returns>
    private static long ExpectedBytesAfter(int completed, int lineLength) =>
        completed == 0 ? 0 : ((long)completed * lineLength) + (completed - 1);

    /// <summary>Produces records, asserting before each one (after the first) that the stream has already received at least the previous record's bytes.</summary>
    /// <param name="count">The number of records to produce.</param>
    /// <param name="counting">The stream tracking bytes written so far.</param>
    /// <param name="lineLength">The byte length of one serialized record.</param>
    /// <param name="lagViolations">Receives the index of any record produced before the stream caught up to the one before it.</param>
    /// <param name="cancellationToken">The token bound to the enumerator by the sending content.</param>
    /// <returns>The asynchronous sequence of records.</returns>
    private static async IAsyncEnumerable<LagRecord> ProduceAsync(
        int count,
        CountingStream counting,
        int lineLength,
        List<int> lagViolations,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        for (var i = 0; i < count; i++)
        {
            if (i > 0 && counting.Written < ExpectedBytesAfter(i - 1, lineLength))
            {
                lagViolations.Add(i);
            }

            cancellationToken.ThrowIfCancellationRequested();
            yield return new(FormatId(i));
        }
    }

    /// <summary>A write-only stream that only counts the bytes written to it, without retaining them.</summary>
    private sealed class CountingStream : Stream
    {
        /// <summary>The running total of bytes written.</summary>
        private long _written;

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>Gets the total number of bytes written so far.</summary>
        internal long Written => Interlocked.Read(ref _written);

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => _ = Interlocked.Add(ref _written, count);

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _ = Interlocked.Add(ref _written, buffer.Length);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>A write-only stream that discards everything written to it.</summary>
    private sealed class NullTargetStream : Stream
    {
        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    /// <summary>A write-only stream that duplicates every write to two destination streams.</summary>
    /// <param name="first">The first destination, whose running total the producer inspects.</param>
    /// <param name="second">The second destination, which discards the bytes.</param>
    private sealed class TeeStream(Stream first, Stream second) : Stream
    {
        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            first.Write(buffer, offset, count);
            second.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await first.WriteAsync(buffer, cancellationToken);
            await second.WriteAsync(buffer, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>A fixed-shape record whose only member is a fixed-width identifier, keeping every serialized line the same length.</summary>
    /// <param name="Id">The zero-padded identifier.</param>
    public sealed record LagRecord(string Id);
}
