// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Response DTO whose properties exercise AliasAs vs JsonPropertyName aliasing during deserialization.</summary>
public class TestAliasObject
{
    /// <summary>Gets or sets the value mapped via an <see cref="AliasAsAttribute"/> attribute (which does not affect response deserialization).</summary>
    [AliasAs("FIELD_WE_SHOULD_SHORTEN_WITH_ALIAS_AS")]
    public string? ShortNameForAlias { get; set; }

    /// <summary>Gets or sets the value mapped via the JSON property name attribute (which does affect response deserialization).</summary>
    [JsonPropertyName("FIELD_WE_SHOULD_SHORTEN_WITH_JSON_PROPERTY")]
    public string? ShortNameForJsonProperty { get; set; }
}
