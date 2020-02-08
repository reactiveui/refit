using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Refit
{
    /// <summary>
    /// A <see langword="class"/> implementing <see cref="IContentSerializer"/> using the System.Text.Json APIs
    /// </summary>
    public sealed class SystemTextJsonContentSerializer : IContentSerializer
    {
        /// <summary>
        /// The JSON serialization settings to use
        /// </summary>
        private readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance
        /// </summary>
        public SystemTextJsonContentSerializer() : this(new JsonSerializerOptions()) { }

        /// <summary>
        /// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance with the specified parameters
        /// </summary>
        /// <param name="jsonSerializerOptions">The serialization settings to use for the current instance</param>
        public SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.jsonSerializerOptions = jsonSerializerOptions;
        }

        /// <inheritdoc/>
        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var utf8Stream = new PooledMemoryStream();
            var utf8JsonWriter = new Utf8JsonWriter(utf8Stream);

            JsonSerializer.Serialize(utf8JsonWriter, item, jsonSerializerOptions);

            var content = new StreamContent(utf8Stream)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json") { CharSet = Encoding.UTF8.WebName }
                }
            };

            return Task.FromResult<HttpContent>(content);
        }

        /// <inheritdoc/>
        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            var buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
            try
            {
                var length = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                return Deserialize<T>(buffer, length, jsonSerializerOptions);

            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Deserializes an item of a specific type from a given UTF8 buffer
        /// </summary>
        /// <typeparam name="T">The type of item to deserialize</typeparam>
        /// <param name="buffer">The input buffer of UTF8 bytes to read</param>
        /// <param name="length">The length of the usable data within <paramref name="buffer"/></param>
        /// <param name="jsonSerializerOptions">The JSON serialization settings to use</param>
        /// <returns>A <typeparamref name="T"/> item deserialized from the UTF8 bytes within <paramref name="buffer"/></returns>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T Deserialize<T>(byte[] buffer, int length, JsonSerializerOptions jsonSerializerOptions)
        {
            var span = new ReadOnlySpan<byte>(buffer, 0, length);
            var utf8JsonReader = new Utf8JsonReader(span);

            return JsonSerializer.Deserialize<T>(ref utf8JsonReader, jsonSerializerOptions);
        }
    }

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
