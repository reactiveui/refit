// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>A small payload item used by the fast-path serialization benchmark.</summary>
public sealed class FastItem
{
    /// <summary>Gets or sets the item identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the item name.</summary>
    public string Name { get; set; } = string.Empty;
}
