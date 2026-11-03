// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET9_0_OR_GREATER
using System.Buffers;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Refit;

/// <summary>A <see langword="class"/> implementing <see cref="IHttpContentSerializer"/> using the System.Text.Json APIs.</summary>
/// <param name="jsonSerializerOptions">The serialization options to use for the current instance.</param>
/// <remarks>
/// Creates a new <see cref="SystemTextJsonContentSerializer"/> instance with the specified parameters.
/// </remarks>
[System.Diagnostics.DebuggerDisplay("{SerializerOptions}")]
#if NET8_0_OR_GREATER
public sealed partial class SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
#else
public sealed class SystemTextJsonContentSerializer(JsonSerializerOptions jsonSerializerOptions)
#endif
    : IHttpContentSerializer, IStreamingContentSerializer, ISynchronousContentSerializer, ISynchronousContentDeserializer, IContentSerializerNameStyle
{
    /// <summary>Initializes a new instance of the <see cref="SystemTextJsonContentSerializer"/> class.</summary>
    public SystemTextJsonContentSerializer()
        : this(GetDefaultJsonSerializerOptions())
    {
    }

    /// <summary>Gets the JSON serialization options this serializer uses, exposed so a query converter can walk a
    /// registered type's <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo"/> without reflection.</summary>
    public JsonSerializerOptions SerializerOptions => jsonSerializerOptions;

    /// <inheritdoc/>
    public ContentSerializerNameStyle NameStyle => ContentSerializerNameStyle.SystemTextJson;

    /// <summary>
    /// Gets the options with type metadata available. Options that carry no <see cref="JsonSerializerOptions.TypeInfoResolver"/>
    /// get the reflection resolver the Refit.Reflection package registers, or fail with a message that explains how to
    /// supply a source-generated context or register that package.
    /// </summary>
    private JsonSerializerOptions ReadyOptions
    {
        get
        {
            if (jsonSerializerOptions.TypeInfoResolver is null && !jsonSerializerOptions.IsReadOnly)
            {
                jsonSerializerOptions.TypeInfoResolver = ReflectionHooks.CreateDefaultJsonResolver();
            }

            return jsonSerializerOptions;
        }
    }

    /// <summary>Creates new <see cref="JsonSerializerOptions"/> and fills it with default parameters.</summary>
    /// <returns>The default <see cref="JsonSerializerOptions"/>.</returns>
    [SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "Returns a new mutable options instance on each call; a property would wrongly imply a cached value.")]
    [SuppressMessage(
        "Performance",
        "PSH1416:Cache constructed objects in a static readonly field",
        Justification = "Returns a new mutable options instance on each call; callers mutate it, so a cached instance would be shared.")]
    public static JsonSerializerOptions GetDefaultJsonSerializerOptions()
    {
        // Default to case insensitive property name matching as that's likely the behavior most users expect
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET10_0_OR_GREATER
            AllowDuplicateProperties = false,
#endif
        };

        return jsonSerializerOptions;
    }

    /// <summary>
    /// Creates <see cref="JsonSerializerOptions"/> that are eligible for the System.Text.Json source-generated
    /// fast-path on serialization. They are the same as <see cref="GetDefaultJsonSerializerOptions"/>: Refit adds no
    /// converters and leaves number handling at its default, because either would disable the fast-path. Assign a generated TypeInfoResolver (a
    /// <see cref="JsonSerializerContext"/> generated in <see cref="JsonSourceGenerationMode.Serialization"/> or
    /// <see cref="JsonSourceGenerationMode.Default"/> mode) to enable it. The fast-path runs through the synchronous
    /// serialization primitives (<c>SerializeToUtf8Bytes</c> / <c>Serialize(Utf8JsonWriter, ...)</c>); the
    /// async content path can also use it for repeated small payloads when the runtime's size checks permit it.
    /// </summary>
    /// <returns>Fast-path-eligible <see cref="JsonSerializerOptions"/>.</returns>
    [SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "Returns a new mutable options instance on each call; a property would wrongly imply a cached value.")]
    [SuppressMessage(
        "Performance",
        "PSH1416:Cache constructed objects in a static readonly field",
        Justification = "Returns a new mutable options instance on each call; callers mutate it, so a cached instance would be shared.")]
    public static JsonSerializerOptions GetFastPathJsonSerializerOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET10_0_OR_GREATER
            AllowDuplicateProperties = false,
