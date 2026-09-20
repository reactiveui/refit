// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>One page of a Jira search, addressed by the offset of its first issue.</summary>
[System.Diagnostics.DebuggerDisplay("{StartAt} of {Total}")]
public sealed class JiraSearchResult
{
    /// <summary>Gets or sets the offset of the first issue on the page.</summary>
    public int StartAt { get; set; }

    /// <summary>Gets or sets the most issues a page can hold.</summary>
    public int MaxResults { get; set; }

    /// <summary>Gets or sets the total number of issues the search matches.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets the issues on the page.</summary>
    [JsonPropertyName("issues")]
    public List<JiraIssue> Issues { get; set; } = [];
}
