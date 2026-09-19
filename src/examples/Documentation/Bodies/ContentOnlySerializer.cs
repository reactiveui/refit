// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Refit.Documentation;

/// <summary>Exposes only IHttpContentSerializer while retaining generated JSON metadata.</summary>
internal sealed class ContentOnlySerializer : IHttpContentSerializer
{
    /// <summary>The concrete generated-metadata serializer used by this capability wrapper.</summary>
    private readonly SystemTextJsonContentSerializer _serializer;

    /// <summary>Initializes a new instance of the <see cref="ContentOnlySerializer"/> class.</summary>
    /// <param name="serializer">The underlying serializer.</param>
    internal ContentOnlySerializer(SystemTextJsonContentSerializer serializer) => _serializer = serializer;

    /// <summary>Creates normal content without exposing synchronous serialization.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="item">The body value.</param>
    /// <returns>Content with generated metadata.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpContent ToHttpContent<T>(T item) => _serializer.ToHttpContent(item);

    /// <summary>Reads a reply through generated metadata.</summary>
    /// <typeparam name="T">The reply type.</typeparam>
    /// <param name="content">The reply content.</param>
    /// <param name="cancellationToken">Cancellation for deserialization.</param>
    /// <returns>The deserialized reply.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken) => _serializer.FromHttpContentAsync<T>(content, cancellationToken);

    /// <summary>Delegates the explicit attribute-based naming hook.</summary>
    /// <param name="propertyInfo">The known property to inspect.</param>
    /// <returns>The explicit JSON name, or null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetFieldNameForProperty(PropertyInfo propertyInfo) => _serializer.GetFieldNameForProperty(propertyInfo);
}
