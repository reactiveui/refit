// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A nested filter object flattened under a dotted key by <see cref="SystemTextJsonQueryConverter{T}"/>.</summary>
public sealed class StjNested
{
    /// <summary>Gets or sets the city.</summary>
    public string? City { get; set; }
}
