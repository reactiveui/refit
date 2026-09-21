// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A warehouse holding stock.</summary>
/// <param name="Aisle">The aisle number.</param>
internal sealed record WarehouseLocation(int Aisle) : StockLocation
{
    /// <inheritdoc/>
    internal override string Describe() => $"Aisle {Aisle}";
}
