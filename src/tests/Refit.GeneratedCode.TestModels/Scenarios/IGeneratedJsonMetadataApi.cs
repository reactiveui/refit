// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>Exercises generated methods that take JSON metadata as a parameter under the repository analyzer configuration.</summary>
    public interface IGeneratedJsonMetadataApi
    {
        /// <summary>Gets an order and reads the reply with the supplied metadata.</summary>
        /// <param name="id">The order identifier.</param>
        /// <param name="orderInfo">The metadata that describes the order.</param>
        /// <param name="cancellationToken">A token that cancels the call.</param>
        /// <returns>The order.</returns>
        [Get("/orders/{id}")]
        Task<GeneratedOrder> GetOrderAsync(string id, JsonTypeInfo<GeneratedOrder> orderInfo, CancellationToken cancellationToken);

        /// <summary>Writes the body and reads the reply with the supplied metadata.</summary>
        /// <param name="order">The order to write.</param>
        /// <param name="orderInfo">The metadata that describes the order.</param>
        /// <returns>The stored order.</returns>
        [Post("/orders")]
        Task<GeneratedOrder> CreateOrderAsync([Body] GeneratedOrder order, JsonTypeInfo<GeneratedOrder> orderInfo);

        /// <summary>Writes the body with the supplied metadata when it is given.</summary>
        /// <param name="order">The order to write.</param>
        /// <param name="orderInfo">The metadata that describes the order, or <see langword="null"/> to use the serializer's lookup.</param>
        /// <returns>A task that completes when the order is stored.</returns>
        [Put("/orders")]
        Task ReplaceOrderAsync([Body] GeneratedOrder order, JsonTypeInfo<GeneratedOrder>? orderInfo);

        /// <summary>Streams the orders with the supplied metadata.</summary>
        /// <param name="orderInfo">The metadata that describes each order.</param>
        /// <returns>The orders as they arrive.</returns>
        [Get("/orders")]
        IAsyncEnumerable<GeneratedOrder> StreamOrdersAsync(JsonTypeInfo<GeneratedOrder> orderInfo);

        /// <summary>Watches an order as a cold observable.</summary>
        /// <param name="id">The order identifier.</param>
        /// <param name="orderInfo">The metadata that describes the order.</param>
        /// <returns>An observable that yields the order.</returns>
        [Get("/orders/{id}")]
        IObservable<GeneratedOrder> WatchOrder(string id, JsonTypeInfo<GeneratedOrder> orderInfo);
    }
}
