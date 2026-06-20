// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>An obsolete JSON content serializer that has been replaced by serializers in dedicated packages.</summary>
/// <seealso cref="Refit.IHttpContentSerializer" />
[Obsolete(
    "Use NewtonsoftJsonContentSerializer in the Refit.Newtonsoft.Json package instead",
    true)]
[ExcludeFromCodeCoverage]
[SuppressMessage(
    "Major Code Smell",
    "S1133:Deprecated code should be removed",
    Justification = "Public API type retained for backwards compatibility; cannot be removed without a breaking change.")]
public class JsonContentSerializer : IHttpContentSerializer
{
    /// <summary>Converts to httpcontent.</summary>
    /// <typeparam name="T">Type of the object to serialize from.</typeparam>
    /// <param name="item">Object to serialize.</param>
    /// <returns>
    ///   <see cref="HttpContent" /> that contains the serialized <typeparamref name="T" /> object.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Always thrown; this serializer is obsolete.</exception>
    public HttpContent ToHttpContent<T>(T item) => throw new NotSupportedException();

    /// <summary>Deserializes an object of type <typeparamref name="T" /> from an <see cref="HttpContent" /> object.</summary>
    /// <typeparam name="T">Type of the object to serialize to.</typeparam>
    /// <param name="content">HttpContent object to deserialize.</param>
    /// <param name="cancellationToken">CancellationToken to abort the deserialization.</param>
    /// <returns>
    /// The deserialized object of type <typeparamref name="T" />.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Always thrown; this serializer is obsolete.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameter for inference",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public Task<T?> FromHttpContentAsync<T>(
        HttpContent content,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <summary>
    /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands.
    /// </summary>
    /// <param name="propertyInfo">A PropertyInfo object.</param>
    /// <returns>
    /// The calculated field name.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Always thrown; this serializer is obsolete.</exception>
    public string GetFieldNameForProperty(PropertyInfo propertyInfo) =>
        throw new NotSupportedException();
}
