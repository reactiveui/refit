// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>A typed DynamoDB attribute value, of which a string and a number are modeled.</summary>
[System.Diagnostics.DebuggerDisplay("S = {S}, N = {N}")]
public sealed class DynamoAttributeValue
{
    /// <summary>Gets or sets the value of a string attribute.</summary>
    [JsonPropertyName("S")]
    public string? S { get; set; }

    /// <summary>Gets or sets the value of a number attribute, which DynamoDB carries as text.</summary>
    [JsonPropertyName("N")]
    public string? N { get; set; }
}
