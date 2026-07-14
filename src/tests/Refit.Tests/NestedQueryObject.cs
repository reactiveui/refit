// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A query object with a nested object property, flattened recursively under a dotted key.</summary>
public sealed class NestedQueryObject
{
    /// <summary>Gets or sets a top-level scalar property.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets a nested object whose properties compose under this property's key.</summary>
    public AddressQuery? Address { get; set; }
}
