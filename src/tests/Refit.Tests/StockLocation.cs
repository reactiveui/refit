// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Where stock is held. Polymorphism is registered by a contract modifier, not by attributes.</summary>
internal abstract record StockLocation
{
    /// <summary>Describes the location for people.</summary>
    /// <returns>The description.</returns>
    internal abstract string Describe();
}
