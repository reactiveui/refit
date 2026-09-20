// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A page of the paged fixture API that names the offset of the following page.</summary>
public sealed class NextOffsetPage
{
    /// <summary>Gets the items on the page.</summary>
    public IReadOnlyList<PagedItem> Items { get; init; } = [];

    /// <summary>Gets the offset of the following page, or <see langword="null"/> on the last page.</summary>
    public int? NextOffset { get; init; }
}
