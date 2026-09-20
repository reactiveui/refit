// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>One page of Google Cloud Storage objects.</summary>
[System.Diagnostics.DebuggerDisplay("{Items.Count} objects, next = {NextPageToken}")]
public sealed class GcsObjectList
{
    /// <summary>Gets or sets the kind of resource, which is always <c>storage#objects</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the token that requests the next page; absent on the last page.</summary>
    public string? NextPageToken { get; set; }

    /// <summary>Gets or sets the objects on the page.</summary>
    [JsonPropertyName("items")]
    public List<GcsObject> Items { get; set; } = [];
}
