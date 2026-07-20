// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Benchmarks;

/// <summary>A request model exercised by the reflection query-flattening and object-path benchmarks.</summary>
public sealed class ReflectionQueryModel
{
    /// <summary>Gets or sets the identifier, bound to <c>{request.id}</c> path placeholders.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the display name flattened into the query string.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the page number flattened into the query string.</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the multi-expanded tag collection flattened into the query string.</summary>
    [Query(CollectionFormat.Multi)]
    public int[]? Tags { get; set; }

    /// <summary>Gets or sets the nested model bound to <c>{request.inner.code}</c> path placeholders and flattened recursively.</summary>
    public ReflectionInnerModel? Inner { get; set; }
}
