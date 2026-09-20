// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>A blob listed by Azure Blob Storage.</summary>
[System.Diagnostics.DebuggerDisplay("{Name}")]
public sealed class Blob
{
    /// <summary>Gets or sets the blob name.</summary>
    public string Name { get; set; } = string.Empty;
}
