// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http.Metrics;

namespace Refit.Documentation;

/// <summary>Adds Refit's route template and method name to HttpClient's own request-duration metric.</summary>
internal sealed class RefitMetricsTagsHandler : DelegatingHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpMetricsEnrichmentContext.AddCallback(request, static context =>
        {
            if (RefitRequestLabels.RouteTemplate(context.Request) is { } route)
            {
                context.AddCustomTag("url.template", route);
            }

            if (RefitRequestLabels.MethodName(context.Request) is { } method)
            {
                context.AddCustomTag("refit.method", method);
            }
        });

        return base.SendAsync(request, cancellationToken);
    }
}
