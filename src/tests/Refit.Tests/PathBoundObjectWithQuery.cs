// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A request fixture combining path-bound properties with a query property.</summary>
public class PathBoundObjectWithQuery
{
    /// <summary>Gets the first property bound to the path.</summary>
    public int SomeProperty { get; init; }

    /// <summary>Gets the second property bound to the path.</summary>
    public string? SomeProperty2 { get; init; }

    /// <summary>Gets the value emitted as a query parameter.</summary>
    [Query]
    public string? SomeQuery { get; init; }
}
