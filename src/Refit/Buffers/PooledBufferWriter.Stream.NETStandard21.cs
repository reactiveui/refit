// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET6_0_OR_GREATER

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Refit.Buffers;

/// <summary>A buffer writer that rents its backing storage from a shared array pool.</summary>
internal sealed partial class PooledBufferWriter
{
    /// <summary>An in-memory <see cref="Stream"/> that uses memory buffers rented from a shared pool.</summary>
    internal sealed partial class PooledMemoryStream : Stream
    {
        /// <summary>Asynchronously copies the remaining buffered bytes to the destination stream.</summary>
        /// <param name="destination">The stream to copy the buffered bytes to.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the copy operation.</returns>
        public Task CopyToInternalAsync(Stream destination, CancellationToken cancellationToken)
        {
            if (_pooledBuffer is null)
            {
                throw CreateObjectDisposedException();
            }

            var bytesAvailable = _length - _position;

            var source = _pooledBuffer.AsMemory(_position, bytesAvailable);

            _position += source.Length;

            return destination.WriteAsync(source, cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Roslynator",
            "RCS1229:Use async/await when necessary",
            Justification = "Read is synchronous; results/exceptions surface via a completed ValueTask, avoiding an async state-machine allocation on this hot path.")]
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new(Task.FromCanceled<int>(cancellationToken));
            }

            try
            {
                var result = Read(buffer.Span);

                return new(result);
            }
            catch (Exception e)
            {
                return new(Task.FromException<int>(e));
            }
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            if (_pooledBuffer is null)
            {
                throw CreateObjectDisposedException();
            }

            if (_position >= _length)
            {
                return 0;
            }

            var bytesAvailable = _length - _position;

            var source = _pooledBuffer.AsSpan(_position, bytesAvailable);

            var bytesCopied = Math.Min(source.Length, buffer.Length);

            var destination = buffer[..bytesCopied];

            source[..bytesCopied].CopyTo(destination);

            _position += bytesCopied;

            return bytesCopied;
        }
    }
}
#endif
