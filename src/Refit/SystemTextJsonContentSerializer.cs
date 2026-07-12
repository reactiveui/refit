// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET9_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
#if NET8_0_OR_GREATER
using System.Globalization;
using System.Text.Json.Serialization.Metadata;
#endif

namespace Refit;

/// <summary>A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the System.Text.Json APIs.</summary>
/// <remarks>
/// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance with the specified parameters.
/// </remarks>
/// <param name="jsonSerializerOptions">The serialization options to use for the current instance.</param>
public sealed class SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
    : IHttpContentSerializer, IStreamingContentSerializer, ISynchronousContentSerializer, ISynchronousContentDeserializer
{
    /// <summary>Justification shared by the reflection-fallback trim/AOT suppressions.</summary>
    private const string ReflectionFallbackJustification =
        "Serializing or deserializing without supplied JSON type metadata requires runtime serializer metadata.";

    /// <summary>Initializes a new instance of the <see cref="SystemTextJsonContentSerializer"/> class.</summary>
    public SystemTextJsonContentSerializer()
        : this(GetDefaultJsonSerializerOptions())
    {
    }

    /// <summary>Gets the JSON serialization options this serializer uses, exposed so a query converter can walk a
    /// registered type's <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo"/> without reflection.</summary>
    public JsonSerializerOptions SerializerOptions => jsonSerializerOptions;

    /// <summary>Creates new <see cref="JsonSerializerOptions"/> and fills it with default parameters.</summary>
    /// <returns>The default <see cref="JsonSerializerOptions"/>.</returns>
    public static JsonSerializerOptions GetDefaultJsonSerializerOptions()
    {
        // Default to case insensitive property name matching as that's likely the behavior most users expect
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
#if NET10_0_OR_GREATER
            AllowDuplicateProperties = false
#endif
        };
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        jsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter());

        return jsonSerializerOptions;
    }

    /// <summary>
    /// Creates <see cref="JsonSerializerOptions"/> that are eligible for the System.Text.Json source-generated
    /// fast-path on serialization. Unlike <see cref="GetDefaultJsonSerializerOptions"/>, these options add no
    /// converters and do not set <see cref="JsonSerializerOptions.NumberHandling"/>, both of which disable the
    /// fast-path. Assign a source-generated <see cref="JsonSerializerOptions.TypeInfoResolver"/> (a
    /// <see cref="JsonSerializerContext"/> generated in <see cref="JsonSourceGenerationMode.Serialization"/> or
    /// <see cref="JsonSourceGenerationMode.Default"/> mode) to enable it. The fast-path runs through the synchronous
    /// serialization primitives (<c>SerializeToUtf8Bytes</c> / <c>Serialize(Utf8JsonWriter, ...)</c>); only the
    /// built-in <c>SerializeAsync(Stream)</c> (used by <c>JsonContent</c>) bypasses it for the metadata logic.
    /// </summary>
    /// <returns>Fast-path-eligible <see cref="JsonSerializerOptions"/>.</returns>
    [SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "Returns a new mutable options instance on each call; a property would wrongly imply a cached value.")]
    public static JsonSerializerOptions GetFastPathJsonSerializerOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET10_0_OR_GREATER
            AllowDuplicateProperties = false
