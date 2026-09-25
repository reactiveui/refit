// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Testing;

/// <summary>
/// Wraps a request body without reading it. Whichever way the reply code consumes the body, as a stream or by copying it
/// out, the bytes pass through to the reader and are recorded up to the capture limit.
/// </summary>
internal sealed class CapturingContent : HttpContent
{
    /// <summary>The original request body.</summary>
    private readonly HttpContent _inner;

    /// <summary>The record filled as the body is read.</summary>
    private readonly CapturedRequestBody _capture;

    /// <summary>Initializes a new instance of the <see cref="CapturingContent"/> class, copying the original content headers.</summary>
    /// <param name="inner">The original request body.</param>
    /// <param name="capture">The record filled as the body is read.</param>
    internal CapturingContent(HttpContent inner, CapturedRequestBody capture)
    {
        _inner = inner;
        _capture = capture;
        foreach (var header in inner.Headers)
        {
            if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                _ = Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        // The copy target belongs to the caller; the wrapper owns nothing and needs no disposal.
        await _inner.CopyToAsync(new CaptureStream(stream, _capture, ownsInner: false)).ConfigureAwait(false);
        _capture.Complete();
    }

#if NET8_0_OR_GREATER
    /// <inheritdoc/>
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        // Pass the token on so a streaming body being recorded still observes the send's cancellation.
        await _inner.CopyToAsync(new CaptureStream(stream, _capture, ownsInner: false), cancellationToken).ConfigureAwait(false);
        _capture.Complete();
    }
#endif

    /// <inheritdoc/>
    protected override async Task<Stream> CreateContentReadStreamAsync() =>
        new CaptureStream(await _inner.ReadAsStreamAsync().ConfigureAwait(false), _capture, ownsInner: true);

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        var known = _inner.Headers.ContentLength;
        length = known.GetValueOrDefault();
        return known.HasValue;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _inner.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>Passes reads or writes through to another stream and records the bytes that pass.</summary>
    /// <param name="inner">The stream read from or written to.</param>
    /// <param name="capture">The record to fill.</param>
    /// <param name="ownsInner">
    /// Whether disposing the wrapper disposes <paramref name="inner"/>: true for a body read stream, false for a copy
    /// target owned by the caller.
    /// </param>
    internal sealed class CaptureStream(Stream inner, CapturedRequestBody capture, bool ownsInner) : Stream
    {
        /// <inheritdoc/>
        public override bool CanRead => inner.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => inner.CanWrite;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush() => inner.Flush();

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => Record(buffer.AsSpan(offset, inner.Read(buffer, offset, count)));

#if NET8_0_OR_GREATER
        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            return Record(buffer.Span[..read]);
        }
#else
        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Record(buffer.AsSpan(offset, await inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false)));
#endif

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
            capture.Append(buffer.AsSpan(offset, count));
        }

#if NET8_0_OR_GREATER
        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            capture.Append(buffer.Span);
        }
#else
        /// <inheritdoc/>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await inner.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            capture.Append(buffer.AsSpan(offset, count));
        }
#endif

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (ownsInner)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>Records bytes just read, completing the record at the end of the body.</summary>
        /// <param name="read">The bytes just read; empty at the end of the body.</param>
        /// <returns>The number of bytes read.</returns>
        private int Record(ReadOnlySpan<byte> read)
        {
            if (read.IsEmpty)
            {
                capture.Complete();
            }
            else
            {
                capture.Append(read);
            }

            return read.Length;
        }
    }
}
