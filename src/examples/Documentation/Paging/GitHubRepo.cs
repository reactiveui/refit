// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>A repository returned by the GitHub REST API.</summary>
[System.Diagnostics.DebuggerDisplay("{FullName}")]
public sealed class GitHubRepo
{
    /// <summary>Gets or sets the repository identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the owner-qualified repository name.</summary>
    public string FullName { get; set; } = string.Empty;
}
