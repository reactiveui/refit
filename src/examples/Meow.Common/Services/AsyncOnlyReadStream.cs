// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Meow;

/// <summary>Read-only stream that rejects synchronous reads to mimic async-only transports.</summary>
/// <param name="data">The data to expose through the stream.</param>
public sealed class AsyncOnlyReadStream(byte[] data) : Stream
{
    /// <summary>The underlying memory stream over the data.</summary>
    private readonly MemoryStream _inner = new(data, false);

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => _inner.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => _inner.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    /// <inheritdoc/>
    public override void Flush() => _inner.Flush();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Synchronous reads are not supported in this stream.");

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc/>
    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        _inner.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
