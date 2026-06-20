// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Nested address query object used by the complex query parameter tests.</summary>
public sealed record Address
{
    /// <summary>Gets the postcode value, aliased to <c>Zip</c>.</summary>
    [AliasAs("Zip")]
    public int Postcode { get; init; }

    /// <summary>Gets the street value.</summary>
    public string Street { get; init; } = string.Empty;
}
