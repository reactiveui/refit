// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Buffers;

namespace Refit.Buffers;

/// <summary>A buffer writer that rents its backing storage from a shared array pool.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "RoslynCommonAnalyzers",
    "SST1432:Mark type as static",
    Justification = "The full type has instance members and implements IBufferWriter<byte> and IDisposable; this partial part only declares a nested type.")]
internal sealed partial class PooledBufferWriter
{
    /// <summary>An in-memory <see cref="Stream"/> that uses memory buffers rented from a shared pool.</summary>
#if NET6_0_OR_GREATER
    private sealed partial class PooledMemoryStream : Stream
#else
    private sealed class PooledMemoryStream : Stream
#endif
    {
        /// <summary>The current used length for <see cref="_pooledBuffer"/>.</summary>
        private readonly int _length;

        /// <summary>The buffer rented from <see cref="ArrayPool{T}"/> currently in use.</summary>
        private byte[]? _pooledBuffer;

        /// <summary>The current position within <see cref="_pooledBuffer"/>.</summary>
        private int _position;

        /// <summary>Initializes a new instance of the <see cref="PooledMemoryStream"/> class.</summary>
        /// <param name="writer">The <see cref="PooledBufferWriter"/> whose buffer is detached into the stream.</param>
        public PooledMemoryStream(PooledBufferWriter writer)
        {
            _length = writer._position;
            _pooledBuffer = writer._buffer;
        }

        /// <summary>Finalizes an instance of the <see cref="PooledMemoryStream"/> class.</summary>
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        ~PooledMemoryStream() => Dispose(false);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => _length;

        /// <inheritdoc/>
        public override long Position
        {
            get => _position;
            set
            {
                _ = value;
                ThrowNotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override
#if NET6_0_OR_GREATER
            async
#endif
            Task CopyToAsync(
            Stream destination,
            int bufferSize,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
#if NET6_0_OR_GREATER
                cancellationToken.ThrowIfCancellationRequested();
#else
                return Task.FromCanceled(cancellationToken);
#endif
            }

#if NET6_0_OR_GREATER
            await CopyToInternalAsync(destination, cancellationToken).ConfigureAwait(false);
#else
            try
            {
                CopyTo(destination, bufferSize);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException e)
            {
                return Task.FromCanceled(e.CancellationToken);
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
#endif
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0)
            {
                ThrowArgumentOutOfRangeExceptionForNegativeOffset();
            }

            if (count < 0)
            {
                ThrowArgumentOutOfRangeExceptionForNegativeCount();
            }

            if (offset + count > buffer.Length)
            {
                ThrowArgumentOutOfRangeExceptionForEndOfStreamReached();
            }

            if (_pooledBuffer is null)
            {
                ThrowObjectDisposedException();
            }

            var destination = buffer.AsSpan(offset, count);
            var source = _pooledBuffer.AsSpan(0, _length)[_position..];

            // If the source is contained within the destination, copy the entire span
            if (source.Length <= destination.Length)
            {
                source.CopyTo(destination);

                _position += source.Length;

                return source.Length;
            }

            // Resize the source slice and only copy the overlapping region
            source[..destination.Length].CopyTo(destination);

            _position += destination.Length;

            return destination.Length;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            try
            {
                var result = Read(buffer, offset, count);

                return Task.FromResult(result);
            }
            catch (Exception e)
            {
                return Task.FromException<int>(e);
            }
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            var pooledBuffer = _pooledBuffer;
            if (pooledBuffer is null)
            {
                ThrowObjectDisposedException();
            }

            if (_position >= _length)
            {
                return -1;
            }

            return pooledBuffer![_position++];
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowNotSupportedException();

            return default;
        }

        /// <inheritdoc/>
        public override void SetLength(long value) => ThrowNotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => ThrowNotSupportedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_pooledBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(_pooledBuffer);
                    _pooledBuffer = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
