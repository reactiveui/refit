// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose interface-level <see cref="PathPrefixAttribute"/> is prepended to every method's route.</summary>
[PathPrefix("/api/v2")]
public interface IPathPrefixApi
{
    /// <summary>A route whose template already carries a leading slash.</summary>
    /// <returns>The response body.</returns>
    [Get("/users")]
    Task<string> GetUsers();

    /// <summary>A route whose template omits the leading slash, so the join must supply exactly one.</summary>
    /// <returns>The response body.</returns>
    [Get("orders")]
    Task<string> GetOrders();

    /// <summary>A route with a placeholder substituted after the prefix is applied.</summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The response body.</returns>
    [Get("/users/{id}")]
    Task<string> GetUser(int id);

    /// <summary>A route carrying a query parameter, preserved after the prefix is applied.</summary>
    /// <param name="query">The search term.</param>
    /// <returns>The response body.</returns>
    [Get("/search")]
    Task<string> Search(string query);

    /// <summary>A route with a hardcoded query string, preserved after the prefix is applied.</summary>
    /// <returns>The response body.</returns>
    [Get("/items?active=true")]
    Task<string> GetActiveItems();
}
