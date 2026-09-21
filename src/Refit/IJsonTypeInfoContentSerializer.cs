// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Refit;

/// <summary>
/// An optional capability for an <see cref="IHttpContentSerializer"/> that reads and writes JSON with metadata the caller
/// supplies for each call, such as a property of a source-generated context, instead of looking the metadata up.
/// </summary>
public interface IJsonTypeInfoContentSerializer
{
    /// <summary>Serializes a value to <see cref="HttpContent"/> with explicit type metadata.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="item">The value to serialize.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>The content that holds the serialized value.</returns>
    HttpContent ToHttpContent<T>(T item, JsonTypeInfo<T> typeInfo);

    /// <summary>Serializes a value into a buffered body with explicit type metadata.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="item">The value to serialize.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>The content that holds the serialized value.</returns>
    HttpContent ToHttpContentSynchronous<T>(T item, JsonTypeInfo<T> typeInfo);

    /// <summary>Creates a body that serializes a value with explicit type metadata when the request sends it.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="item">The value to serialize.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>The content that writes the serialized value to the request stream.</returns>
    HttpContent ToStreamingHttpContent<T>(T item, JsonTypeInfo<T> typeInfo);

    /// <summary>Reads a value from <see cref="HttpContent"/> with explicit type metadata.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="content">The body to read.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>The value, or <see langword="null"/> for JSON null.</returns>
    [SuppressMessage(
        "Design",
        "SST2309:An externally visible member declares an optional parameter, so callers bake in the default",
        Justification = "Optional CancellationToken is part of the published interface contract, as on the sibling serializer interfaces.")]
    Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken = default);

    /// <summary>Reads buffered JSON text with explicit type metadata.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="content">The JSON text.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>The value, or <see langword="null"/> for JSON null.</returns>
    T? DeserializeFromString<T>(string content, JsonTypeInfo<T> typeInfo);

    /// <summary>Reads one value at a time from a framed response stream with explicit type metadata.</summary>
    /// <typeparam name="T">The element type to read.</typeparam>
    /// <param name="stream">The response body.</param>
    /// <param name="format">The body framing: a JSON array, JSON Lines, or JSON data payloads in server-sent events.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <param name="cancellationToken">A token that cancels enumeration.</param>
    /// <returns>The values as they arrive.</returns>
    [SuppressMessage(
        "Design",
        "SST2309:An externally visible member declares an optional parameter, so callers bake in the default",
        Justification = "Optional CancellationToken is part of the published interface contract, as on the sibling serializer interfaces.")]
    IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
        Stream stream,
        StreamingContentFormat format,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken = default);
}
#endif
