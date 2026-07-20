// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Benchmarks;

/// <summary>A nested request model exercised by the nested-property path binding and recursive query-flattening benchmarks.</summary>
public sealed class ReflectionInnerModel
{
    /// <summary>Gets or sets the code, bound to <c>{request.inner.code}</c> path placeholders.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the label flattened into the query string.</summary>
    public string? Label { get; set; }
}
