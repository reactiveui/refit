// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Stream that delegates to an inner stream. This is taken from System.Net.Http.</summary>
/// <remarks>
/// https://github.com/ASP-NET-MVC/aspnetwebstack/blob/d5188c8a75b5b26b09ab89bedfd7ee635ae2ff17/src/System.Net.Http.Formatting/Internal/DelegatingStream.cs.
/// </remarks>
[ExcludeFromCodeCoverage]
[SuppressMessage(
    "Design",
    "SST1496:An abstract type declares nothing abstract",
    Justification = "Verbatim base class ported from System.Net.Http; kept abstract so only concrete stream wrappers are instantiated.")]
internal abstract class DelegatingStream : Stream
{
    /// <summary>Indicates whether the inner stream is disposed when this stream is disposed.</summary>
    private readonly bool _ownsInnerStream;

    /// <summary>Initializes a new instance of the <see cref="DelegatingStream"/> class.</summary>
    /// <param name="innerStream">The inner stream that operations are delegated to.</param>
    /// <param name="ownsInnerStream">
    /// <c>true</c> if the inner stream should be disposed when this stream is disposed; otherwise <c>false</c>.
    /// </param>
    protected DelegatingStream(Stream innerStream, bool ownsInnerStream = true)
    {
        InnerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _ownsInnerStream = ownsInnerStream;
    }

    /// <inheritdoc/>
    public override bool CanRead => InnerStream.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => InnerStream.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => InnerStream.CanWrite;

    /// <inheritdoc/>
    public override bool CanTimeout => InnerStream.CanTimeout;

    /// <inheritdoc/>
    public override long Length => InnerStream.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => InnerStream.Position;
        set => InnerStream.Position = value;
    }

    /// <inheritdoc/>
    public override int ReadTimeout
    {
        get => InnerStream.ReadTimeout;
        set => InnerStream.ReadTimeout = value;
    }

    /// <inheritdoc/>
    public override int WriteTimeout
    {
        get => InnerStream.WriteTimeout;
        set => InnerStream.WriteTimeout = value;
    }

    /// <summary>Gets the inner stream that operations are delegated to.</summary>
    protected Stream InnerStream { get; }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) =>
        InnerStream.Read(buffer, offset, count);

    /// <inheritdoc/>
    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        InnerStream.ReadAsync(buffer, offset, count, cancellationToken);

#if NET6_0_OR_GREATER
    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        InnerStream.ReadAsync(buffer, cancellationToken);
#endif

    /// <inheritdoc/>
    public override int ReadByte() => InnerStream.ReadByte();

    /// <inheritdoc/>
    public override void Flush() => InnerStream.Flush();

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        InnerStream.FlushAsync(cancellationToken);

    /// <inheritdoc/>
    public override Task CopyToAsync(
        Stream destination,
        int bufferSize,
        CancellationToken cancellationToken) =>
        InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);

    /// <inheritdoc/>
    public override void SetLength(long value) => InnerStream.SetLength(value);

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) =>
        InnerStream.Write(buffer, offset, count);

    /// <inheritdoc/>
    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        InnerStream.WriteAsync(buffer, offset, count, cancellationToken);

#if NET6_0_OR_GREATER
    /// <inheritdoc/>
    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        InnerStream.WriteAsync(buffer, cancellationToken);
#endif

    /// <inheritdoc/>
    public override void WriteByte(byte value) => InnerStream.WriteByte(value);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsInnerStream)
        {
            InnerStream.Dispose();
        }

        base.Dispose(disposing);
    }
}
