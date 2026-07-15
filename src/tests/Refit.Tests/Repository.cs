// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A path-bound object whose optional property drives an optional <c>{repo.Name?}</c> path segment.</summary>
public sealed class Repository
{
    /// <summary>Gets or sets the required owner segment.</summary>
    public string? Owner { get; set; }

    /// <summary>Gets or sets the optional repository name segment.</summary>
    public string? Name { get; set; }
}
