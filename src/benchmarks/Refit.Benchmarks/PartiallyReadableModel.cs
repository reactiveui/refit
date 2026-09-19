// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Runtime.CompilerServices;

namespace Refit.Benchmarks;

/// <summary>A model with a mix of readable and write-only properties, exercising the reflection property filter's
/// slow path where the readable count differs from the total property count.</summary>
[System.Diagnostics.DebuggerDisplay("{Id}")]
public sealed class PartiallyReadableModel
{
    /// <summary>Gets or sets a readable identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets a readable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value whose getter is non-public, so the readable-property filter skips it.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1044",
        Justification = "The filtered-path benchmark requires a public property with a non-public getter; making it readable removes the scenario being measured.")]
    public string Hidden { private get; set; } = string.Empty;

    /// <summary>Reads the hidden value, keeping the non-public getter observable.</summary>
    /// <returns>The current hidden value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string PeekHidden() => Hidden;
}
