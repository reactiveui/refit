// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>An object listed by the Google Cloud Storage JSON API.</summary>
[System.Diagnostics.DebuggerDisplay("{Name}: {Size}")]
public sealed class GcsObject
{
    /// <summary>Gets or sets the object name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the object size in bytes, which the API returns as a string.</summary>
    public string Size { get; set; } = string.Empty;
}
