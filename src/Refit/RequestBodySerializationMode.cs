// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Controls how Refit serializes JSON request bodies.</summary>
public enum RequestBodySerializationMode
{
    /// <summary>
    /// The default: the body is serialized asynchronously (via <c>JsonContent</c>). This uses the metadata-based
    /// System.Text.Json logic, never the source-generated fast-path.
    /// </summary>
    Default,

    /// <summary>
    /// The body is serialized synchronously into a buffer and sent as a <c>ByteArrayContent</c>. This lets the
    /// source-generated fast-path engage (with fast-path-eligible options) and sets a <c>Content-Length</c> header.
    /// Best for small-to-medium bodies. Requires an <see cref="ISynchronousContentSerializer"/>.
    /// </summary>
    Buffered,

    /// <summary>
    /// The body is serialized through a <c>Utf8JsonWriter</c> written to the request stream and flushed
    /// asynchronously. This keeps the source-generated fast-path while bounding peak memory (the writer buffers in
    /// pooled chunks rather than materializing the whole body), at the cost of a <c>Content-Length</c> header. Best
    /// for large uploads. Requires an <see cref="ISynchronousContentSerializer"/>.
    /// </summary>
    Streamed
}
