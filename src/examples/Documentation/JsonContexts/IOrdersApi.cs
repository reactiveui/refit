// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>Reads and places orders on a small shop's HTTP API.</summary>
internal interface IOrdersApi
{
    /// <summary>Reads one order.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The order the shop returns.</returns>
    [Get("/orders/{id}")]
    Task<Order> GetOrderAsync(int id, CancellationToken cancellationToken);

    /// <summary>Reads every order.</summary>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The orders the shop returns.</returns>
    [Get("/orders")]
    Task<List<Order>> ListOrdersAsync(CancellationToken cancellationToken);

    /// <summary>Places an order.</summary>
    /// <param name="order">The products and customer, sent as the JSON request body.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The order the shop created.</returns>
    [Post("/orders")]
    Task<Order> PlaceOrderAsync([Body] NewOrder order, CancellationToken cancellationToken);

    /// <summary>Reads how an order travels to the customer.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>A courier or pickup shipment, selected by the <c>kind</c> property.</returns>
    [Get("/orders/{id}/shipment")]
    Task<Shipment> GetShipmentAsync(int id, CancellationToken cancellationToken);
}
