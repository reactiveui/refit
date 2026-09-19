// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Generated request construction with headers and local request properties.</summary>
[Headers("Accept: application/json", "X-App: Delivery", "X-Remove: old")]
internal interface IHeaderApi
{
    /// <summary>Builds a request with dynamic authorization, headers and local properties without sending it.</summary>
    /// <param name="app">Replaces the static application header.</param>
    /// <param name="headers">Additional request headers.</param>
    /// <param name="token">The bearer token supplied directly to the request.</param>
    /// <param name="tenant">The local tenant property.</param>
    /// <param name="page">The local page property whose key follows the parameter name.</param>
    /// <returns>The constructed request owned by the caller.</returns>
    [Get("/headers")]
    [Headers("X-Remove")]
    Task<HttpRequestMessage> BuildAsync(
        [Header("X-App")] string app,
        [HeaderCollection] IDictionary<string, string> headers,
        [Authorize] string token,
        [Property("tenant")] string tenant,
        [Property] int page);

    /// <summary>Reads a person using the configured authorization token getter.</summary>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The deserialized person body.</returns>
    [Get("/header-person")]
    [Headers("Authorization: Bearer")]
    Task<Person> ReadAsync(CancellationToken cancellationToken);
}
