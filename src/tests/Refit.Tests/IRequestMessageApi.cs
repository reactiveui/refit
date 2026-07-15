// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>An API whose methods return the fully built <see cref="HttpRequestMessage"/> without sending it.</summary>
public interface IRequestMessageApi
{
    /// <summary>Builds a GET request with a path parameter and a query parameter.</summary>
    /// <param name="id">The user id substituted into the path.</param>
    /// <param name="filter">The value bound to the <c>filter</c> query string.</param>
    /// <returns>The built request message, not sent.</returns>
    [Get("/users/{id}")]
    Task<HttpRequestMessage> GetUserRequest(int id, [Query] string filter);

    /// <summary>Builds a POST request with a serialized body and a dynamic header.</summary>
    /// <param name="payload">The request body.</param>
    /// <param name="trace">The value bound to the <c>X-Trace</c> header.</param>
    /// <returns>The built request message, not sent.</returns>
    [Post("/users")]
    [Headers("User-Agent: RefitRequestBuilder")]
    Task<HttpRequestMessage> CreateUserRequest([Body] BodyPayload payload, [Header("X-Trace")] string trace);

    /// <summary>Builds a GET request while accepting a cancellation token argument.</summary>
    /// <param name="cancellationToken">A cancellation token argument, allowed but unused when only building.</param>
    /// <returns>The built request message, not sent.</returns>
    [Get("/ping")]
    Task<HttpRequestMessage> PingRequest(CancellationToken cancellationToken);
}
