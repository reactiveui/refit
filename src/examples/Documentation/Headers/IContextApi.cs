// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Combines interface context, direct authorization and a captured request body.</summary>
internal interface IContextApi
{
    /// <summary>Gets or sets context shared by this client instance.</summary>
    [Property("tenant")]
    string Tenant { get; set; }

    /// <summary>Builds a Basic-authorized request with a nullable replacement header.</summary>
    /// <param name="token">The already encoded Basic credentials.</param>
    /// <param name="app">Null removes the static application header.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/context")]
    [Headers("X-App: Delivery")]
    Task<HttpRequestMessage> BuildAsync([Authorize("Basic")] string token, [Header("X-App")] string? app);

    /// <summary>Sends a person and supplies per-call context overriding the interface property.</summary>
    /// <param name="person">The body serialized with generated JSON metadata.</param>
    /// <param name="tenant">The tenant for this call.</param>
    /// <param name="cancellationToken">Cancels the send and body read.</param>
    /// <returns>A task that completes after a successful HTTP response.</returns>
    [Post("/context")]
    Task SaveAsync([Body] Person person, [Property("tenant")] string tenant, CancellationToken cancellationToken);
}
