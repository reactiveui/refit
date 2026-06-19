// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Refit.Buffers;

/// <summary>A <see langword="struct"/> that provides a fast implementation of a binary writer, leveraging <see cref="ArrayPool{T}"/> for memory pooling.</summary>
internal sealed partial class PooledBufferWriter : IBufferWriter<byte>, IDisposable
{
    /// <summary>The default size to use to create new <see cref="PooledBufferWriter"/> instances.</summary>
    public const int DefaultSize = 1024;

    /// <summary>The <see cref="byte"/> array current in use.</summary>
    private byte[] _buffer;

    /// <summary>The current position into <see cref="_buffer"/>.</summary>
    private int _position;

    /// <summary>Initializes a new instance of the <see cref="PooledBufferWriter"/> class.</summary>
    public PooledBufferWriter()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(DefaultSize);
        _position = 0;
    }

    /// <inheritdoc/>
    public void Advance(int count)
    {
        if (count < 0)
        {
            ThrowArgumentOutOfRangeExceptionForNegativeCount();
        }

        if (_position > _buffer.Length - count)
        {
            ThrowArgumentOutOfRangeExceptionForAdvancedTooFar();
        }

        _position += count;
    }

    /// <inheritdoc/>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureFreeCapacity(sizeHint);

        return _buffer.AsMemory(_position);
    }

    /// <inheritdoc/>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureFreeCapacity(sizeHint);

        return _buffer.AsSpan(_position);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_buffer.Length == 0)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(_buffer);
    }

    /// <summary>Gets a readable <see cref="Stream"/> for the current instance, by detaching the used buffer.</summary>
    /// <returns>A readable <see cref="Stream"/> with the contents of the current instance.</returns>
    public Stream DetachStream()
    {
        var stream = new PooledMemoryStream(this);

        _buffer = [];

        return stream;
    }

    /// <summary>Ensures the buffer in use has the free capacity to contain the specified amount of new data.</summary>
    /// <param name="count">The size in bytes of the new data to insert into the buffer.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureFreeCapacity(int count)
    {
        if (count < 0)
        {
            ThrowArgumentOutOfRangeExceptionForNegativeCount();
        }

        if (count == 0)
        {
            count = 1;
        }

        int currentLength = _buffer.Length;
        int freeCapacity = currentLength - _position;

        if (count <= freeCapacity)
        {
            return;
        }

        int growBy = Math.Max(count, currentLength);
        int newSize = checked(currentLength + growBy);

        var rent = ArrayPool<byte>.Shared.Rent(newSize);

        Array.Copy(_buffer, rent, _position);

        ArrayPool<byte>.Shared.Return(_buffer);

        _buffer = rent;
    }
}
