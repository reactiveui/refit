// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation;

/// <summary>Provides generated metadata for Page of Person when resolver contexts are combined.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Page<Person>))]
internal sealed partial class PageJsonContext : JsonSerializerContext;
