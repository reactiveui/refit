// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Documentation.JsonContexts;

/// <summary>Metadata that describes <c>Order</c> and nothing else, so <c>NewOrder</c> has no metadata.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Order))]
internal sealed partial class OrderReadsJsonContext : JsonSerializerContext;
