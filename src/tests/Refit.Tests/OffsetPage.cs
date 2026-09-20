// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A page of the paged fixture API addressed by offset and limit.</summary>
public sealed class OffsetPage
{
    /// <summary>Gets the items on the page.</summary>
    public IReadOnlyList<PagedItem> Items { get; init; } = [];

    /// <summary>Gets the total number of items the API holds.</summary>
    public int Total { get; init; }
}
