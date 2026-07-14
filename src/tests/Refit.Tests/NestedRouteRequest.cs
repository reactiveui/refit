// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A request object exposing a nested object whose property binds to a route path.</summary>
public sealed class NestedRouteRequest
{
    /// <summary>Gets or sets the nested object supplying the route value.</summary>
    public NestedRouteInner? Inner { get; set; }
}
