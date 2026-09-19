// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Supplies a single query value instead of a set of flattened properties.</summary>
/// <param name="Value">The text returned by ToString.</param>
internal sealed record SearchText(string Value)
{
    /// <summary>Returns the service's single text value.</summary>
    /// <returns>The search text.</returns>
    public override string ToString() => Value;
}
