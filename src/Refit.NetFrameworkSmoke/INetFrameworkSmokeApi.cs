// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.NetFrameworkSmoke;

/// <summary>The Refit interface exercised by the .NET Framework smoke test. Every method generates inline, so the
/// smoke test never needs the reflection request builder.</summary>
internal interface INetFrameworkSmokeApi
{
    /// <summary>Fetches a todo by identifier.</summary>
    /// <param name="id">The todo identifier.</param>
    /// <returns>The fetched todo.</returns>
    [Get("/todos/{id}")]
    Task<Todo> GetTodoAsync(int id);

    /// <summary>Creates a todo.</summary>
    /// <param name="todo">The todo to create.</param>
    /// <returns>The created todo.</returns>
    [Post("/todos")]
    Task<Todo> CreateTodoAsync([Body] Todo todo);

    /// <summary>Searches todos.</summary>
    /// <param name="q">The search term.</param>
    /// <param name="page">The page number.</param>
    /// <returns>The raw response body.</returns>
    [Get("/search")]
    Task<string> SearchAsync(string q, int page);
}
