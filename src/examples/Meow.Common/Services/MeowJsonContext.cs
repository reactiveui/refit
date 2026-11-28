// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;
using Meow.Responses;

namespace Meow;

/// <summary>Describes the HTTP bodies written by the local handler and read by the cat search client.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CustomerEchoResponse))]
[JsonSerializable(typeof(LargePayloadResponse))]
[JsonSerializable(typeof(IEnumerable<SearchResult>))]
internal sealed partial class MeowJsonContext : JsonSerializerContext;
