// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A value-typed query object, which is flattened inline because it can have no derived type.</summary>
public readonly record struct StructQueryObject
{
    /// <summary>Gets the identifier.</summary>
    public int Id { get; init; }

    /// <summary>Gets the tag.</summary>
    public string? Tag { get; init; }
}
