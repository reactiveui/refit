// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>An API mixing a plain query parameter, a request-property-only parameter (excluded from the query) and a
/// parameter carrying both <see cref="PropertyAttribute"/> and <see cref="QueryAttribute"/> (kept in the query), pinning the
/// reflection builder's per-parameter query-attribute classification.</summary>
public interface IReflectionPropertyQueryApi
{
    /// <summary>Sends a request whose parameters exercise each query/property classification branch.</summary>
    /// <param name="q">A plain query parameter.</param>
    /// <param name="trace">A request-property-only parameter that must not appear in the query.</param>
    /// <param name="tag">A parameter that is both a request property and a query parameter.</param>
    /// <returns>The response body.</returns>
    [Get("/search")]
    Task<string> Search(
        string q,
        [Property("trace-id")] string trace,
        [Property("tag-prop")][Query] string tag);
}
