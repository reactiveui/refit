// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Benchmarks;

/// <summary>A nested address used by the query-flattening benchmark to exercise the recursive object walk.</summary>
public sealed class QueryAddressModel
{
    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the postal code.</summary>
    public string Zip { get; set; } = string.Empty;
}
