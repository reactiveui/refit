// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Refit.MinimalApi.Tests;

/// <summary>Route values feature used when endpoint delegates are invoked directly.</summary>
internal sealed class TestRouteValuesFeature : IRouteValuesFeature
{
    /// <inheritdoc/>
    public RouteValueDictionary RouteValues { get; set; } = [];
}
