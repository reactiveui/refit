// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A generic CRUD Refit API keyed by an arbitrary key type.</summary>
/// <typeparam name="T">The entity type managed by the API.</typeparam>
/// <typeparam name="TKey">The type of the entity key.</typeparam>
public interface IDataCrudApi<T, TKey>
    where T : class
{
    /// <summary>Creates a new entity.</summary>
    /// <param name="payload">The entity to create.</param>
    /// <returns>The created entity.</returns>
    [Post("")]
    Task<T> Create([Body] T payload);

    /// <summary>Reads all entities, exercising a method with an unrelated type parameter.</summary>
    /// <typeparam name="TFoo">An unrelated type parameter used to verify generator handling.</typeparam>
    /// <returns>The list of entities.</returns>
    [Get("")]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters to enable type inference",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    [SuppressMessage(
        "StyleSharp",
        "SST1452:Unused type parameters should be removed",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    Task<List<T>> ReadAll<TFoo>()
        where TFoo : new();

    /// <summary>Reads all entities, exercising a method with two unrelated type parameters.</summary>
    /// <typeparam name="TFoo">An unrelated type parameter used to verify generator handling.</typeparam>
    /// <typeparam name="TBar">A second unrelated type parameter used to verify generator handling.</typeparam>
    /// <returns>The list of entities.</returns>
    [Get("")]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters to enable type inference",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    [SuppressMessage(
        "StyleSharp",
        "SST1452:Unused type parameters should be removed",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    Task<List<T>> ReadAll<TFoo, TBar>()
        where TFoo : new()
        where TBar : struct;

    /// <summary>Reads a single entity by key.</summary>
    /// <param name="key">The key of the entity to read.</param>
    /// <returns>The matching entity.</returns>
    [Get("/{key}")]
    Task<T> ReadOne(TKey key);

    /// <summary>Updates the entity with the given key.</summary>
    /// <param name="key">The key of the entity to update.</param>
    /// <param name="payload">The updated entity payload.</param>
    /// <returns>A task that completes when the update finishes.</returns>
    [Put("/{key}")]
    Task Update(TKey key, [Body] T payload);

    /// <summary>Deletes the entity with the given key.</summary>
    /// <param name="key">The key of the entity to delete.</param>
    /// <returns>A task that completes when the delete finishes.</returns>
    [Delete("/{key}")]
    Task Delete(TKey key);

    /// <summary>Reads all entities, exercising a class-constrained unrelated type parameter.</summary>
    /// <typeparam name="TFoo">An unrelated class-constrained type parameter used to verify generator handling.</typeparam>
    /// <returns>A task that completes when the read finishes.</returns>
    [Get("")]
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters to enable type inference",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    [SuppressMessage(
        "StyleSharp",
        "SST1452:Unused type parameters should be removed",
        Justification = "Intentional test fixture exercising Refit generation of generic methods with unused type parameters.")]
    Task ReadAllClasses<TFoo>()
        where TFoo : class, new();
}
