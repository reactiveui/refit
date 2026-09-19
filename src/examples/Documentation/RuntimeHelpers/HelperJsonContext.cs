// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Supplies compile-time JSON metadata for every serialized demonstration type.</summary>
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(FormBody))]
internal sealed partial class HelperJsonContext : JsonSerializerContext;
