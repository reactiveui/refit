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
/// A response body with no <c>Content-Length</c> that reads a <see cref="StreamSource"/> as the test releases it.
/// Reading the body as a stream is incremental; buffering it waits for the source to complete.
/// </summary>
internal sealed class StreamSourceContent : HttpContent
{
    /// <summary>The single body stream handed to every reader of this content.</summary>
    private readonly StreamSourceReadStream _stream;

    /// <summary>Initializes a new instance of the <see cref="StreamSourceContent"/> class.</summary>
    /// <param name="source">The bound source.</param>
    internal StreamSourceContent(StreamSource source)
    {
        _stream = new(source);
        Headers.ContentType = new(source.MediaType);
    }

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => _stream.CopyToAsync(stream);

    /// <inheritdoc/>
    protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(_stream);

#if NET5_0_OR_GREATER
    /// <inheritdoc/>
    protected override Stream CreateContentReadStream(CancellationToken cancellationToken) => _stream;
#endif

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // Closing the source only signals the test; it is safe on either dispose path.
        _stream.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>The read side of a <see cref="StreamSource"/>: each read waits for the next released chunk.</summary>
    /// <param name="source">The bound source.</param>
    internal sealed class StreamSourceReadStream(StreamSource source) : Stream
    {
        /// <summary>The chunk being copied out, or <see langword="null"/> before the first read.</summary>
        private byte[]? _current;

        /// <summary>The next unread offset in <see cref="_current"/>.</summary>
        private int _offset;

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

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
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadCoreAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadCoreAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

#if NET8_0_OR_GREATER
        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ReadCoreAsync(buffer, cancellationToken);
#endif

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            source.Close();
            base.Dispose(disposing);
        }

        /// <summary>Copies from the current chunk, waiting for the next released chunk once it is exhausted.</summary>
        /// <param name="destination">The buffer to fill.</param>
        /// <param name="cancellationToken">A token that cancels the wait.</param>
        /// <returns>The number of bytes copied; 0 at the end of the body.</returns>
        private async ValueTask<int> ReadCoreAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            var current = _current;
            while (current is null || _offset == current.Length)
            {
                current = await source.ReadChunkAsync(cancellationToken).ConfigureAwait(false);
                if (current is null)
                {
                    return 0;
                }

                _current = current;
                _offset = 0;
            }

            var copied = Math.Min(destination.Length, current.Length - _offset);
            current.AsSpan(_offset, copied).CopyTo(destination.Span);
            _offset += copied;
            return copied;
        }
    }
}
