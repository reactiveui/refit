// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface returning <see cref="Stream"/> responses.</summary>
public interface IStreamApi
{
    /// <summary>Gets a remote file as a stream.</summary>
    /// <param name="filename">The name of the file to retrieve.</param>
    /// <returns>The file content stream.</returns>
    [Post("/{filename}")]
    Task<Stream> GetRemoteFile(string filename);

    /// <summary>Gets a remote file as a stream with response metadata.</summary>
    /// <param name="filename">The name of the file to retrieve.</param>
    /// <returns>The API response wrapping the file content stream.</returns>
    [Post("/{filename}")]
    Task<ApiResponse<Stream>> GetRemoteFileWithMetadata(string filename);
}
