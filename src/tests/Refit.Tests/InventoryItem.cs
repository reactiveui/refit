// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A stock line used to check JSON context registration.</summary>
/// <param name="Id">The identifier of the stock line.</param>
/// <param name="DisplayName">The name shown to users.</param>
/// <param name="OnHand">The number of units in stock.</param>
internal sealed record InventoryItem(int Id, string DisplayName, int OnHand);
