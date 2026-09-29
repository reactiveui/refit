// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;

namespace Refit.Documentation;

/// <summary>
/// An in-memory connection for <see cref="SocketsHttpHandler.ConnectCallback"/>: it discards the request and answers
/// with one fixed HTTP/1.1 reply. HttpClient's own tracing and metrics run over it without a network.
/// </summary>
internal sealed class CannedResponseStream : Stream
{
    /// <summary>The reply bytes the client reads.</summary>
    private readonly MemoryStream _reply;

    /// <summary>Initializes a new instance of the <see cref="CannedResponseStream"/> class.</summary>
    /// <param name="json">The JSON body of the <c>200 OK</c> reply.</param>
    internal CannedResponseStream(string json)
    {
        byte[] body = Encoding.UTF8.GetBytes(json);
        string head = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        _reply = new MemoryStream([.. Encoding.ASCII.GetBytes(head), .. body]);
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => _reply.Read(buffer, offset, count);

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        // The request is discarded: every request gets the same reply.
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        // Nothing is buffered.
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reply.Dispose();
        }

        base.Dispose(disposing);
    }
}
