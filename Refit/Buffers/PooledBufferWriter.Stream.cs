using System;
using System.Buffers;
using System.IO;

namespace Refit.Buffers
{
    internal sealed partial class PooledBufferWriter
    {
        /// <summary>
        /// An in-memory <see cref="Stream"/> that uses memory buffers rented from a shared pool
        /// </summary>
        private sealed class PooledMemoryStream : Stream
        {
            /// <summary>
            /// The current used length for <see cref="pooledBuffer"/>
            /// </summary>
            private readonly int length;

            /// <summary>
            /// The buffer rented from <see cref="ArrayPool{T}"/> currently in use
            /// </summary>
            private byte[] pooledBuffer;

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
                position = 0;
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
                set => throw new NotSupportedException("This stream doesn't support seek operations");

            }

            /// <inheritdoc/>
            public override void Flush() { }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "The offset can't be negative");
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "The count can't be negative");
                if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is larger than the buffer length");
                if (pooledBuffer is null) throw new ObjectDisposedException(nameof(PooledMemoryStream));

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
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException("This stream doesn't support seek operations");
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                throw new NotSupportedException("The lenght can't be externally set for this stream type");
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException("This stream doesn't support write operations");
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
