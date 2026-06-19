// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Provides content serialization to <see cref="HttpContent"/>.</summary>
public interface IHttpContentSerializer
{
    /// <summary>Serializes an object of type <typeparamref name="T"/> to <see cref="HttpContent"/>.</summary>
    /// <typeparam name="T">Type of the object to serialize from.</typeparam>
    /// <param name="item">Object to serialize.</param>
    /// <returns><see cref="HttpContent"/> that contains the serialized <typeparamref name="T"/> object.</returns>
#if !NET8_0_OR_GREATER
    [RequiresUnreferencedCode("System.Text.Json serialization may require metadata that trimming cannot statically preserve. Use the Refit source generator for trimmed/AOT apps.")]
    [RequiresDynamicCode("System.Text.Json serialization may generate code dynamically for runtime types. Use the Refit source generator for trimmed/AOT apps.")]
#endif
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    HttpContent ToHttpContent<T>(T item);

    /// <summary>Deserializes an object of type <typeparamref name="T"/> from an <see cref="HttpContent"/> object.</summary>
    /// <typeparam name="T">Type of the object to serialize to.</typeparam>
    /// <param name="content">HttpContent object to deserialize.</param>
    /// <param name="cancellationToken">CancellationToken to abort the deserialization.</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/>.</returns>
#if !NET8_0_OR_GREATER
    [RequiresUnreferencedCode("System.Text.Json deserialization may require metadata that trimming cannot statically preserve. Use the Refit source generator for trimmed/AOT apps.")]
    [RequiresDynamicCode("System.Text.Json deserialization may generate code dynamically for runtime types. Use the Refit source generator for trimmed/AOT apps.")]
#endif
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Optional CancellationToken is part of the published interface contract; overloads need default interface methods unavailable on netstandard2.0/net4x.")]
    Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands.
    /// </summary>
    /// <param name="propertyInfo">A PropertyInfo object.</param>
    /// <returns>The calculated field name.</returns>
    string? GetFieldNameForProperty(PropertyInfo propertyInfo);
}
