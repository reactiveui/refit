// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Refit;

/// <summary>Request body content creation for source-generated requests: JSON, JSON Lines, and URL-encoded forms.</summary>
public static partial class GeneratedRequestRunner
{
    /// <summary>Serializes a generated request body using Refit body rules.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="serializationMethod">The configured body serialization method.</param>
    /// <param name="streamBody">Whether serialized content should be streamed into the request.</param>
    /// <returns>The HTTP content for the body.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    public static HttpContent CreateBodyContent<TBody>(
        RefitSettings settings,
        TBody body,
        BodySerializationMethod serializationMethod,
        bool streamBody)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        if (serializationMethod == BodySerializationMethod.Default && body is string stringBody)
        {
            return new StringContent(stringBody);
        }

        var content = CreateSerializedBodyContent(settings, body, serializationMethod);

        // A synchronously-serialized body is already a buffer (and lets the fast-path engage), so never re-stream it.
        return streamBody && !UsesSynchronousSerialization(settings)
            ? new PushStreamContent(
                async (stream, _, _) =>
                {
#if NET8_0_OR_GREATER
                    await using (stream.ConfigureAwait(false))
#else
                    using (stream)
#endif
                    {
                        await content.CopyToAsync(stream).ConfigureAwait(false);
                    }
                },
                content.Headers.ContentType)
            : content;
    }

    /// <summary>Serializes a generated request body as JSON Lines (newline-delimited JSON).</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The enumerable body value.</param>
    /// <returns>The HTTP content for the JSON Lines body.</returns>
    public static HttpContent CreateJsonLinesBodyContent<TBody>(
        RefitSettings settings,
        TBody body)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        var items = body is IEnumerable enumerable and not string
            ? enumerable
            : new[] { (object?)body };

        return new JsonLinesContent(items, settings.ContentSerializer);
    }

    /// <summary>Serializes a generated URL-encoded request body using the declared body type.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <returns>The HTTP content for the URL-encoded body.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Generated callers specify the declared body type for AOT-safe form property discovery.")]
    public static HttpContent CreateUrlEncodedBodyContent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TBody>(
        RefitSettings settings,
        TBody body)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        return body is string stringBody
            ? new StringContent(
                StringHelpers.EscapeDataString(stringBody),
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
            : new FormUrlEncodedContent(FormValueMultimap.Create(body, settings));
    }

    /// <summary>Serializes a generated URL-encoded request body using source-generated field descriptors.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="fields">The compile-time field descriptors for the body type.</param>
    /// <returns>The HTTP content for the URL-encoded body.</returns>
    /// <remarks>
    /// The reflection-free path runs only when the configured content serializer resolves field names purely
    /// from attributes the generator already inlined (the built-in <see cref="SystemTextJsonContentSerializer"/>).
    /// For any other serializer the field-name hook may need the runtime <see cref="System.Reflection.PropertyInfo"/>,
    /// so this falls back to the reflection-based <see cref="FormValueMultimap"/>.
    /// </remarks>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Generated callers specify the declared body type for AOT-safe form property discovery.")]
    public static HttpContent CreateUrlEncodedBodyContent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TBody>(
        RefitSettings settings,
        TBody body,
        FormField<TBody>[] fields)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        if (body is string stringBody)
        {
            return new StringContent(
                StringHelpers.EscapeDataString(stringBody),
                Encoding.UTF8,
                "application/x-www-form-urlencoded");
        }

        // The descriptor path only matches the reflection path when the serializer resolves field names from
        // attributes the generator already inlined (the built-in System.Text.Json serializer); otherwise fall back.
        var useDescriptors = body is not null and not System.Collections.IDictionary
                             && settings.ContentSerializer is SystemTextJsonContentSerializer;

        return new FormUrlEncodedContent(
            useDescriptors
                ? FormValueMultimap.CreateFromFields(body, fields, settings)
                : FormValueMultimap.Create(body, settings));
    }

    /// <summary>Determines whether a form body can be serialized by the generated straight-line unrolled fast path.</summary>
    /// <param name="body">The body instance.</param>
    /// <returns><see langword="true"/> when the body is a plain object the unrolled path can flatten field-by-field;
    /// <see langword="false"/> for the <see langword="null"/>, <see cref="HttpContent"/>, <see cref="Stream"/>,
    /// <see cref="string"/>, and <see cref="System.Collections.IDictionary"/> bodies the reflection path special-cases.</returns>
    /// <remarks>The <see cref="NotNullWhenAttribute"/> lets generated code dereference the body directly inside the guard.</remarks>
    public static bool CanUnrollForm([NotNullWhen(true)] object? body) =>
        body is not null
        && body is not HttpContent
        && body is not Stream
        && body is not string
        && body is not System.Collections.IDictionary;
}
