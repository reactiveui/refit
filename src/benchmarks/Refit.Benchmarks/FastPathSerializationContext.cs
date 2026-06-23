// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Benchmarks;

/// <summary>Source-gen context for fast-path serialization (converter-free, NumberHandling-free, Serialization mode).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(List<FastItem>))]
internal sealed partial class FastPathSerializationContext : JsonSerializerContext;
