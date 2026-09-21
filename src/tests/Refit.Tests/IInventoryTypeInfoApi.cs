// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>An inventory API whose methods take the JSON metadata for a call as a parameter.</summary>
internal interface IInventoryTypeInfoApi
{
    /// <summary>Gets one stock line.</summary>
    /// <param name="id">The identifier of the stock line.</param>
    /// <param name="typeInfo">The metadata used to read the reply.</param>
    /// <returns>The stock line.</returns>
    [Get("/items/{id}")]
    Task<InventoryItem> GetItem(int id, JsonTypeInfo<InventoryItem> typeInfo);

    /// <summary>Gets one stock line as an API response.</summary>
    /// <param name="id">The identifier of the stock line.</param>
    /// <param name="typeInfo">The metadata used to read the reply body.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>The API response holding the stock line.</returns>
    [Get("/items/{id}")]
    Task<ApiResponse<InventoryItem>> GetItemResponse(int id, JsonTypeInfo<InventoryItem> typeInfo, CancellationToken cancellationToken);

    /// <summary>Lists the stock lines.</summary>
    /// <param name="typeInfo">The metadata used to read the reply.</param>
    /// <returns>The stock lines.</returns>
    [Get("/items")]
    Task<List<InventoryItem>> ListItems(JsonTypeInfo<List<InventoryItem>> typeInfo);

    /// <summary>Adds a stock line.</summary>
    /// <param name="item">The stock line to add.</param>
    /// <param name="typeInfo">The metadata used to write the body and read the reply.</param>
    /// <returns>The stored stock line.</returns>
    [Post("/items")]
    Task<InventoryItem> AddItem([Body] InventoryItem item, JsonTypeInfo<InventoryItem> typeInfo);

    /// <summary>Adds a stock line and reads a differently shaped reply.</summary>
    /// <param name="item">The stock line to add.</param>
    /// <param name="itemInfo">The metadata used to write the body.</param>
    /// <param name="noteInfo">The metadata used to read the reply.</param>
    /// <returns>The note stored with the stock line.</returns>
    [Post("/notes")]
    Task<StockNote> AddItemNote([Body] InventoryItem item, JsonTypeInfo<InventoryItem> itemInfo, JsonTypeInfo<StockNote> noteInfo);

    /// <summary>Streams the stock lines.</summary>
    /// <param name="typeInfo">The metadata used to read each element.</param>
    /// <returns>The stock lines as they arrive.</returns>
    [Get("/items")]
    IAsyncEnumerable<InventoryItem> StreamItems(JsonTypeInfo<InventoryItem> typeInfo);

    /// <summary>Watches one stock line as a cold observable.</summary>
    /// <param name="id">The identifier of the stock line.</param>
    /// <param name="typeInfo">The metadata used to read the reply.</param>
    /// <returns>An observable that yields the stock line.</returns>
    [Get("/items/{id}")]
    IObservable<InventoryItem> WatchItem(int id, JsonTypeInfo<InventoryItem> typeInfo);
}
