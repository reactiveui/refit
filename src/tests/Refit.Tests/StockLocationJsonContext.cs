// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Source-generated JSON metadata for <see cref="StockLocation"/> and its derived type.</summary>
[JsonSerializable(typeof(StockLocation))]
[JsonSerializable(typeof(WarehouseLocation))]
internal sealed partial class StockLocationJsonContext : JsonSerializerContext;
