// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace LibraryWithSDKandRefitService;

/// <summary>Defines the JSON contract shared by the local API and its client.</summary>
[System.Diagnostics.DebuggerDisplay("Local API JSON metadata")]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ModelForTest))]
public sealed partial class LocalApiJsonContext : JsonSerializerContext;
