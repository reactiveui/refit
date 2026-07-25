// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Interface used to construct typed and untyped reflection request builders.</summary>
public interface IReflectionIndexedApi
{
    /// <summary>Gets indexed items.</summary>
    /// <param name="items">The indexed items.</param>
    /// <returns>The response.</returns>
    [Get("/items")]
    Task<string> Get([Query(CollectionFormat.Indexed)] IReadOnlyList<IndexedItem> items);

    /// <summary>Gets indexed items without a response value.</summary>
    /// <param name="items">The indexed items.</param>
    /// <returns>A task representing the request.</returns>
    [Get("/items/void")]
    Task GetVoid([Query(CollectionFormat.Indexed)] IReadOnlyList<IndexedItem> items);

    /// <summary>Gets indexed items with response metadata.</summary>
    /// <param name="items">The indexed items.</param>
    /// <returns>The response and its metadata.</returns>
    [Get("/items/response")]
    Task<ApiResponse<string>> GetResponse([Query(CollectionFormat.Indexed)] IReadOnlyList<IndexedItem> items);
}
