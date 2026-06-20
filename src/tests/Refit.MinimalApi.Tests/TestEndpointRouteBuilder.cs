// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Refit.MinimalApi.Tests;

/// <summary>Endpoint route builder used by tests without constructing a web server host.</summary>
internal sealed class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; } = new EmptyServiceProvider();

    /// <inheritdoc/>
    public ICollection<EndpointDataSource> DataSources { get; } = [];

    /// <inheritdoc/>
    public IApplicationBuilder CreateApplicationBuilder() =>
        new ApplicationBuilder(ServiceProvider);
}
