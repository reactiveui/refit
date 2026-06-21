// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
    : IHttpContentSerializer
{
    /// <summary>Justification shared by the reflection-fallback trim/AOT suppressions.</summary>
    private const string ReflectionFallbackJustification =
        "Serializing or deserializing without supplied JSON type metadata requires runtime serializer metadata.";

    /// <summary>Initializes a new instance of the <see cref="SystemTextJsonContentSerializer"/> class.</summary>
    public SystemTextJsonContentSerializer()
        : this(GetDefaultJsonSerializerOptions())
    {
    }

    /// <summary>Creates new <see cref="JsonSerializerOptions"/> and fills it with default parameters.</summary>
    /// <returns>The default <see cref="JsonSerializerOptions"/>.</returns>
    public static JsonSerializerOptions GetDefaultJsonSerializerOptions()
    {
        // Default to case insensitive property name matching as that's likely the behavior most users expect
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET10_0_OR_GREATER
            AllowDuplicateProperties = false
#endif
        };
        jsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
        jsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter());

        return jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public HttpContent ToHttpContent<T>(T item)
    {
        if (item is not null
            && (typeof(T).IsInterface || typeof(T).IsAbstract)
            && !DeclaredTypeIsPolymorphic(typeof(T), jsonSerializerOptions))
        {
            return ToHttpContentRuntimeTyped(item, item.GetType());
        }

#if NET8_0_OR_GREATER
        if (jsonSerializerOptions.TypeInfoResolver is not null)
        {
            return JsonContent.Create(item, GetJsonTypeInfo<T>());
        }
#endif
        return ToHttpContentReflection(item);
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public async Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        if (jsonSerializerOptions.TypeInfoResolver is not null)
        {
            return await content
                .ReadFromJsonAsync(GetJsonTypeInfo<T>(), cancellationToken)
                .ConfigureAwait(false);
        }
#endif
        return await FromHttpContentReflectionAsync<T>(content, cancellationToken).ConfigureAwait(false);
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
        if (type.IsDefined(typeof(JsonPolymorphicAttribute), false)
            || type.IsDefined(typeof(JsonDerivedTypeAttribute), false))
        {
            return true;
        }

#if NET8_0_OR_GREATER
        if (jsonSerializerOptions.TypeInfoResolver is null)
        {
            return false;
        }

        return GetJsonTypeInfo(type, jsonSerializerOptions).PolymorphismOptions is not null;
#else
        _ = jsonSerializerOptions;
        return false;
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
        if (jsonSerializerOptions.TypeInfoResolver is not null)
        {
            return JsonContent.Create(item, GetJsonTypeInfo(runtimeType, jsonSerializerOptions));
        }
#endif
        return ToHttpContentRuntimeReflection(item, runtimeType);
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
}