#endif
        };

    /// <inheritdoc/>
    public HttpContent ToHttpContent<T>(T item)
    {
        var serializeByRuntimeType = item is not null
            && (typeof(T).IsInterface || typeof(T).IsAbstract)
            && !DeclaredTypeIsPolymorphic(typeof(T), jsonSerializerOptions);

#if NET8_0_OR_GREATER
        return serializeByRuntimeType switch
        {
            true => ToHttpContentRuntimeTyped(item!, item!.GetType()),
            false when jsonSerializerOptions.TypeInfoResolver is not null => JsonContent.Create(item, GetJsonTypeInfo<T>()),
            _ => ToHttpContentReflection(item),
        };
#else
        return serializeByRuntimeType
            ? ToHttpContentRuntimeTyped(item!, item!.GetType())
            : ToHttpContentReflection(item);
#endif
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public HttpContent ToHttpContentSynchronous<T>(T item)
    {
        var content = new ByteArrayContent(SerializeToUtf8Bytes(item));
        content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        return content;
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [SuppressMessage("Roslynator", "RCS1261:Resource can be disposed asynchronously", Justification = "Newer .NET versions only.")]
    public HttpContent ToStreamingHttpContent<T>(T item)
    {
        var content = new PushStreamContent(
            async (stream, _, _) =>
            {
                // Disposing the stream signals PushStreamContent that serialization is complete.
#if NET8_0_OR_GREATER
                await using (stream.ConfigureAwait(false))
#else
                using (stream)
#endif
                {
                    await using var writer = new Utf8JsonWriter(stream);
                    SerializeToWriter(item, writer);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            });
        content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        return content;
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default) =>
#if NET8_0_OR_GREATER
        jsonSerializerOptions.TypeInfoResolver is not null
            ? content.ReadFromJsonAsync(GetJsonTypeInfo<T>(), cancellationToken)
            : FromHttpContentReflectionAsync<T>(content, cancellationToken);
#else
        FromHttpContentReflectionAsync<T>(content, cancellationToken);
#endif

    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public T? DeserializeFromString<T>(string content)
    {
#if NET8_0_OR_GREATER
        return jsonSerializerOptions.TypeInfoResolver is not null
            ? JsonSerializer.Deserialize(content, GetJsonTypeInfo<T>())
            : DeserializeFromStringReflection<T>(content);
#else
        return DeserializeFromStringReflection<T>(content);
#endif
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
        Stream stream,
        StreamingContentFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentExceptionHelper.ThrowIfNull(stream);

        return format == StreamingContentFormat.JsonLines
            ? DeserializeJsonLinesAsync<T>(stream, cancellationToken)
            : DeserializeJsonArrayAsync<T>(stream, cancellationToken);
    }

    /// <summary>
    /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands.
    /// </summary>
    /// <param name="propertyInfo">A PropertyInfo object.</param>
    /// <returns>
    /// The calculated field name.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">propertyInfo.</exception>
    public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
    {
        ArgumentExceptionHelper.ThrowIfNull(propertyInfo);
        return propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name;
    }

    /// <summary>Determines whether the declared type is configured for polymorphic serialization.</summary>
    /// <param name="type">The declared type to inspect.</param>
    /// <param name="jsonSerializerOptions">The serializer options to consult for type metadata.</param>
    /// <returns><see langword="true"/> if the type is polymorphic; otherwise <see langword="false"/>.</returns>
    private static bool DeclaredTypeIsPolymorphic(Type type, JsonSerializerOptions jsonSerializerOptions)
    {
#if NET8_0_OR_GREATER
        return type.IsDefined(typeof(JsonPolymorphicAttribute), false)
            || type.IsDefined(typeof(JsonDerivedTypeAttribute), false)
            || (jsonSerializerOptions.TypeInfoResolver is not null
                && GetJsonTypeInfo(type, jsonSerializerOptions).PolymorphismOptions is not null);
#else
        _ = jsonSerializerOptions;
        return type.IsDefined(typeof(JsonPolymorphicAttribute), false)
            || type.IsDefined(typeof(JsonDerivedTypeAttribute), false);
#endif
    }

#if NET8_0_OR_GREATER
    /// <summary>Gets the JSON type metadata for the given runtime type from the supplied options.</summary>
    /// <param name="type">The runtime type to resolve metadata for.</param>
    /// <param name="jsonSerializerOptions">The serializer options to consult.</param>
    /// <returns>The JSON type metadata.</returns>
    private static JsonTypeInfo GetJsonTypeInfo(Type type, JsonSerializerOptions jsonSerializerOptions) =>
        jsonSerializerOptions.GetTypeInfo(type)
        ?? throw new InvalidOperationException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The serializer options did not provide metadata for {0}.",
                type));

#if NET11_0_OR_GREATER
    /// <summary>Gets the JSON type metadata for the given type from the supplied options.</summary>
    /// <typeparam name="T">The type to resolve metadata for.</typeparam>
    /// <param name="jsonSerializerOptions">The serializer options to consult.</param>
    /// <returns>The JSON type metadata.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private static JsonTypeInfo<T> GetJsonTypeInfo<T>(JsonSerializerOptions jsonSerializerOptions) =>
        jsonSerializerOptions.GetTypeInfo<T>();
#endif
#endif

#if NET8_0_OR_GREATER
    /// <summary>Gets the JSON type metadata for the given type from the configured options.</summary>
    /// <typeparam name="T">The type to resolve metadata for.</typeparam>
    /// <returns>The JSON type metadata.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private JsonTypeInfo<T> GetJsonTypeInfo<T>() =>
#if NET11_0_OR_GREATER
        GetJsonTypeInfo<T>(jsonSerializerOptions)
#else
        (GetJsonTypeInfo(typeof(T), jsonSerializerOptions) as JsonTypeInfo<T>)
#endif
        ?? throw new InvalidOperationException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The serializer options did not provide metadata for {0}.",
                typeof(T)));
