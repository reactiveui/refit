// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Documentation;

/// <summary>Supplies generated metadata for the naming example. The settings, not the context, choose the naming convention.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ClientNamingInput))]
internal sealed partial class ClientNamingJsonContext : JsonSerializerContext;
