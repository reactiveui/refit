// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>An issue returned by a Jira search.</summary>
[System.Diagnostics.DebuggerDisplay("{Key}: {Summary}")]
public sealed class JiraIssue
{
    /// <summary>Gets or sets the issue key, such as <c>OPS-12</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the issue summary.</summary>
    public string Summary { get; set; } = string.Empty;
}
