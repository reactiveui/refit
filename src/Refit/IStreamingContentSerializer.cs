// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>
/// An optional capability for an <see cref="IHttpContentSerializer"/> that can deserialize a response body
/// incrementally into an <see cref="IAsyncEnumerable{T}"/>. Refit uses this for interface methods that return
/// <see cref="IAsyncEnumerable{T}"/>, reading items as they arrive instead of buffering the whole response.
/// </summary>
public interface IStreamingContentSerializer
{
    /// <summary>Deserializes a response <paramref name="stream"/> into a sequence of <typeparamref name="T"/> values.</summary>
    /// <typeparam name="T">The element type to deserialize.</typeparam>
    /// <param name="stream">The response body stream to read.</param>
    /// <param name="format">How the body is framed (a single JSON array or newline-delimited JSON).</param>
    /// <param name="cancellationToken">A token to cancel enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized values.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Type parameter intentionally specified explicitly by callers.")]
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Optional CancellationToken is part of the published interface contract; overloads need default interface methods unavailable on netstandard2.0/net4x.")]
    IAsyncEnumerable<T?> DeserializeStreamAsync<T>(
        Stream stream,
        StreamingContentFormat format,
        CancellationToken cancellationToken = default);
}
