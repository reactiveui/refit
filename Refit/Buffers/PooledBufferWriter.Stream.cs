using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Refit.Buffers
{
    internal sealed partial class PooledBufferWriter
    {
        /// <summary>
        /// An in-memory <see cref="Stream"/> that uses memory buffers rented from a shared pool
        /// </summary>
        private sealed partial class PooledMemoryStream : Stream
        {
            /// <summary>
            /// The current used length for <see cref="pooledBuffer"/>
            /// </summary>
            private readonly int length;

            /// <summary>
            /// The buffer rented from <see cref="ArrayPool{T}"/> currently in use
            /// </summary>
            private byte[]? pooledBuffer;

            /// <summary>
            /// The current position within <see cref="pooledBuffer"/>
            /// </summary>
            private int position;

            /// <summary>
            /// Creates a new <see cref="PooledMemoryStream"/> instance
            /// </summary>
            public PooledMemoryStream(PooledBufferWriter writer)
            {
                length = writer.position;
                pooledBuffer = writer.buffer;
            }

            /// <summary>
            /// Releases the resources for the current <see cref="PooledMemoryStream"/> instance
            /// </summary>
            ~PooledMemoryStream()
            {
                Dispose(true);
            }

            /// <inheritdoc/>
            public override bool CanRead => true;

            /// <inheritdoc/>
            public override bool CanSeek => false;

            /// <inheritdoc/>
            public override bool CanWrite => false;

            /// <inheritdoc/>
            public override long Length => length;

            /// <inheritdoc/>
            public override long Position
            {
                get => position;
                set => ThrowNotSupportedException();

            }

            /// <inheritdoc/>
            public override void Flush() { }

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
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                try
                {
#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                    return CopyToInternalAsync(destination, cancellationToken);
#else
                    CopyTo(destination, bufferSize);
                    return Task.CompletedTask;
#endif
                }
                catch (OperationCanceledException e)
                {
                    return Task.FromCanceled(e.CancellationToken);
                }
                catch (Exception e)
                {
                    return Task.FromException(e);
                }
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (offset < 0) ThrowArgumentOutOfRangeExceptionForNegativeOffset();
                if (count < 0) ThrowArgumentOutOfRangeExceptionForNegativeCount();
                if (offset + count > buffer.Length) ThrowArgumentOutOfRangeExceptionForEndOfStreamReached();
                if (pooledBuffer is null) ThrowObjectDisposedException();

                var destination = buffer.AsSpan(offset, count);
                var source = pooledBuffer.AsSpan(0, length).Slice(position);

                // If the source is contained within the destination, copy the entire span
                if (source.Length <= destination.Length)
                {
                    source.CopyTo(destination);

                    position += source.Length;

                    return source.Length;
                }

                // Resize the source slice and only copy the overlapping region
                source.Slice(0, destination.Length).CopyTo(destination);

                position += destination.Length;

                return destination.Length;
            }

            /// <inheritdoc/>
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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
                catch (OperationCanceledException e)
                {
                    return Task.FromCanceled<int>(e.CancellationToken);
                }
                catch (Exception e)
                {
                    return Task.FromException<int>(e);
                }
            }

            /// <inheritdoc/>
            public override int ReadByte()
            {
                if (pooledBuffer is null) ThrowObjectDisposedException();

                if (position >= pooledBuffer!.Length)
                {
                    return -1;
                }

                return pooledBuffer[position++];
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                ThrowNotSupportedException();

                return default;
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                ThrowNotSupportedException();
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                ThrowNotSupportedException();
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (pooledBuffer == null) return;

                GC.SuppressFinalize(this);

                ArrayPool<byte>.Shared.Return(pooledBuffer);

                pooledBuffer = null;
            }
        }
    }
}