#endif

    /// <summary>Serializes the item to HTTP content using the supplied runtime type.</summary>
    /// <param name="item">The item to serialize.</param>
    /// <param name="runtimeType">The runtime type to serialize the item as.</param>
    /// <returns>The serialized HTTP content.</returns>
    private JsonContent ToHttpContentRuntimeTyped(object item, Type runtimeType)
    {
#if NET8_0_OR_GREATER
        return jsonSerializerOptions.TypeInfoResolver is not null
            ? JsonContent.Create(item, GetJsonTypeInfo(runtimeType, jsonSerializerOptions))
            : ToHttpContentRuntimeReflection(item, runtimeType);
#else
        return ToHttpContentRuntimeReflection(item, runtimeType);
#endif
    }

    /// <summary>Serializes the item by runtime type using reflection-based metadata (used when the options provide no resolver).</summary>
    /// <param name="item">The item to serialize.</param>
    /// <param name="runtimeType">The runtime type to serialize the item as.</param>
    /// <returns>The serialized HTTP content.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    private JsonContent ToHttpContentRuntimeReflection(object item, Type runtimeType) =>
        JsonContent.Create(item, runtimeType, options: jsonSerializerOptions);

    /// <summary>Serializes the item using reflection-based metadata (used when the options provide no resolver).</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The serialized HTTP content.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    private JsonContent ToHttpContentReflection<T>(T item) =>
        JsonContent.Create(item, options: jsonSerializerOptions);

    /// <summary>Deserializes the content using reflection-based metadata (used when the options provide no resolver).</summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="content">The HTTP content to read from.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The deserialized value.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    private Task<T?> FromHttpContentReflectionAsync<T>(HttpContent content, CancellationToken cancellationToken) =>
        content.ReadFromJsonAsync<T>(jsonSerializerOptions, cancellationToken);

    /// <summary>Serializes an item to UTF-8 JSON bytes, using source-gen metadata when a resolver is configured.</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The UTF-8 encoded JSON.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private byte[] SerializeToUtf8Bytes<T>(T item)
    {
#if NET8_0_OR_GREATER
        return jsonSerializerOptions.TypeInfoResolver is not null
            ? JsonSerializer.SerializeToUtf8Bytes(item, GetJsonTypeInfo<T>())
            : SerializeToUtf8BytesReflection(item);
#else
        return SerializeToUtf8BytesReflection(item);
#endif
    }

    /// <summary>Serializes an item to UTF-8 JSON bytes using reflection-based metadata.</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The UTF-8 encoded JSON.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private byte[] SerializeToUtf8BytesReflection<T>(T item) =>
        JsonSerializer.SerializeToUtf8Bytes(item, jsonSerializerOptions);

    /// <summary>Writes an item to a <see cref="Utf8JsonWriter"/>, using source-gen metadata when a resolver is configured.</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="writer">The writer to serialize into.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private void SerializeToWriter<T>(T item, Utf8JsonWriter writer)
    {
#if NET8_0_OR_GREATER
        if (jsonSerializerOptions.TypeInfoResolver is not null)
        {
            JsonSerializer.Serialize(writer, item, GetJsonTypeInfo<T>());
        }
        else
        {
            SerializeToWriterReflection(item, writer);
        }
#else
        SerializeToWriterReflection(item, writer);
#endif
    }

    /// <summary>Writes an item to a <see cref="Utf8JsonWriter"/> using reflection-based metadata.</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="writer">The writer to serialize into.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private void SerializeToWriterReflection<T>(T item, Utf8JsonWriter writer) =>
        JsonSerializer.Serialize(writer, item, jsonSerializerOptions);

    /// <summary>Streams the elements of a single top-level JSON array.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private IAsyncEnumerable<T?> DeserializeJsonArrayAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
#if NET8_0_OR_GREATER
        return jsonSerializerOptions.TypeInfoResolver is not null
            ? JsonSerializer.DeserializeAsyncEnumerable<T>(stream, GetJsonTypeInfo<T>(), cancellationToken)
            : DeserializeJsonArrayReflectionAsync<T>(stream, cancellationToken);
