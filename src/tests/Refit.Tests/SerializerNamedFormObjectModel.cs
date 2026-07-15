// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A model whose property field name is resolved by the content serializer.</summary>
public class SerializerNamedFormObjectModel
{
    /// <summary>Gets or sets the identifier whose form field name comes from the JSON property name.</summary>
    [JsonPropertyName("user_id")]
    public string? Id { get; set; }
}
