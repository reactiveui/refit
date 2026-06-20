// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.MinimalApi.Example;

/// <summary>Refit interface exposed by the Minimal API example.</summary>
public interface ITodoApi
{
    /// <summary>Gets an item.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The item.</returns>
    [Get("/todos/{id}")]
    Task<Todo> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Creates an item.</summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created item.</returns>
    [Post("/todos")]
    Task<Todo> CreateAsync([Body] CreateTodoRequest request, CancellationToken cancellationToken);
}
