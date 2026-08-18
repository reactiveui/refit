// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO.Compression;

namespace Refit.Reflection.Tests;

/// <summary>An API declaring a request body coding on the body parameter, one method per coding decision.</summary>
public interface ICompressedBodyApi
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
