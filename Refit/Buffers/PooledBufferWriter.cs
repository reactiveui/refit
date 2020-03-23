using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Refit.Buffers
{
    /// <summary>
    /// A <see langword="struct"/> that provides a fast implementation of a binary writer, leveraging <see cref="ArrayPool{T}"/> for memory pooling
    /// </summary>
    internal sealed partial class PooledBufferWriter : IBufferWriter<byte>, IDisposable
    {
        /// <summary>
        /// The default size to use to create new <see cref="PooledBufferWriter"/> instances
        /// </summary>
        public const int DefaultSize = 1024;

        /// <summary>
        /// The <see cref="byte"/> array current in use
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// The current position into <see cref="buffer"/>
        /// </summary>
        private int position;

        /// <summary>
        /// Creates a new <see cref="PooledBufferWriter"/> instance
        /// </summary>
        public PooledBufferWriter()
        {
            buffer = ArrayPool<byte>.Shared.Rent(DefaultSize);
            position = 0;
        }

        /// <inheritdoc/>
        public void Advance(int count)
        {
            if (count < 0) ThrowArgumentOutOfRangeExceptionForNegativeCount();
            if (position > buffer.Length - count) ThrowArgumentOutOfRangeExceptionForAdvancedTooFar();

            position += count;
        }

        /// <inheritdoc/>
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureFreeCapacity(sizeHint);

            return buffer.AsMemory(position);
        }

        /// <inheritdoc/>
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureFreeCapacity(sizeHint);

            return buffer.AsSpan(position);
        }

        /// <summary>
        /// Ensures the buffer in use has the free capacity to contain the specified amount of new data
        /// </summary>
        /// <param name="count">The size in bytes of the new data to insert into the buffer</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureFreeCapacity(int count)
        {
            if (count < 0) ThrowArgumentOutOfRangeExceptionForNegativeCount();

            if (count == 0) count = 1;

            int
                currentLength = buffer.Length,
                freeCapacity = currentLength - position;

            if (count <= freeCapacity) return;

            int
                growBy = Math.Max(count, currentLength),
                newSize = checked(currentLength + growBy);

            var rent = ArrayPool<byte>.Shared.Rent(newSize);

            Array.Copy(buffer, rent, position);

            ArrayPool<byte>.Shared.Return(buffer);

            buffer = rent;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (buffer == null) return;

            ArrayPool<byte>.Shared.Return(buffer);
        }

        /// <summary>
        /// Gets a readable <see cref="Stream"/> for the current instance, by detaching the used buffer
        /// </summary>
        /// <returns>A readable <see cref="Stream"/> with the contents of the current instance</returns>
        public Stream DetachStream()
        {
            var stream = new PooledMemoryStream(this);

            buffer = null;

            return stream;
        }
    }
}
