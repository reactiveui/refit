// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A deserialization fixture mirroring the npmjs registry document used by the Refit tests.</summary>
public class RootObject
{
    /// <summary>Gets or sets the document identifier.</summary>
    /// <remarks>The registry names this field <c>_id</c>, which case-insensitive matching does not reach from
    /// <see cref="Id"/>, so the mapping is stated explicitly.</remarks>
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    /// <summary>Gets or sets the document revision.</summary>
    /// <remarks>Mapped explicitly for the same reason as <see cref="Id"/>.</remarks>
    [JsonPropertyName("_rev")]
    public string? Rev { get; set; }

    /// <summary>Gets or sets the package name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
