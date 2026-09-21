// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>An order as the shop returns it.</summary>
/// <param name="Id">The shop's identifier for the order.</param>
/// <param name="Customer">The name of the customer who placed the order.</param>
/// <param name="Status">Where the order is in fulfilment.</param>
/// <param name="Lines">The products on the order.</param>
internal sealed record Order(int Id, string Customer, OrderStatus Status, List<OrderLine> Lines);
