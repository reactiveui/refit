// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization.Metadata;

namespace Refit.Documentation.JsonContexts;

/// <summary>Reads and places orders. Each method takes the JSON metadata for its call as a parameter.</summary>
internal interface IOrdersTypeInfoApi
{
    /// <summary>Reads one order with metadata the caller passes for the reply.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="orderInfo">The metadata that describes <c>Order</c>.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The order the shop returns.</returns>
    [Get("/orders/{id}")]
    Task<Order> GetOrderAsync(int id, JsonTypeInfo<Order> orderInfo, CancellationToken cancellationToken);

    /// <summary>Reads one order with its status and headers, and metadata the caller passes for the reply body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="orderInfo">The metadata that describes <c>Order</c>.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The order the shop returns, wrapped with the response details.</returns>
    [Get("/orders/{id}")]
    Task<ApiResponse<Order>> GetOrderResponseAsync(int id, JsonTypeInfo<Order> orderInfo, CancellationToken cancellationToken);

    /// <summary>Reads one order. Without metadata the serializer looks the metadata up.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="orderInfo">The metadata that describes <c>Order</c>, or <see langword="null"/>.</param>
    /// <returns>The order the shop returns.</returns>
    [Get("/orders/{id}")]
    Task<Order> FindOrderAsync(int id, JsonTypeInfo<Order>? orderInfo = null);

    /// <summary>Reads every order with metadata for the whole list.</summary>
    /// <param name="ordersInfo">The metadata that describes <c>List&lt;Order&gt;</c>.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The orders the shop returns.</returns>
    [Get("/orders")]
    Task<List<Order>> ListOrdersAsync(JsonTypeInfo<List<Order>> ordersInfo, CancellationToken cancellationToken);

    /// <summary>Reads the orders one at a time with metadata for each order.</summary>
    /// <param name="orderInfo">The metadata that describes <c>Order</c>.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The orders as the shop sends them.</returns>
    [Get("/orders")]
    IAsyncEnumerable<Order> StreamOrdersAsync(JsonTypeInfo<Order> orderInfo, CancellationToken cancellationToken);

    /// <summary>Places an order with metadata for the request body and for the reply.</summary>
    /// <param name="order">The products and customer, sent as the JSON request body.</param>
    /// <param name="newOrderInfo">The metadata that describes <c>NewOrder</c>.</param>
    /// <param name="orderInfo">The metadata that describes <c>Order</c>.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The order the shop created.</returns>
    [Post("/orders")]
    Task<Order> PlaceOrderAsync([Body] NewOrder order, JsonTypeInfo<NewOrder> newOrderInfo, JsonTypeInfo<Order> orderInfo, CancellationToken cancellationToken);
}
