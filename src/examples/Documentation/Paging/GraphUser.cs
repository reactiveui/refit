// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>A user returned by Microsoft Graph.</summary>
[System.Diagnostics.DebuggerDisplay("{DisplayName}")]
public sealed class GraphUser
{
    /// <summary>Gets or sets the user identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}
