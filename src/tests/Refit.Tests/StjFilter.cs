// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A filter flattened by <see cref="SystemTextJsonQueryConverter{T}"/> in the converter tests.</summary>
public sealed class StjFilter
{
    /// <summary>Gets or sets a value renamed by System.Text.Json.</summary>
    [JsonPropertyName("q")]
    public string? Query { get; set; }

    /// <summary>Gets or sets a numeric value.</summary>
    public int Count { get; set; }

    /// <summary>Gets or sets a nested object flattened under a dotted key.</summary>
    public StjNested? Sub { get; set; }
}
