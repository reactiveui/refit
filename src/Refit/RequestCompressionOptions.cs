// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET9_0_OR_GREATER
using System.IO.Compression;

namespace Refit;

/// <summary>The per-coding compressor settings used in place of <see cref="RefitSettings.RequestCompressionLevel"/>.</summary>
/// <remarks>
/// Each coding has its own options type carrying knobs a level cannot express - window size, strategy, a Zstandard
/// dictionary. Setting the options for a coding overrides the level for that coding only; the codings left null still
/// compress by level. The options types themselves arrived with .NET 9.0, and Zstandard's with .NET 11.0, so this type
/// does not exist on earlier targets.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{GZip} {Brotli}")]
public sealed class RequestCompressionOptions
{
    /// <summary>Gets or sets the gzip compressor settings, or <see langword="null"/> to compress by level.</summary>
    public ZLibCompressionOptions? GZip { get; set; }

    /// <summary>Gets or sets the Brotli compressor settings, or <see langword="null"/> to compress by level.</summary>
    public BrotliCompressionOptions? Brotli { get; set; }

#if NET11_0_OR_GREATER
    /// <summary>Gets or sets the Zstandard compressor settings, or <see langword="null"/> to compress by level.</summary>
    public ZstandardCompressionOptions? Zstandard { get; set; }
#endif
}
#endif
