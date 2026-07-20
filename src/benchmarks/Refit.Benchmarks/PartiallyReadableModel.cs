// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Benchmarks;

/// <summary>A model with a mix of readable and write-only properties, exercising the reflection property filter's
/// slow path where the readable count differs from the total property count.</summary>
public sealed class PartiallyReadableModel
{
    /// <summary>Gets or sets a readable identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets a readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value whose getter is non-public, so the readable-property filter skips it.</summary>
    public string Hidden { private get; set; } = string.Empty;

    /// <summary>Reads the hidden value, keeping the non-public getter observable.</summary>
    /// <returns>The current hidden value.</returns>
    public string PeekHidden() => Hidden;
}
