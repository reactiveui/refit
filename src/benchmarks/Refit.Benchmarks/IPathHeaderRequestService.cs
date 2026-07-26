// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Request shapes used to compare generated and reflection-built path/header binding.</summary>
public interface IPathHeaderRequestService
{
    /// <summary>Sends a request with one path parameter.</summary>
    /// <param name="id">The path identifier.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/users/{userId}")]
    Task<HttpResponseMessage> PathOnlyAsync([AliasAs("userId")] int id);

    /// <summary>Sends a request with one argument bound to both the path and a header.</summary>
    /// <param name="id">The identifier used by both bindings.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/users/{userId}")]
    Task<HttpResponseMessage> PathAndHeaderAsync(
        [AliasAs("userId")]
        [Header("X-User-Id")]
        int id);
}
