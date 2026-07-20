// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>A nested query model flattened recursively under a dotted key.</summary>
public sealed class ReflectionCachingInnerModel
{
    /// <summary>Gets or sets the nested code.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the nested label.</summary>
    public string? Label { get; set; }
}
