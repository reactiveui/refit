using System;
using System.Buffers;
using System.IO;

namespace Refit.IO
{
    /// <summary>
    /// An in-memory <see cref="Stream"/> that uses memory buffers rented from a shared pool
    /// </summary>
    public sealed class PooledMemoryStream : Stream
    {
        /// <summary>
        /// The default size for the buffer to use to write data
        /// </summary>
        private const int DefaultBufferSize = 1024;

        /// <summary>
        /// The buffer rented from <see cref="ArrayPool{T}"/> currently in use
        /// </summary>
        private byte[] pooledBuffer;

        /// <summary>
        /// The current position within <see cref="pooledBuffer"/>
        /// </summary>
        private int position;

        /// <summary>
        /// The current used length for <see cref="pooledBuffer"/>
        /// </summary>
        private int length;

        /// <summary>
        /// Creates a new <see cref="PooledMemoryStream"/> instance
        /// </summary>
        public PooledMemoryStream()
        {
            pooledBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
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
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => length;

        /// <inheritdoc/>
        public override long Position
        {
            get => position;
            set
            {
                if (value < 0 || value >= length)
                {
                    throw new ArgumentOutOfRangeException(nameof(Position), value, $"The position must be in the [0, {length}) range");
                }

                position = (int)value;
            }

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
            return origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => Position = length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "The position was outside the buffer length")
            };
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("The lenght can't be externally set for this stream type");
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "The offset can't be negative");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "The count can't be negative");
            if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is larger than the buffer length");
            if (pooledBuffer is null) throw new ObjectDisposedException(nameof(PooledMemoryStream));
            if (position != length) throw new InvalidOperationException("Writing not in sequential mode is not supported");

            var targetLength = position + count;

            // Expand the pooled buffer, if needed
            if (targetLength > pooledBuffer.Length)
            {
                var expandedBuffer = ArrayPool<byte>.Shared.Rent(targetLength);

                pooledBuffer.AsSpan(0, length).CopyTo(expandedBuffer);

                ArrayPool<byte>.Shared.Return(pooledBuffer);

                pooledBuffer = expandedBuffer;
            }

            var source = buffer.AsSpan(offset, count);
            var destination = pooledBuffer.AsSpan(position);

            source.CopyTo(destination);

            position = length = targetLength;
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