#endif
        };

    /// <inheritdoc/>
    public HttpContent ToHttpContent<T>(T item)
    {
        var options = ReadyOptions;
        var serializeByRuntimeType = item is not null
            && (typeof(T).IsInterface || typeof(T).IsAbstract)
            && !DeclaredTypeIsPolymorphic(typeof(T), options);

        return serializeByRuntimeType
            ? ToHttpContentRuntimeTyped(item!, item!.GetType())
            : JsonContent.Create(item, GetJsonTypeInfo<T>(options));
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public HttpContent ToHttpContentSynchronous<T>(T item)
    {
        var content = new ByteArrayContent(SerializeToUtf8Bytes(item));
        content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        return content;
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default) =>
        content.ReadFromJsonAsync(GetJsonTypeInfo<T>(), cancellationToken);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public T? DeserializeFromString<T>(string content) =>
        JsonSerializer.Deserialize(content, GetJsonTypeInfo<T>());

    /// <inheritdoc/>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
        Stream stream,
        StreamingContentFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentExceptionHelper.ThrowIfNull(stream);

        return format switch
        {
            StreamingContentFormat.ServerSentEvents => DeserializeServerSentEventsAsync<T>(stream, cancellationToken),
            StreamingContentFormat.JsonLines => DeserializeJsonLinesAsync<T>(stream, cancellationToken),
            _ => DeserializeJsonArrayAsync<T>(stream, cancellationToken),
        };
    }

    /// <summary>Determines whether the declared type is configured for polymorphic serialization.</summary>
    /// <param name="type">The declared type to inspect.</param>
    /// <param name="jsonSerializerOptions">The serializer options to consult for type metadata.</param>
    /// <returns><see langword="true"/> if the type is polymorphic; otherwise <see langword="false"/>.</returns>
    internal static bool DeclaredTypeIsPolymorphic(Type type, JsonSerializerOptions jsonSerializerOptions) =>
        GetJsonTypeInfo(type, jsonSerializerOptions).PolymorphismOptions is not null;

