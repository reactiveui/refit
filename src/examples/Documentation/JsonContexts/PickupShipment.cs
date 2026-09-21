// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>A shipment the customer collects from a shop. Only the app's own serializer settings map it to the <c>pickup</c> kind.</summary>
/// <param name="Reference">The shop's reference for the shipment.</param>
/// <param name="Store">The name of the shop that holds the order.</param>
internal sealed record PickupShipment(string Reference, string Store) : Shipment(Reference);
