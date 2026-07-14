// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A query object whose properties are collections of simple elements, flattened inline by the generator.</summary>
public sealed class CollectionPropertyQueryObject
{
    /// <summary>Gets or sets an integer collection joined with the default (CSV) collection format.</summary>
    public int[]? Ids { get; init; }

    /// <summary>Gets or sets an enum collection rendered one <c>key=value</c> pair per element.</summary>
    [Query(CollectionFormat.Multi)]
    public IReadOnlyList<QuerySort>? Tags { get; set; }

    /// <summary>Gets or sets an aliased string collection joined with the default (CSV) collection format.</summary>
    [AliasAs("n")]
    public string[]? Names { get; set; }
}
