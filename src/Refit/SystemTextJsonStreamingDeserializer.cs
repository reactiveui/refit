// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET9_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Refit;

/// <summary>Provides stateless streaming JSON deserialization helpers.</summary>
internal static class SystemTextJsonStreamingDeserializer
{
    /// <summary>Justification shared by reflection-fallback trim and AOT suppressions.</summary>
    private const string ReflectionFallbackJustification =
        "Deserializing without supplied JSON type metadata requires runtime serializer metadata.";

    /// <summary>Streams JSON array elements using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static IAsyncEnumerable<T?> DeserializeJsonArrayReflectionAsync<T>(
        Stream stream,
        JsonSerializerOptions options,
        CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken);

#if NET9_0_OR_GREATER
    /// <summary>Streams top-level JSON values using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static IAsyncEnumerable<T?> DeserializeJsonLinesTopLevelReflectionAsync<T>(
        Stream stream,
        JsonSerializerOptions options,
        CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsyncEnumerable<T>(stream, topLevelValues: true, options, cancellationToken);
#else
    /// <summary>Reads newline-delimited JSON from a stream using a pooled UTF-8 buffer.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [ExcludeFromCodeCoverage]
    internal static async IAsyncEnumerable<T?> DeserializeJsonLinesManualAsync<T>(
        Stream stream,
        JsonSerializerOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int lineScanBufferSize = 4096;
        const int lineScanBufferGrowthFactor = 2;

        var buffer = ArrayPool<byte>.Shared.Rent(lineScanBufferSize);
        var start = 0;
        var end = 0;
        try
        {
            while (true)
            {
                var newline = Array.IndexOf(buffer, (byte)'\n', start, end - start);
                if (newline >= 0)
                {
                    var length = newline - start;
                    if (!SystemTextJsonContentSerializer.IsBlankLine(buffer, start, length))
                    {
                        yield return DeserializeLine<T>(buffer, start, length, options);
                    }

                    start = newline + 1;
                    continue;
                }

                if (start > 0)
                {
                    Buffer.BlockCopy(buffer, start, buffer, 0, end - start);
                    end -= start;
                    start = 0;
                }

                if (end == buffer.Length)
                {
                    buffer = SystemTextJsonContentSerializer.GrowLineScanBuffer(buffer, end, lineScanBufferGrowthFactor);
                }

#if NET8_0_OR_GREATER
                var read = await stream
                    .ReadAsync(buffer.AsMemory(end), cancellationToken)
                    .ConfigureAwait(false);
#else
                var read = await stream
                    .ReadAsync(buffer, end, buffer.Length - end, cancellationToken)
                    .ConfigureAwait(false);
#endif
                if (read == 0)
                {
                    if (!SystemTextJsonContentSerializer.IsBlankLine(buffer, start, end - start))
                    {
                        yield return DeserializeLine<T>(buffer, start, end - start, options);
                    }

                    yield break;
                }

                end += read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>Deserializes a single buffered JSON line, trimming a trailing carriage return.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="buffer">The buffer holding the line bytes.</param>
    /// <param name="start">The offset of the line.</param>
    /// <param name="length">The length of the line.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The deserialized value.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static T? DeserializeLine<T>(byte[] buffer, int start, int length, JsonSerializerOptions options)
    {
        var span = new ReadOnlySpan<byte>(buffer, start, length);
        if (span[span.Length - 1] == (byte)'\r')
        {
            span = span.Slice(0, span.Length - 1);
        }

#if NET8_0_OR_GREATER
        return options.TypeInfoResolver is not null
            ? JsonSerializer.Deserialize(span, SystemTextJsonContentSerializer.GetJsonTypeInfo<T>(options))
            : DeserializeLineReflection<T>(span, options);
#else
        return DeserializeLineReflection<T>(span, options);
#endif
    }

    /// <summary>Deserializes a JSON line using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON value.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The deserialized value.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static T? DeserializeLineReflection<T>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(utf8Json, options);
#endif

    /// <summary>Deserializes a server-sent event's UTF-8 JSON data payload.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="eventType">The event type, which is not surfaced by this serializer.</param>
    /// <param name="data">The UTF-8 encoded JSON payload bytes.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The deserialized value.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static T? DeserializeSseData<T>(
        string eventType,
        ReadOnlySpan<byte> data,
        JsonSerializerOptions options)
    {
        _ = eventType;
#if NET8_0_OR_GREATER
        return options.TypeInfoResolver is not null
            ? JsonSerializer.Deserialize(data, SystemTextJsonContentSerializer.GetJsonTypeInfo<T>(options))
            : DeserializeSseDataReflection<T>(data, options);
#else
        return DeserializeSseDataReflection<T>(data, options);
#endif
    }

    /// <summary>Deserializes a server-sent event payload using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON payload bytes.</param>
    /// <param name="options">The serializer options to use.</param>
    /// <returns>The deserialized value.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static T? DeserializeSseDataReflection<T>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(utf8Json, options);
}
