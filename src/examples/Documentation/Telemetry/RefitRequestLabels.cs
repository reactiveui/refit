// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Reads the low-cardinality labels Refit stores on every request it builds.</summary>
internal static class RefitRequestLabels
{
    /// <summary>Gets the declared interface method name, such as <c>GetPersonAsync</c>.</summary>
    /// <param name="request">The request Refit built.</param>
    /// <returns>The method name, or null for a request Refit did not build.</returns>
    internal static string? MethodName(HttpRequestMessage request) =>
        request.Options.TryGetValue(new HttpRequestOptionsKey<string>(HttpRequestMessageOptions.MethodName), out string? name) ? name : null;

    /// <summary>Gets the unfilled route, such as <c>/people/{id}</c>.</summary>
    /// <param name="request">The request Refit built.</param>
    /// <returns>The route template, or null for a request Refit did not build.</returns>
    internal static string? RouteTemplate(HttpRequestMessage request) =>
        request.Options.TryGetValue(new HttpRequestOptionsKey<string>(HttpRequestMessageOptions.RelativePathTemplate), out string? route) ? route : null;
}
