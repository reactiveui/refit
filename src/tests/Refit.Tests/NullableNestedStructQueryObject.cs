// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A query object with a nullable nested value-type property, flattened inline through <c>.Value</c>.</summary>
public sealed class NullableNestedStructQueryObject
{
    /// <summary>Gets or sets a top-level scalar property.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets a nullable nested value type; when non-null its properties compose under this key.</summary>
    public GeoPoint? Location { get; set; }
}
