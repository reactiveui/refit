// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.JsonContexts;

/// <summary>A shipment a courier delivers to the customer's door.</summary>
/// <param name="Reference">The shop's reference for the shipment.</param>
/// <param name="TrackingNumber">The courier's tracking number.</param>
internal sealed record CourierShipment(string Reference, string TrackingNumber) : Shipment(Reference);
