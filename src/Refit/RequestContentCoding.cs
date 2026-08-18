// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Net.Http;

namespace Refit;

/// <summary>Resolves a request body coding and wraps content in it.</summary>
internal static class RequestContentCoding
{
    /// <summary>Gets the <c>Content-Encoding</c> token a coding sends.</summary>
    /// <param name="compression">The coding.</param>
    /// <returns>The token.</returns>
    /// <exception cref="PlatformNotSupportedException">The coding has no token because it encodes nothing.</exception>
    internal static string Token(RequestCompression compression) =>
        compression switch
        {
            RequestCompression.GZip => "gzip",
            RequestCompression.Brotli => "br",
            RequestCompression.Zstandard => "zstd",
            _ => throw Unsupported(compression),
        };

    /// <summary>Builds the error for a coding this framework cannot produce.</summary>
    /// <param name="compression">The requested coding.</param>
    /// <returns>The exception to throw.</returns>
    internal static PlatformNotSupportedException Unsupported(RequestCompression compression) =>
        new($"Request compression '{compression}' is not available on this target framework. gzip is always available, Brotli requires .NET 8.0 or later, and Zstandard requires .NET 11.0 or later.");

    /// <summary>Resolves the coding and level for a body, letting the parameter override the settings.</summary>
    /// <param name="settings">The Refit settings supplying the instance-wide default.</param>
    /// <param name="compression">The coding the body parameter declared.</param>
    /// <param name="level">The level the body parameter declared, used only when it also declared a coding.</param>
    /// <returns>The coding to apply and how hard to compress.</returns>
    internal static (RequestCompression Compression, CompressionLevel Level) Resolve(
        RefitSettings settings,
        RequestCompression compression,
        CompressionLevel level) =>
        compression == RequestCompression.Default
            ? (settings.RequestCompression, settings.RequestCompressionLevel)
            : (compression, level);

    /// <summary>Wraps content in the compressor for a coding.</summary>
    /// <param name="content">The content to compress.</param>
    /// <param name="compression">The resolved coding, never <see cref="RequestCompression.Default"/> or <see cref="RequestCompression.None"/>.</param>
    /// <param name="level">How hard to compress.</param>
    /// <returns>The compressing content.</returns>
    /// <exception cref="PlatformNotSupportedException">This framework cannot produce the coding.</exception>
    internal static HttpContent Wrap(HttpContent content, RequestCompression compression, CompressionLevel level) =>
#if NET11_0_OR_GREATER
        compression switch
        {
            RequestCompression.GZip => new GZipCompressedContent(content, level),
            RequestCompression.Brotli => new BrotliCompressedContent(content, level),
            RequestCompression.Zstandard => new ZstandardCompressedContent(content, level),
            _ => throw Unsupported(compression),
        };
#elif NET8_0_OR_GREATER
        compression is RequestCompression.GZip or RequestCompression.Brotli
            ? new CompressedContent(content, compression, level)
            : throw Unsupported(compression);
#else
        compression is RequestCompression.GZip
            ? new CompressedContent(content, compression, level)
            : throw Unsupported(compression);
#endif
}
