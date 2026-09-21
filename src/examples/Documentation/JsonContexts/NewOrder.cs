// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>The request body that places an order.</summary>
/// <param name="Customer">The name of the customer placing the order.</param>
/// <param name="Lines">The products to order.</param>
internal sealed record NewOrder(string Customer, List<OrderLine> Lines);
