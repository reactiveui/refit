#if NETSTANDARD2_1

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Refit.Buffers
{
    internal sealed partial class PooledBufferWriter
    {
        private sealed partial class PooledMemoryStream : Stream
        {
            /// <inheritdoc/>
            public override void CopyTo(Stream destination, int bufferSize)
            {
                if (pooledBuffer is null) ThrowObjectDisposedException();

                var bytesAvailable = length - position;
                var spanLength = Math.Min(bytesAvailable, bufferSize);

                var source = pooledBuffer.AsSpan(position, spanLength);

                position += source.Length;

                destination.Write(source);
            }

            /// <inheritdoc/>
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
                }

                try
                {
                    var result = Read(buffer.Span);

                    return new ValueTask<int>(result);
                }
                catch (OperationCanceledException e)
                {
                    return new ValueTask<int>(Task.FromCanceled<int>(e.CancellationToken));
                }
                catch (Exception e)
                {
                    return new ValueTask<int>(Task.FromException<int>(e));
                }
            }

            /// <inheritdoc/>
            public override int Read(Span<byte> buffer)
            {
                if (pooledBuffer is null) ThrowObjectDisposedException();

                if (position >= length) return 0;

                var bytesAvailable = length - position;

                var source = pooledBuffer.AsSpan(position, bytesAvailable);

                var bytesCopied = Math.Min(source.Length, buffer.Length);

                var destination = buffer.Slice(0, bytesCopied);

                source.CopyTo(destination);

                position += bytesCopied;

                return bytesCopied;
            }
        }
    }
}

#endif
