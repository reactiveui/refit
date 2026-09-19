// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Provides simple form values whose descriptors use direct getters.</summary>
internal sealed class FormBody
{
    /// <summary>Gets the count rendered with a three-digit format.</summary>
    public int Count { get; init; } = SampleValues.Count;

    /// <summary>Gets the absent note rendered as an empty field.</summary>
    public string? Note { get; init; }
}
