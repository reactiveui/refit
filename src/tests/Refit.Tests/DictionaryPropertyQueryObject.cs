// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A query object with dictionary properties, whose entries expand under each property's key.</summary>
public sealed class DictionaryPropertyQueryObject
{
    /// <summary>Gets or sets a top-level scalar property.</summary>
    public string? Name { get; set; }

    /// <summary>Gets a string-valued dictionary; its entries expand under this property's key.</summary>
    public IDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

    /// <summary>Gets an integer-valued dictionary, exercising the fast-format value path.</summary>
    public Dictionary<string, int> Counts { get; } = new();
}
