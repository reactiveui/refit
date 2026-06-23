// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Source-generated JSON context used to exercise the streaming and synchronous serializer fast paths.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(StreamItem))]
internal sealed partial class StreamingJsonContext : JsonSerializerContext;
