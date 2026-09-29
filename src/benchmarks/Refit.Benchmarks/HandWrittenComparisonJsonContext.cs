// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Benchmarks;

/// <summary>The JSON metadata shared by the generated Refit client and the hand-written client it is compared with.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(FastItem))]
[JsonSerializable(typeof(List<FastItem>))]
internal sealed partial class HandWrittenComparisonJsonContext : JsonSerializerContext;
