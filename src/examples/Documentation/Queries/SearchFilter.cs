// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Supplies aliases, number formatting and an explicitly serialized null query field.</summary>
internal sealed class SearchFilter
{
    /// <summary>Gets the search text emitted under the aliased name field.</summary>
    [AliasAs("name")]
    public string Term { get; init; } = "Ada Lovelace";

    /// <summary>Gets the result limit whose query name follows the configured key formatter.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Gets the price emitted with exactly two decimal places.</summary>
    [Query(Format = "0.00")]
    public decimal Price { get; init; } = 5;

    /// <summary>Gets the optional note emitted as an empty query field when null.</summary>
    [Query(SerializeNull = true)]
    public string? Note { get; init; }
}
