// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.JsonContexts;

/// <summary>The orders metadata without <c>JsonSourceGenerationOptions</c>, so System.Text.Json's own defaults apply.</summary>
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<Order>))]
[JsonSerializable(typeof(NewOrder))]
[JsonSerializable(typeof(Shipment))]
internal sealed partial class UnconfiguredOrdersJsonContext : JsonSerializerContext;
