// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>An <see cref="IHttpContentSerializer"/> that intentionally does not support streaming, used to verify the streaming error path.</summary>
public sealed class NonStreamingContentSerializer : IHttpContentSerializer
{
    /// <summary>The serializer used to satisfy the non-streaming operations.</summary>
    private readonly SystemTextJsonContentSerializer _inner = new();

    /// <inheritdoc/>
    public HttpContent ToHttpContent<T>(T item) => _inner.ToHttpContent(item);

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
        _inner.FromHttpContentAsync<T>(content, cancellationToken);

    /// <inheritdoc/>
    public string? GetFieldNameForProperty(PropertyInfo propertyInfo) =>
        _inner.GetFieldNameForProperty(propertyInfo);
}
