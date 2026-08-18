// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The content coding applied to a request body before it is sent.</summary>
/// <remarks>
/// Not every coding exists on every target framework: <see cref="GZip"/> is always available, <see cref="Brotli"/>
/// needs .NET 8.0 or later, and <see cref="Zstandard"/> needs .NET 11.0 or later. Asking for a coding the running
/// framework cannot produce throws <see cref="PlatformNotSupportedException"/> when the request is built, rather than
/// silently sending the body uncompressed.
/// </remarks>
public enum RequestCompression
{
    /// <summary>Take the coding from <see cref="RefitSettings.RequestCompression"/>.</summary>
    Default = 0,

    /// <summary>Send the body uncompressed, whatever <see cref="RefitSettings.RequestCompression"/> says.</summary>
    None = 1,

    /// <summary>Compress with gzip and send <c>Content-Encoding: gzip</c>.</summary>
    GZip = 2,

    /// <summary>Compress with Brotli and send <c>Content-Encoding: br</c>. Requires .NET 8.0 or later.</summary>
    Brotli = 3,

    /// <summary>Compress with Zstandard and send <c>Content-Encoding: zstd</c>. Requires .NET 11.0 or later.</summary>
    Zstandard = 4,
}
