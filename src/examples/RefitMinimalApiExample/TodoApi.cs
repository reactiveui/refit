// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.MinimalApi.Example;

/// <summary>Example implementation for the Refit interface.</summary>
public sealed class TodoApi : ITodoApi
{
    /// <summary>The identifier assigned to created items.</summary>
    private const int CreatedItemId = 100;

    /// <inheritdoc/>
    public Task<Todo> GetAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(new Todo(id, $"sample item {id}"));

    /// <inheritdoc/>
    public Task<Todo> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new Todo(CreatedItemId, request.Title));
}
