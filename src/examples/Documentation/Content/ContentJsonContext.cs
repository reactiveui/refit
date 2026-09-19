// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refit.Documentation.Content;

/// <summary>Supplies metadata for every runtime type used by the object converter.</summary>
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(Person))]
internal sealed partial class ContentJsonContext : JsonSerializerContext;
