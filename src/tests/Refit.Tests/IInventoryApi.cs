// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A small inventory API used to check clients created on a JSON context.</summary>
internal interface IInventoryApi
{
    /// <summary>Gets one stock line.</summary>
    /// <param name="id">The identifier of the stock line.</param>
    /// <returns>The stock line.</returns>
    [Get("/items/{id}")]
    Task<InventoryItem> GetItem(int id);

    /// <summary>Adds a stock line.</summary>
    /// <param name="item">The stock line to add.</param>
    /// <returns>The stored stock line.</returns>
    [Post("/items")]
    Task<InventoryItem> AddItem([Body] InventoryItem item);

    /// <summary>Gets the note attached to the inventory, a type the inventory context does not describe.</summary>
    /// <returns>The note.</returns>
    [Get("/note")]
    Task<StockNote> GetNote();
}
