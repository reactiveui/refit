// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Meow.Responses;

/// <summary>Describes the weight of a cat breed in imperial and metric units.</summary>
public class Weight
{
    /// <summary>Gets or sets the weight in imperial units.</summary>
    [JsonPropertyName("imperial")]
    public string? Imperial { get; set; }

    /// <summary>Gets or sets the weight in metric units.</summary>
    [JsonPropertyName("metric")]
    public string? Metric { get; set; }
}
