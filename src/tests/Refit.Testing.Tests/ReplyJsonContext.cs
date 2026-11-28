// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Refit.Testing.Tests;

/// <summary>Uses uppercase wire names so a reply written with default options is distinguishable.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(JsonObject))]
internal sealed partial class ReplyJsonContext : JsonSerializerContext;
