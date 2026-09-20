// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>One page of Microsoft Graph users, which names the next page with an absolute link.</summary>
[System.Diagnostics.DebuggerDisplay("{Value.Count} users, next = {NextLink}")]
public sealed class GraphUserPage
{
    /// <summary>Gets or sets the users on the page.</summary>
    [JsonPropertyName("value")]
    public List<GraphUser> Value { get; set; } = [];

    /// <summary>Gets or sets the link to the next page; absent on the last page.</summary>
    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}
