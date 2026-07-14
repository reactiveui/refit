// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Provides factory helpers for creating <see cref="ApiResponse{T}"/> instances.</summary>
internal static class ApiResponse
{
    /// <summary>Creates an API response instance for the given request, response and content.</summary>
    /// <typeparam name="T">The API response interface type.</typeparam>
    /// <typeparam name="TBody">The deserialized body type.</typeparam>
    /// <param name="request">The original HTTP request.</param>
    /// <param name="resp">The HTTP response message.</param>
    /// <param name="content">The deserialized response content.</param>
    /// <param name="settings">The Refit settings used to send the request.</param>
    /// <param name="error">The exception, if the request failed.</param>
    /// <returns>The created API response.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    internal static T Create<T, TBody>(
        HttpRequestMessage request,
        HttpResponseMessage? resp,
        TBody? content,
        RefitSettings settings,
        ApiExceptionBase? error = null) =>
        (T)(object)new ApiResponse<TBody>(request, resp, content, settings, error);
}
