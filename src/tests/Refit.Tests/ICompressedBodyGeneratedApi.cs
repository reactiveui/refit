// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;

namespace Refit.Tests;

/// <summary>An API whose generated client declares a request body coding, one method per coding decision.</summary>
public interface ICompressedBodyGeneratedApi
{
    /// <summary>Sends a gzip-coded body.</summary>
    /// <param name="body">The body value.</param>
    /// <returns>The response body.</returns>
    [Post("/gzip")]
    Task<string> GZip([Body(Compression = RequestCompression.GZip, CompressionLevel = CompressionLevel.SmallestSize)] string body);

    /// <summary>Sends a body with no coding declared, so the settings decide.</summary>
    /// <param name="body">The body value.</param>
    /// <returns>The response body.</returns>
    [Post("/inherited")]
    Task<string> Inherited([Body] string body);

    /// <summary>Sends an uncoded body whatever the settings say.</summary>
    /// <param name="body">The body value.</param>
    /// <returns>The response body.</returns>
    [Post("/uncompressed")]
    Task<string> Uncompressed([Body(Compression = RequestCompression.None)] string body);
}
