// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;

namespace Refit;

/// <summary>Explicit <see cref="JsonTypeInfo{T}"/> metadata for source-generated requests: a method parameter supplies the metadata for a call.</summary>
public static partial class GeneratedRequestRunner
{
    /// <summary>The request-options key under which the metadata for the response body is stored.</summary>
    private const string JsonTypeInfoOptionKey = "Refit.JsonTypeInfo";

    /// <summary>Stores the metadata that describes the response body, so the reply is read with it instead of the serializer's own lookup.</summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    /// <param name="request">The generated request message.</param>
    /// <param name="typeInfo">The metadata supplied by the caller, or <see langword="null"/> to use the serializer's own lookup.</param>
    public static void SetRequestJsonTypeInfo<T>(HttpRequestMessage request, JsonTypeInfo<T>? typeInfo)
    {
        if (typeInfo is not null)
        {
            request.Options.Set(new HttpRequestOptionsKey<JsonTypeInfo>(JsonTypeInfoOptionKey), typeInfo);
        }
    }

    /// <summary>Serializes a generated request body with explicit metadata, following the same body rules as the lookup-based overload.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="TBody"/>, or <see langword="null"/> to use the serializer's own lookup.</param>
    /// <param name="serializationMethod">The configured body serialization method.</param>
    /// <param name="streamBody">Whether serialized content should be streamed into the request.</param>
    /// <returns>The HTTP content for the body.</returns>
    /// <exception cref="InvalidOperationException">The content serializer does not accept explicit metadata.</exception>
    public static HttpContent CreateBodyContent<TBody>(
        RefitSettings settings,
        TBody body,
        JsonTypeInfo<TBody>? typeInfo,
        BodySerializationMethod serializationMethod,
        bool streamBody)
    {
        if (typeInfo is null
            || body is HttpContent
            || body is Stream
            || (serializationMethod == BodySerializationMethod.Default && body is string))
        {
            return CreateBodyContent(settings, body, serializationMethod, streamBody);
        }

        var content = CreateSerializedBodyContent(settings, body, typeInfo, serializationMethod);

        return streamBody && !UsesSynchronousSerialization(settings) ? StreamBodyContent(content) : content;
    }

    /// <summary>Gets the metadata a caller supplied for the response body.</summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    /// <param name="request">The request message.</param>
    /// <returns>The metadata, or <see langword="null"/> when the caller supplied none for <typeparamref name="T"/>.</returns>
    internal static JsonTypeInfo<T>? GetRequestJsonTypeInfo<T>(HttpRequestMessage request) =>
        request.Options.TryGetValue(new HttpRequestOptionsKey<JsonTypeInfo>(JsonTypeInfoOptionKey), out var typeInfo)
            ? typeInfo as JsonTypeInfo<T>
            : null;

    /// <summary>Gets the serializer capability that reads and writes with explicit metadata.</summary>
    /// <param name="settings">The Refit settings to use.</param>
    /// <returns>The content serializer as an <see cref="IJsonTypeInfoContentSerializer"/>.</returns>
    /// <exception cref="InvalidOperationException">The content serializer does not accept explicit metadata.</exception>
    internal static IJsonTypeInfoContentSerializer RequireJsonTypeInfoSerializer(RefitSettings settings) =>
        settings.ContentSerializer as IJsonTypeInfoContentSerializer
        ?? throw new InvalidOperationException(
            "A JsonTypeInfo parameter needs a content serializer that implements IJsonTypeInfoContentSerializer, such as SystemTextJsonContentSerializer.");

    /// <summary>Serializes a body value with explicit metadata through the configured content serializer.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="TBody"/>.</param>
    /// <param name="serializationMethod">The configured body serialization method.</param>
    /// <returns>The serialized HTTP content.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="serializationMethod"/> is not one of the serialized body methods.</exception>
    internal static HttpContent CreateSerializedBodyContent<TBody>(
        RefitSettings settings,
        TBody body,
        JsonTypeInfo<TBody> typeInfo,
        BodySerializationMethod serializationMethod)
    {
        if (serializationMethod is not (BodySerializationMethod.Default or BodySerializationMethod.Serialized)
            && !IsObsoleteJsonSerializationMethod(serializationMethod))
        {
            throw new ArgumentOutOfRangeException(nameof(serializationMethod), serializationMethod, null);
        }

        var serializer = RequireJsonTypeInfoSerializer(settings);
        if (settings.ContentSerializer is ISynchronousContentSerializer)
        {
            switch (settings.RequestBodySerialization)
            {
                case RequestBodySerializationMode.Buffered:
                    return serializer.ToHttpContentSynchronous(body, typeInfo);
                case RequestBodySerializationMode.Streamed:
                    return serializer.ToStreamingHttpContent(body, typeInfo);
                default:
                {
                    break;
                }
            }
        }

        return serializer.ToHttpContent(body, typeInfo);
    }
}
#endif
