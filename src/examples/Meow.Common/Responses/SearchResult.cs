// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Meow.Responses;

/// <summary>A single cat image search result.</summary>
public class SearchResult
{
    /// <summary>Gets or sets the breeds associated with the image.</summary>
    [JsonPropertyName("breeds")]
    public Breed[]? Breeds { get; set; }

    /// <summary>Gets or sets the image identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Gets or sets the image URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the image width.</summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>Gets or sets the image height.</summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }
}
