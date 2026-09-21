// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.JsonContexts;

/// <summary>Where an order is in the shop's fulfilment process.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
internal enum OrderStatus
{
    /// <summary>The shop has accepted the order and has not sent it.</summary>
    Pending = 0,

    /// <summary>The order has left the warehouse.</summary>
    Shipped = 1,

    /// <summary>The customer has received the order.</summary>
    Delivered = 2,
}
