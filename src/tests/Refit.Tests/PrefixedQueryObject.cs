// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A query object whose property declares a <see cref="QueryAttribute"/> prefix and delimiter.</summary>
public class PrefixedQueryObject
{
    /// <summary>Gets or sets a value whose query key is customized by a prefix and delimiter.</summary>
    [Query("-", "dontlog")]
    public string? Password { get; set; }

    /// <summary>Gets or sets a value that uses the default query key.</summary>
    public string? User { get; set; }
}
