// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A sealed query object whose properties carry their own prefix and delimiter.</summary>
public sealed class PrefixedSealedQueryObject
{
    /// <summary>Gets or sets a prefixed scalar keyed as <c>addr-Zip</c>.</summary>
    [Query("-", "addr")]
    public string? Zip { get; set; }

    /// <summary>Gets or sets a prefixed, aliased scalar keyed as <c>addr-cty</c>.</summary>
    [Query("-", "addr")]
    [AliasAs("cty")]
    public string? City { get; set; }
}
