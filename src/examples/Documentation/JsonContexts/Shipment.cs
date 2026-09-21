// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.JsonContexts;

/// <summary>How an order travels to the customer. The <c>kind</c> property in the JSON selects the derived type.</summary>
/// <param name="Reference">The shop's reference for the shipment.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CourierShipment), "courier")]
internal abstract record Shipment(string Reference);
