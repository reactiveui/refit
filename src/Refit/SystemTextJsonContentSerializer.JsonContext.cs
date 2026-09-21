// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Refit;

/// <summary>Registration of a source-generated <see cref="JsonSerializerContext"/> and overloads that take explicit <see cref="JsonTypeInfo{T}"/> metadata.</summary>
public sealed partial class SystemTextJsonContentSerializer : IJsonTypeInfoContentSerializer
{
    /// <summary>Creates a serializer that runs on the settings of a source-generated context, with reflection-based JSON disabled.</summary>
    /// <param name="context">The generated context. Its own <see cref="JsonSerializerContext.Options"/> supply the naming, converters and number handling.</param>
    /// <returns>A serializer whose options are the context's options.</returns>
    /// <remarks>A type the context does not describe throws <see cref="NotSupportedException"/> instead of falling back to reflection.</remarks>
    public static SystemTextJsonContentSerializer ForContext(JsonSerializerContext context)
    {
        ArgumentExceptionHelper.ThrowIfNull(context);

        return new(context.Options);
    }

    /// <summary>Creates a serializer that runs on the settings of a source-generated context, optionally with a reflection fallback.</summary>
    /// <param name="context">The generated context. Its own <see cref="JsonSerializerContext.Options"/> supply the naming, converters and number handling.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type the context does not describe use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>A serializer whose options are the context's options.</returns>
    public static SystemTextJsonContentSerializer ForContext(
        JsonSerializerContext context,
        bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(context);

        return allowReflectionFallback
            ? new(ComposeOptions(context.Options, context, true))
            : new(context.Options);
    }

    /// <summary>Creates a serializer that keeps this serializer's settings and adds a source-generated context, with reflection-based JSON disabled.</summary>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <returns>A serializer built on a copy of this serializer's options, or this serializer when the context is already registered.</returns>
    /// <remarks>
    /// The naming policy, converters and every other option come from this serializer, not from the context.
    /// Resolvers already registered stay first and in order. Contract modifiers of a reflection resolver move onto the context.
    /// </remarks>
    public SystemTextJsonContentSerializer WithContext(JsonSerializerContext context) =>
        WithContext(context, false);

    /// <summary>Creates a serializer that keeps this serializer's settings and adds a source-generated context, optionally with a reflection fallback.</summary>
    /// <param name="context">The generated context that supplies metadata for the types it describes.</param>
    /// <param name="allowReflectionFallback"><see langword="true"/> to let a type no resolver describes use reflection-based JSON. Reflection is not trim or Native AOT safe.</param>
    /// <returns>A serializer built on a copy of this serializer's options, or this serializer when the context is already registered.</returns>
    public SystemTextJsonContentSerializer WithContext(
        JsonSerializerContext context,
        bool allowReflectionFallback)
    {
        ArgumentExceptionHelper.ThrowIfNull(context);

        return IsRegistered(jsonSerializerOptions, context, allowReflectionFallback)
            ? this
            : new(ComposeOptions(jsonSerializerOptions, context, allowReflectionFallback));
    }

    /// <summary>Serializes a value with explicit type metadata.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="item">The value to serialize.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>, such as a property of a generated context.</param>
    /// <returns>The JSON content.</returns>
    public HttpContent ToHttpContent<T>(T item, JsonTypeInfo<T> typeInfo)
    {
        ArgumentExceptionHelper.ThrowIfNull(typeInfo);

        return JsonContent.Create(item, typeInfo);
    }

