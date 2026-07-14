// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A nested value type whose scalar properties flatten under the enclosing property's key.</summary>
/// <param name="Lat">The latitude.</param>
/// <param name="Lng">The longitude.</param>
public readonly record struct GeoPoint(double Lat, double Lng);
