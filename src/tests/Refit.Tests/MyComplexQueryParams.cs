// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;

namespace Refit.Tests;

/// <summary>Complex query parameter object with nested and collection members for query expansion tests.</summary>
public class MyComplexQueryParams
{
    /// <summary>Gets or sets the first name query value.</summary>
    public required string FirstName { get; init; }

    /// <summary>Gets or sets the last name query value.</summary>
    public required string LastName { get; init; }

    /// <summary>Gets the nested address query object, aliased to <c>Addr</c>.</summary>
    [AliasAs("Addr")]
    public Address Address { get; } = new();

    /// <summary>Gets the arbitrary metadata expanded into prefixed query values.</summary>
    public Dictionary<string, object> MetaData { get; } = [];

    /// <summary>Gets the loosely typed values expanded into repeated query values.</summary>
    public List<object> Other { get; } = [];
}
