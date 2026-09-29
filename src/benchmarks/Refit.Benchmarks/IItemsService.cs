// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Benchmarks;

/// <summary>The generated Refit client measured against a hand-written <see cref="HttpClient"/> client.</summary>
public interface IItemsService
{
    /// <summary>Reads one item.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The item.</returns>
    [Get("/items/{id}")]
    Task<FastItem> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Reads every item.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The items.</returns>
    [Get("/items")]
    Task<List<FastItem>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Creates an item.</summary>
    /// <param name="item">The item to create.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The created item.</returns>
    [Post("/items")]
    Task<FastItem> CreateAsync([Body] FastItem item, CancellationToken cancellationToken);
}
