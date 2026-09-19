// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BlazorWasmIssue2065;

/// <summary>Possible status values for the sample data.</summary>
[JsonSerializable(typeof(Issue2067Status))]
[JsonSerializable(typeof(Issue2067Response))]
[DebuggerDisplay("JsonContext")]
public partial class JsonContext : JsonSerializerContext;