#if !NET9_0_OR_GREATER
    /// <summary>Determines whether a buffered line is blank, ignoring a trailing carriage return and treating spaces and tabs as blank.</summary>
    /// <param name="lineBuffer">The buffer holding the line bytes.</param>
    /// <param name="lineStart">The offset of the line.</param>
    /// <param name="lineLength">The length of the line.</param>
    /// <returns><see langword="true"/> when the line contains only spaces and tabs.</returns>
    internal static bool IsBlankLine(byte[] lineBuffer, int lineStart, int lineLength)
    {
        var line = new ReadOnlySpan<byte>(lineBuffer, lineStart, lineLength);
        if (!line.IsEmpty && line[line.Length - 1] == (byte)'\r')
        {
            line = line.Slice(0, line.Length - 1);
        }

        foreach (var b in line)
        {
            if (b is not ((byte)' ' or (byte)'\t'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Grows the pooled line-scan buffer, copying the buffered prefix into a larger rented buffer and returning the old one to the pool.</summary>
    /// <param name="buffer">The current buffer, returned to the pool once its prefix is copied.</param>
    /// <param name="length">The number of buffered bytes to preserve.</param>
    /// <param name="growthFactor">The factor by which the buffer capacity grows.</param>
    /// <returns>The larger rented buffer holding the preserved prefix.</returns>
    internal static byte[] GrowLineScanBuffer(byte[] buffer, int length, int growthFactor)
    {
        var larger = ArrayPool<byte>.Shared.Rent(buffer.Length * growthFactor);
        Buffer.BlockCopy(buffer, 0, larger, 0, length);
        ArrayPool<byte>.Shared.Return(buffer);
        return larger;
    }
#endif

    /// <summary>Gets the JSON type metadata for the given runtime type from the supplied options.</summary>
    /// <param name="type">The runtime type to resolve metadata for.</param>
    /// <param name="jsonSerializerOptions">The serializer options to consult.</param>
    /// <returns>The JSON type metadata.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static JsonTypeInfo GetJsonTypeInfo(Type type, JsonSerializerOptions jsonSerializerOptions) =>
        jsonSerializerOptions.GetTypeInfo(type); // Returns non-null metadata for the requested type or throws; there is no null case to guard.

    /// <summary>Gets the JSON type metadata for the given type from the supplied options.</summary>
    /// <typeparam name="T">The type to resolve metadata for.</typeparam>
    /// <param name="jsonSerializerOptions">The serializer options to consult.</param>
    /// <returns>The JSON type metadata.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static JsonTypeInfo<T> GetJsonTypeInfo<T>(JsonSerializerOptions jsonSerializerOptions) =>
#if NET11_0_OR_GREATER
        jsonSerializerOptions.GetTypeInfo<T>();
#else
        (JsonTypeInfo<T>)GetJsonTypeInfo(typeof(T), jsonSerializerOptions);
#endif

    /// <summary>Determines whether <see cref="ToHttpContent{T}(T)"/> writes a declared-type value as it writes it passed as <see cref="object"/>.</summary>
    /// <param name="declaredType">The declared type of the values.</param>
    /// <returns>
    /// <see langword="true"/> when the type has no derived types (a value type or sealed class), so the runtime type
    /// the <see cref="object"/> path resolves is always <paramref name="declaredType"/>, and no configured converter
    /// replaces the built-in handling of <see cref="object"/>.
    /// </returns>
    internal bool WritesDeclaredTypeAsObject(Type declaredType)
    {
        if (!declaredType.IsValueType && !declaredType.IsSealed)
        {
            return false;
        }

        foreach (var converter in jsonSerializerOptions.Converters)
        {
            // A converter replacing the built-in handling of object forces the untyped fallback.
            if (converter.CanConvert(typeof(object)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Gets the JSON type metadata for the given type from the configured options.</summary>
    /// <typeparam name="T">The type to resolve metadata for.</typeparam>
    /// <returns>The JSON type metadata.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal JsonTypeInfo<T> GetJsonTypeInfo<T>() => GetJsonTypeInfo<T>(ReadyOptions);

    /// <summary>Serializes the item to HTTP content using the supplied runtime type.</summary>
    /// <param name="item">The item to serialize.</param>
    /// <param name="runtimeType">The runtime type to serialize the item as.</param>
    /// <returns>The serialized HTTP content.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal JsonContent ToHttpContentRuntimeTyped(object item, Type runtimeType) =>
        JsonContent.Create(item, GetJsonTypeInfo(runtimeType, ReadyOptions));

    /// <summary>Serializes an item to UTF-8 JSON bytes through the configured type metadata.</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <returns>The UTF-8 encoded JSON.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal byte[] SerializeToUtf8Bytes<T>(T item) =>
        JsonSerializer.SerializeToUtf8Bytes(item, GetJsonTypeInfo<T>());

    /// <summary>Writes an item to a <see cref="Utf8JsonWriter"/> through the configured type metadata.</summary>
    /// <typeparam name="T">The type of the item being serialized.</typeparam>
    /// <param name="item">The item to serialize.</param>
    /// <param name="writer">The writer to serialize into.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal void SerializeToWriter<T>(T item, Utf8JsonWriter writer) =>
        JsonSerializer.Serialize(writer, item, GetJsonTypeInfo<T>());

    /// <summary>Streams the elements of a single top-level JSON array.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal IAsyncEnumerable<T?> DeserializeJsonArrayAsync<T>(Stream stream, CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsyncEnumerable(stream, GetJsonTypeInfo<T>(), cancellationToken);

    /// <summary>Streams newline-delimited JSON values from the response body.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal IAsyncEnumerable<T?> DeserializeJsonLinesAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
#if NET9_0_OR_GREATER
        // .NET 9 added the topLevelValues overload, which reads a stream of whitespace-separated
        // top-level JSON values - exactly the JSON Lines framing - without per-line buffering.
        return JsonSerializer.DeserializeAsyncEnumerable(stream, GetJsonTypeInfo<T>(), topLevelValues: true, cancellationToken);
#else
        return SystemTextJsonStreamingDeserializer.DeserializeJsonLinesManualAsync<T>(stream, ReadyOptions, cancellationToken);
#endif
    }

    /// <summary>Streams the JSON <c>data</c> payload of each server-sent event, yielding one deserialized value per event as it arrives.</summary>
    /// <typeparam name="T">The element type each event's <c>data</c> payload is deserialized to.</typeparam>
    /// <param name="stream">The unbuffered response body stream.</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized event payloads.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    [ExcludeFromCodeCoverage] // async-iterator dispose-mode epilogue: the compiler-generated <>w__disposeMode false-edge cannot be exercised or removed.
    internal async IAsyncEnumerable<T?> DeserializeServerSentEventsAsync<T>(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = ReadyOptions;
        var parser = SseParser.Create(
            stream,
            (eventType, data) => SystemTextJsonStreamingDeserializer.DeserializeSseData<T>(eventType, data, options));
        await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return item.Data;
        }
    }
}
