// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Object whose properties bind to route and query parameters.</summary>
public class PathBoundObject
{
    /// <summary>Gets or sets the value bound to a route segment.</summary>
    public string SomeProperty { get; set; } = null!;

    /// <summary>Gets or sets the value bound to a query parameter.</summary>
    [Query]
    public string SomeQuery { get; set; } = null!;
}
