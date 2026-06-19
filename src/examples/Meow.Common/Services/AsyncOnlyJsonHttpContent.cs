// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;

namespace Meow;

/// <summary>HTTP content backed by a stream that only supports asynchronous reads.</summary>
/// <param name="json">The JSON content to serve.</param>
public sealed class AsyncOnlyJsonHttpContent(string json) : HttpContent
{
    /// <summary>The UTF-8 encoded content buffer.</summary>
    private readonly byte[] _buffer = Encoding.UTF8.GetBytes(json);

    /// <inheritdoc/>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        stream.WriteAsync(_buffer, 0, _buffer.Length);

    /// <inheritdoc/>
    protected override bool TryComputeLength(out long length)
    {
        length = _buffer.Length;
        return true;
    }

    /// <inheritdoc/>
    protected override Task<Stream> CreateContentReadStreamAsync() =>
        Task.FromResult<Stream>(new AsyncOnlyReadStream(_buffer));
}
