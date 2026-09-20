// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An item returned by the paged fixture API.</summary>
public sealed class PagedItem
{
    /// <summary>Gets the item identifier.</summary>
    public int Id { get; init; }

    /// <summary>Gets the item name.</summary>
    public string Name { get; init; } = string.Empty;
}
