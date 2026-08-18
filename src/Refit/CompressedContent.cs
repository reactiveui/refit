// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Net;
using System.Net.Http;

namespace Refit;

/// <summary>Compresses another content's bytes as they are written to the request stream.</summary>
/// <remarks>
/// Stands in for the framework's <c>GZipCompressedContent</c> and <c>BrotliCompressedContent</c> on targets that
/// predate them. .NET 11.0 and later use those types directly instead; this one is never constructed there.
/// </remarks>
internal sealed class CompressedContent : HttpContent
{
    /// <summary>The content whose bytes are compressed.</summary>
    private readonly HttpContent _inner;

    /// <summary>The coding to apply.</summary>
    private readonly RequestCompression _compression;

    /// <summary>How hard to compress.</summary>
    private readonly CompressionLevel _level;

    /// <summary>Initializes a new instance of the <see cref="CompressedContent"/> class.</summary>
    /// <param name="inner">The content whose bytes are compressed.</param>
    /// <param name="compression">The coding to apply.</param>
    /// <param name="level">How hard to compress.</param>
    internal CompressedContent(HttpContent inner, RequestCompression compression, CompressionLevel level)
    {
        _inner = inner;
        _compression = compression;
        _level = level;

        // The entity headers describe the entity, not the coding, so they carry over unchanged; Content-Length does
        // not, because the compressed length is unknown until the body has been written.
        foreach (var header in inner.Headers)
        {
            if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                _ = Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        Headers.ContentEncoding.Add(RequestContentCoding.Token(compression));
    }

    /// <inheritdoc/>
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        // leaveOpen, because the request stream belongs to the transport; only the compressor is finished here, and
        // finishing it is what writes the trailer that makes the body readable.
        Stream compressor = _compression switch
        {
            RequestCompression.GZip => new GZipStream(stream, _level, leaveOpen: true),
#if NET8_0_OR_GREATER
            RequestCompression.Brotli => new BrotliStream(stream, _level, leaveOpen: true),
#endif
            _ => throw RequestContentCoding.Unsupported(_compression),
        };

#if NET8_0_OR_GREATER
        await using (compressor.ConfigureAwait(false))
#else
        using (compressor)
#endif
        {
            await _inner.CopyToAsync(compressor).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        // Unknown until the body is written, so the request is sent chunked.
        length = -1;
        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
