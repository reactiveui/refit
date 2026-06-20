// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.MinimalApi.Tests;

/// <summary>Refit interface used by the Minimal API mapping tests.</summary>
internal interface ITodoApi
{
    /// <summary>Gets an item.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="tag">A query value.</param>
    /// <param name="trace">A header value.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The item.</returns>
    [Get("/todos/{id}")]
    Task<Todo> GetTodoAsync(
        [AliasAs("id")] int id,
        [AliasAs("tag")] string tag,
        [Header("X-Trace")] string trace,
        CancellationToken cancellationToken);

    /// <summary>Creates an item.</summary>
    /// <param name="request">The create request.</param>
    /// <param name="trace">A header value.</param>
    /// <returns>The created item.</returns>
    [Post("/todos")]
    Task<Todo> CreateTodoAsync([Body] CreateTodoRequest request, [Header("X-Trace")] string trace);
}
