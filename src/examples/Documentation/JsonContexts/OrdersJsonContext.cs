// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Documentation.JsonContexts;

/// <summary>Generated JSON metadata for every request and reply type of the orders API.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(NewOrder))]
[JsonSerializable(typeof(Shipment))]
[JsonSerializable(typeof(PickupShipment))]
internal sealed partial class OrdersJsonContext : JsonSerializerContext;
