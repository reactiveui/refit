// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.LiveCompilation;

/// <summary>Exercises a generated client compiled into the generator test assembly.</summary>
internal interface ILiveGeneratedApi
{
    /// <summary>Gets or sets the tenant identifier added as a request property.</summary>
    [Property("property-tenant")]
    int TenantId { get; set; }

    /// <summary>Gets a response while applying headers and request properties.</summary>
    /// <param name="id">The header identifier.</param>
    /// <param name="headers">The dynamic headers.</param>
    /// <param name="tenantId">The parameter tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response body.</returns>
    [Headers("X-Static: static")]
    [Get("/users")]
    Task<string> Get(
        [Header("X-Id")] int id,
        [HeaderCollection] IDictionary<string, string> headers,
        [Property("parameter-tenant")] int tenantId,
        CancellationToken cancellationToken);
}
