// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>One product on an order.</summary>
/// <param name="Sku">The product's stock-keeping unit.</param>
/// <param name="Quantity">How many units the customer ordered.</param>
/// <param name="UnitPrice">The price of one unit.</param>
internal sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);
