// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A query object whose keys exercise the serializer-aware naming precedence for flattened properties.</summary>
public sealed class JsonNamedQueryObject
{
    /// <summary>Gets or sets a property named by <see cref="JsonPropertyNameAttribute"/>.</summary>
    [JsonPropertyName("json_name")]
    public string? Named { get; set; }

    /// <summary>Gets or sets a property carrying both attributes; the <see cref="AliasAsAttribute"/> name must win.</summary>
    [AliasAs("alias_wins")]
    [JsonPropertyName("json_loses")]
    public string? Both { get; set; }

    /// <summary>Gets or sets a property with no naming attribute, which passes through the key formatter.</summary>
    public string? Plain { get; set; }
}
