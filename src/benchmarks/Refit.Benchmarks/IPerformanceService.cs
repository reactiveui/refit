// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Refit service interface used by the benchmarks.</summary>
public interface IPerformanceService
{
    /// <summary>Performs a request against a constant route.</summary>
    /// <returns>The HTTP response.</returns>
    [Get("/users")]
    Task<HttpResponseMessage> ConstantRouteAsync();

    /// <summary>Performs a request against a route with one dynamic segment.</summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The HTTP response.</returns>
    [Get("/users/{id}")]
    Task<HttpResponseMessage> DynamicRouteAsync(int id);

    /// <summary>Performs a request against a route with several dynamic segments.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="user">The user name.</param>
    /// <param name="status">The user status.</param>
    /// <returns>The HTTP response.</returns>
    [Get("/users/{id}/{user}/{status}")]
    Task<HttpResponseMessage> ComplexDynamicRouteAsync(int id, string user, string status);

    /// <summary>Performs a request whose route is bound to an object property.</summary>
    /// <param name="request">The request object.</param>
    /// <returns>The HTTP response.</returns>
    [Get("/users/{request.someProperty}")]
    Task<HttpResponseMessage> ObjectRequestAsync(PathBoundObject request);

    /// <summary>Performs a POST request combining dynamic segments, an object and queries.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="request">The request object.</param>
    /// <param name="queries">The query values.</param>
    /// <returns>The HTTP response.</returns>
    [Post("/users/{id}/{request.someProperty}")]
    [Headers("User-Agent: Awesome Octocat App", "X-Emoji: :smile_cat:")]
    Task<HttpResponseMessage> ComplexRequestAsync(
        int id,
        PathBoundObject request,
        [Query(CollectionFormat.Multi)] int[] queries);
}
