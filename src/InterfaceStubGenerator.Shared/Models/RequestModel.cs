// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes an HTTP operation and its request bindings.</summary>
/// <param name="HttpMethod">The HTTP method name.</param>
/// <param name="Path">The route template.</param>
/// <param name="Return">The response type and disposal policy.</param>
/// <param name="Generation">The inline generation strategy and fallback.</param>
/// <param name="Bindings">The static headers and parameter bindings.</param>
/// <param name="Body">The multipart body configuration.</param>
internal readonly record struct RequestModel(
    string HttpMethod,
    string Path,
    RequestReturnModel Return,
    RequestGenerationModel Generation,
    RequestBindingsModel Bindings,
    RequestBodyModel Body)
{
    /// <summary>Gets an empty model used for non-Refit method placeholders.</summary>
    internal static RequestModel Empty { get; } = new(
        string.Empty,
        string.Empty,
        new RequestReturnModel(string.Empty, string.Empty, false, true),
        new RequestGenerationModel(false, null, default),
        new RequestBindingsModel([], []),
        new RequestBodyModel(default, string.Empty));

    /// <summary>Gets the <c>System.UriFormat</c> value from the method's <c>[QueryUriFormat]</c> attribute, or null when
    /// absent. When set, the built path and query are re-encoded with this mode, matching the reflection builder's final
    /// <c>Uri.GetComponents(PathAndQuery, QueryUriFormat)</c> pass.</summary>
    internal int? QueryUriFormat { get; init; }

    /// <summary>Gets how a valid <c>[Paged]</c> method pages; <see langword="null"/> for every other method.</summary>
    internal PagingModel? Paging { get; init; }

    /// <summary>Gets the per-call timeout in milliseconds from the method's <c>[Timeout]</c> attribute, or 0 when absent.
    /// The value is emitted into the generated send call and layered onto the request's effective cancellation token,
    /// mirroring the reflection builder's <c>RestMethodInfoInternal.TimeoutMilliseconds</c>.</summary>
    internal int TimeoutMilliseconds { get; init; }

    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(RequestModel other) =>
        EqualityComparer<string>.Default.Equals(
            HttpMethod,
            other.HttpMethod)
        && EqualityComparer<string>.Default.Equals(
            Path,
            other.Path)
        && EqualityComparer<RequestReturnModel>.Default.Equals(
            Return,
            other.Return)
        && EqualityComparer<RequestGenerationModel>.Default.Equals(
            Generation,
            other.Generation)
        && EqualityComparer<RequestBindingsModel>.Default.Equals(
            Bindings,
            other.Bindings)
        && EqualityComparer<RequestBodyModel>.Default.Equals(
            Body,
            other.Body)
        && EqualityComparer<int?>.Default.Equals(
            QueryUriFormat,
            other.QueryUriFormat)
        && EqualityComparer<PagingModel?>.Default.Equals(
            Paging,
            other.Paging)
        && EqualityComparer<int>.Default.Equals(TimeoutMilliseconds, other.TimeoutMilliseconds);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(HttpMethod));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Path));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Return));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Generation));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Bindings));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Body));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(QueryUriFormat));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Paging));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(TimeoutMilliseconds));
        return hash;
    }
}
