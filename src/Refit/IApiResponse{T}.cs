// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <inheritdoc/>
/// <typeparam name="T">The deserialized response content type.</typeparam>
public interface IApiResponse<out T> : IApiResponse
{
    /// <summary>Gets the deserialized request content as <typeparamref name="T"/>.</summary>
    T? Content { get; }

    /// <summary>Gets a value indicating whether deserialized <see cref="Content"/> is available.</summary>
    /// <remarks>
    /// This is the covariance-safe way to null-check <see cref="Content"/> with flow analysis:
    /// <c>if (response.HasContent) { /* response.Content is non-null here */ }</c>. The base
    /// success/error/header members are intentionally not redeclared, so a single setup on the
    /// generic interface is observed through the non-generic <see cref="IApiResponse"/> as well.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Content))]
    bool HasContent { get; }
}