#else
        return DeserializeJsonArrayReflectionAsync<T>(stream, cancellationToken);
#endif
    }

    /// <summary>Streams JSON array elements using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private IAsyncEnumerable<T?> DeserializeJsonArrayReflectionAsync<T>(Stream stream, CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsyncEnumerable<T>(stream, jsonSerializerOptions, cancellationToken);

    /// <summary>Streams newline-delimited JSON values from the response body.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private IAsyncEnumerable<T?> DeserializeJsonLinesAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
#if NET9_0_OR_GREATER
        // .NET 9 added the topLevelValues overload, which reads a stream of whitespace-separated
        // top-level JSON values - exactly the JSON Lines framing - without per-line buffering.
        return jsonSerializerOptions.TypeInfoResolver is not null
            ? JsonSerializer.DeserializeAsyncEnumerable<T>(stream, GetJsonTypeInfo<T>(), topLevelValues: true, cancellationToken)
            : DeserializeJsonLinesTopLevelReflectionAsync<T>(stream, cancellationToken);
#else
        return DeserializeJsonLinesManualAsync<T>(stream, cancellationToken);
#endif
    }

#if NET9_0_OR_GREATER
    /// <summary>Streams top-level JSON values using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private IAsyncEnumerable<T?> DeserializeJsonLinesTopLevelReflectionAsync<T>(Stream stream, CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsyncEnumerable<T>(stream, topLevelValues: true, jsonSerializerOptions, cancellationToken);
#else
    /// <summary>Reads newline-delimited JSON from a stream using a pooled UTF-8 buffer, deserializing each line.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private async IAsyncEnumerable<T?> DeserializeJsonLinesManualAsync<T>(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        static bool IsBlankLine(byte[] lineBuffer, int lineStart, int lineLength)
        {
            var line = new ReadOnlySpan<byte>(lineBuffer, lineStart, lineLength);
            if (!line.IsEmpty && line[line.Length - 1] == (byte)'\r')
            {
                line = line.Slice(0, line.Length - 1);
            }

            foreach (var b in line)
            {
                if (b != (byte)' ' && b != (byte)'\t')
                {
                    return false;
                }
            }

            return true;
        }

        // The initial pooled buffer, and the factor it grows by when a single line does not fit in it.
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
                    if (!IsBlankLine(buffer, start, length))
                    {
                        yield return DeserializeLine<T>(buffer, start, length);
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
                    var larger = ArrayPool<byte>.Shared.Rent(buffer.Length * lineScanBufferGrowthFactor);
                    Buffer.BlockCopy(buffer, 0, larger, 0, end);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = larger;
                }

#if NET8_0_OR_GREATER
                var read = await stream
                    .ReadAsync(buffer.AsMemory(end, buffer.Length - end), cancellationToken)
                    .ConfigureAwait(false);
#else
                var read = await stream
                    .ReadAsync(buffer, end, buffer.Length - end, cancellationToken)
                    .ConfigureAwait(false);
#endif
                if (read == 0)
                {
                    if (!IsBlankLine(buffer, start, end - start))
                    {
                        yield return DeserializeLine<T>(buffer, start, end - start);
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
    /// <returns>The deserialized value.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private T? DeserializeLine<T>(byte[] buffer, int start, int length)
    {
        // DeserializeLine is only called for non-blank lines, so the span is never empty here.
        var span = new ReadOnlySpan<byte>(buffer, start, length);
        if (span[span.Length - 1] == (byte)'\r')
        {
            span = span.Slice(0, span.Length - 1);
        }

#if NET8_0_OR_GREATER
        return jsonSerializerOptions.TypeInfoResolver is not null
            ? JsonSerializer.Deserialize(span, GetJsonTypeInfo<T>())
            : DeserializeLineReflection<T>(span);
#else
        return DeserializeLineReflection<T>(span);
#endif
    }

    /// <summary>Deserializes a JSON line using reflection-based metadata.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON value.</param>
    /// <returns>The deserialized value.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private T? DeserializeLineReflection<T>(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<T>(utf8Json, jsonSerializerOptions);
#endif

    /// <summary>Deserializes a buffered string using reflection-based metadata.</summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="content">The buffered content string.</param>
    /// <returns>The deserialized value.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private T? DeserializeFromStringReflection<T>(string content) =>
        JsonSerializer.Deserialize<T>(content, jsonSerializerOptions);
}
