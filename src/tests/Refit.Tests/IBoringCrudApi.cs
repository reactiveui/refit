// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A generic CRUD Refit interface fixture exercising type parameters and route templates.</summary>
/// <typeparam name="T">The entity type managed by the API.</typeparam>
/// <typeparam name="TKey">The key type used to identify entities.</typeparam>
public interface IBoringCrudApi<T, in TKey>
    where T : class
{
    /// <summary>Creates a new entity.</summary>
    /// <param name="paylod">The entity payload to create.</param>
    /// <returns>The created entity.</returns>
    [Post("")]
    Task<T> Create([Body] T paylod);

    /// <summary>Reads all entities.</summary>
    /// <returns>The list of all entities.</returns>
    [Get("")]
    Task<List<T>> ReadAll();

    /// <summary>Reads a single entity by key.</summary>
    /// <param name="key">The key of the entity to read.</param>
    /// <returns>The matching entity.</returns>
    [Get("/{key}")]
    Task<T> ReadOne(TKey key);

    /// <summary>Updates an existing entity.</summary>
    /// <param name="key">The key of the entity to update.</param>
    /// <param name="payload">The updated entity payload.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Put("/{key}")]
    Task Update(TKey key, [Body] T payload);

    /// <summary>Deletes an entity by key.</summary>
    /// <param name="key">The key of the entity to delete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Delete("/{key}")]
    Task Delete(TKey key);
}
