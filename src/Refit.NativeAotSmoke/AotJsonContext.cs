// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.NativeAotSmoke;

/// <summary>The source-generated JSON serializer context for the native AOT smoke test.</summary>
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(ServiceStatus))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
internal sealed partial class AotJsonContext : JsonSerializerContext;
