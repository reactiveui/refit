// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;

namespace Refit.Documentation;

/// <summary>Uses the upload service's singular field name for repeated tags.</summary>
/// <param name="json">The serializer that reads and writes request and response bodies.</param>
internal sealed class UploadFieldSerializer(IHttpContentSerializer json) : IHttpContentSerializer, IReflectionFieldNameProvider
{
    /// <inheritdoc/>
    public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
        propertyInfo.DeclaringType == typeof(FormFields) && propertyInfo.Name == nameof(FormFields.Tags) ? "tag" : null;

    /// <inheritdoc/>
    public HttpContent ToHttpContent<T>(T item) => json.ToHttpContent(item);

    /// <inheritdoc/>
    public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken) =>
        json.FromHttpContentAsync<T>(content, cancellationToken);
}
