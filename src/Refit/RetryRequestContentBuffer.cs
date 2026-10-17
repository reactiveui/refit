// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
namespace Refit;

/// <summary>Bounds streamed serialization before a replayable request is sent.</summary>
/// <param name="limit">The maximum number of bytes to buffer.</param>
/// <param name="operationToken">The operation cancellation token.</param>
internal sealed class RetryRequestContentBuffer(int limit, CancellationToken operationToken) : MemoryStream
{
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        CheckLength(Position + count);
        base.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CheckLength(Position + buffer.Length);
        base.Write(buffer);
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        CheckLength(Position + 1);
        base.WriteByte(value);
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CheckLength(Position + count);
        return base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CheckLength(Position + buffer.Length);
        return base.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        CheckLength(value);
        base.SetLength(value);
    }

    /// <summary>Rejects buffering beyond the configured limit or after cancellation.</summary>
    /// <param name="length">The requested buffer length.</param>
    /// <exception cref="HttpRequestException">The body exceeds the configured buffer size.</exception>
    private void CheckLength(long length)
    {
        operationToken.ThrowIfCancellationRequested();
        if (length > limit)
        {
            throw new HttpRequestException("The request body exceeds MaxRequestContentBufferSize.");
        }
    }
}
#endif