    /// <summary>Serializes a value into a buffered body with explicit type metadata.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="item">The value to serialize.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>UTF-8 JSON content held in a byte array.</returns>
    public HttpContent ToHttpContentSynchronous<T>(T item, JsonTypeInfo<T> typeInfo)
    {
        ArgumentExceptionHelper.ThrowIfNull(typeInfo);

        var content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(item, typeInfo));
        content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        return content;
    }

    /// <summary>Creates a body that serializes a value with explicit type metadata when the request sends it.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="item">The value to serialize.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>UTF-8 JSON content written to the request stream.</returns>
    public HttpContent ToStreamingHttpContent<T>(T item, JsonTypeInfo<T> typeInfo)
    {
        ArgumentExceptionHelper.ThrowIfNull(typeInfo);

        var content = new PushStreamContent(
            async (stream, _, _) =>
            {
                await using (stream.ConfigureAwait(false))
                {
                    await using var writer = new Utf8JsonWriter(stream);
                    JsonSerializer.Serialize(writer, item, typeInfo);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            });
        content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        return content;
    }

    /// <summary>Reads a JSON body with explicit type metadata.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="content">The body to read.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    /// <returns>The value, or <see langword="null"/> for JSON null.</returns>
    public Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentExceptionHelper.ThrowIfNull(content);
        ArgumentExceptionHelper.ThrowIfNull(typeInfo);

        return content.ReadFromJsonAsync(typeInfo, cancellationToken);
    }

    /// <summary>Reads buffered JSON text with explicit type metadata.</summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="content">The JSON text.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>.</param>
    /// <returns>The value, or <see langword="null"/> for JSON null.</returns>
    public T? DeserializeFromString<T>(string content, JsonTypeInfo<T> typeInfo)
    {
        ArgumentExceptionHelper.ThrowIfNull(content);
        ArgumentExceptionHelper.ThrowIfNull(typeInfo);

        return JsonSerializer.Deserialize(content, typeInfo);
    }

    /// <summary>Reads one value at a time from a framed response stream with explicit type metadata.</summary>
    /// <typeparam name="T">The element type to read.</typeparam>
    /// <param name="stream">The response body.</param>
    /// <param name="format">The framing of the stream.</param>
    /// <param name="typeInfo">The metadata that describes <typeparamref name="T"/>. Its options drive the read.</param>
    /// <param name="cancellationToken">A token that cancels enumeration.</param>
    /// <returns>The values as they arrive.</returns>
    public IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
        Stream stream,
        StreamingContentFormat format,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentExceptionHelper.ThrowIfNull(typeInfo);

        return new SystemTextJsonContentSerializer(typeInfo.Options).DeserializeStreamAsync<T>(stream, format, cancellationToken);
    }

    /// <summary>Determines whether the options already resolve a context, and reflection only when it is allowed.</summary>
    /// <param name="options">The options to inspect.</param>
    /// <param name="context">The context to look for.</param>
    /// <param name="allowReflectionFallback">Whether a reflection resolver is expected.</param>
    /// <returns><see langword="true"/> when the options need no change.</returns>
    internal static bool IsRegistered(
        JsonSerializerOptions options,
        JsonSerializerContext context,
        bool allowReflectionFallback)
    {
        var hasContext = false;
        var hasReflection = false;
        foreach (var resolver in options.TypeInfoResolverChain)
        {
            hasContext |= ReferenceEquals(resolver, context);
            hasReflection |= resolver is DefaultJsonTypeInfoResolver;
        }

        return hasContext && hasReflection == allowReflectionFallback;
    }

    /// <summary>Copies the options and places the context after the resolvers they already carry.</summary>
    /// <param name="source">The options to copy. They are never changed.</param>
    /// <param name="context">The context to register.</param>
    /// <param name="allowReflectionFallback">Whether reflection may describe the types no other resolver describes.</param>
    /// <returns>New options that keep everything in <paramref name="source"/> and resolve <paramref name="context"/>.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ReflectionFallbackJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = ReflectionFallbackJustification)]
    internal static JsonSerializerOptions ComposeOptions(
        JsonSerializerOptions source,
        JsonSerializerContext context,
        bool allowReflectionFallback)
    {
        var options = new JsonSerializerOptions(source);
        var chain = options.TypeInfoResolverChain;

        List<IJsonTypeInfoResolver> leading = [];
        List<DefaultJsonTypeInfoResolver> reflective = [];
        SplitResolvers(chain, context, leading, reflective);

        IJsonTypeInfoResolver contextResolver = context;
        if (!allowReflectionFallback)
        {
            contextResolver = CarryModifiers(context, reflective);
            reflective.Clear();
        }
        else if (reflective.Count == 0)
        {
            reflective.Add(new());
        }

        chain.Clear();
        AddAll(chain, leading);
        chain.Add(contextResolver);
        AddAll(chain, reflective);

        return options;
    }

    /// <summary>Separates reflection resolvers from the resolvers that stay ahead of the context.</summary>
    /// <param name="chain">The resolvers the options carry.</param>
    /// <param name="context">The context being registered. It is dropped so it is not listed twice.</param>
    /// <param name="leading">Receives the resolvers that are not reflection based.</param>
    /// <param name="reflective">Receives the reflection resolvers.</param>
    internal static void SplitResolvers(
        IList<IJsonTypeInfoResolver> chain,
        JsonSerializerContext context,
        List<IJsonTypeInfoResolver> leading,
        List<DefaultJsonTypeInfoResolver> reflective)
    {
        foreach (var resolver in chain)
        {
            if (ReferenceEquals(resolver, context))
            {
                continue;
            }

            if (resolver is DefaultJsonTypeInfoResolver reflectionResolver)
            {
                reflective.Add(reflectionResolver);
            }
            else
            {
                leading.Add(resolver);
            }
        }
    }

    /// <summary>Applies the contract modifiers of reflection resolvers to another resolver, so registrations such as polymorphism survive.</summary>
    /// <param name="resolver">The resolver that receives the modifiers.</param>
    /// <param name="reflective">The reflection resolvers whose modifiers are carried over.</param>
    /// <returns>The resolver with every modifier applied.</returns>
    internal static IJsonTypeInfoResolver CarryModifiers(
        IJsonTypeInfoResolver resolver,
        List<DefaultJsonTypeInfoResolver> reflective)
    {
        foreach (var reflectionResolver in reflective)
        {
            foreach (var modifier in reflectionResolver.Modifiers)
            {
                resolver = resolver.WithAddedModifier(modifier);
            }
        }

        return resolver;
    }

    /// <summary>Appends every resolver to a chain.</summary>
    /// <typeparam name="TResolver">The resolver type.</typeparam>
    /// <param name="chain">The chain to extend.</param>
    /// <param name="resolvers">The resolvers to append in order.</param>
    internal static void AddAll<TResolver>(IList<IJsonTypeInfoResolver> chain, List<TResolver> resolvers)
        where TResolver : IJsonTypeInfoResolver
    {
        foreach (var resolver in resolvers)
        {
            chain.Add(resolver);
        }
    }
}
#endif
