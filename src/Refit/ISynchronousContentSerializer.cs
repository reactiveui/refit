// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>
/// An optional capability for an <see cref="IHttpContentSerializer"/> that can serialize a request body
/// synchronously (buffered or streamed). Refit uses this when <see cref="RefitSettings.RequestBodySerialization"/>
/// is <see cref="RequestBodySerializationMode.Buffered"/> or <see cref="RequestBodySerializationMode.Streamed"/>,
/// allowing the System.Text.Json source-generated fast-path to engage. The fast-path runs through the synchronous serialization primitives
/// (<c>SerializeToUtf8Bytes</c> / <c>Serialize(Utf8JsonWriter, ...)</c>); the built-in <c>SerializeAsync(Stream)</c>
/// used by <c>JsonContent</c> bypasses it and uses the metadata logic instead.
/// </summary>
public interface ISynchronousContentSerializer
{
    /// <summary>Serializes <paramref name="item"/> synchronously into a buffered <see cref="HttpContent"/>.</summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="item">The object to serialize.</param>
    /// <returns>A buffered <see cref="HttpContent"/> (for example a <c>ByteArrayContent</c>) containing the serialized object.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    HttpContent ToHttpContentSynchronous<T>(T item);

    /// <summary>
    /// Creates an <see cref="HttpContent"/> that serializes <paramref name="item"/> through the synchronous fast-path
    /// onto the request stream, flushing asynchronously, without buffering the whole body. Best for large uploads.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="item">The object to serialize.</param>
    /// <returns>A streaming <see cref="HttpContent"/> that writes the serialized object when sent.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    HttpContent ToStreamingHttpContent<T>(T item);
}
