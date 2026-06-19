// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Nested address query object used by the complex query parameter tests.</summary>
public sealed record Address
{
    /// <summary>Gets or sets the postcode value, aliased to <c>Zip</c>.</summary>
    [AliasAs("Zip")]
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1802:Replace the set accessor with init",
        Justification = "Tests mutate this property after construction via myParams.Address.Postcode, so a settable accessor is required.")]
    public int Postcode { get; set; }

    /// <summary>Gets or sets the street value.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1802:Replace the set accessor with init",
        Justification = "Tests mutate this property after construction via myParams.Address.Street, so a settable accessor is required.")]
    public string Street { get; set; } = string.Empty;
}
