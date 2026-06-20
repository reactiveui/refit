// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.MinimalApi.Tests;

/// <summary>Test implementation for the Refit interface.</summary>
internal sealed class TodoApi : ITodoApi
{
    /// <inheritdoc/>
    public Task<Todo> GetTodoAsync(
        int id,
        string tag,
        string trace,
        CancellationToken cancellationToken) =>
        Task.FromResult(new Todo(id, $"{tag}:{trace}:{cancellationToken.CanBeCanceled}"));

    /// <inheritdoc/>
    public Task<Todo> CreateTodoAsync(CreateTodoRequest request, string trace) =>
        Task.FromResult(new Todo(RefitEndpointRouteBuilderExtensionsTests.CreatedItemId, $"{request.Title}:{trace}"));
}
