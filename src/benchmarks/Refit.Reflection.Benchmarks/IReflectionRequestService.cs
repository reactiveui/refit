// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// A representative Refit interface exercising the full binding surface of the reflection request builder:
/// constant and dynamic routes, object and nested-object path binding, scalar/collection/object query parameters,
/// static and per-parameter headers, a header collection, an authorization parameter, a request property, a JSON
/// body, a multipart upload, a generic method, an absolute <c>[Url]</c> parameter, and a cancellation token.
/// </summary>
[Headers("User-Agent: RefitBench")]
public interface IReflectionRequestService
{
    /// <summary>A request against a constant route with no parameters.</summary>
    /// <returns>The HTTP response message.</returns>
    [Get("/users")]
    Task<HttpResponseMessage> ConstantRouteAsync();

    /// <summary>A request against a route with one dynamic path segment.</summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/users/{id}")]
    Task<HttpResponseMessage> UserByIdAsync(int id);

    /// <summary>A request against a route with several dynamic path segments.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="group">The group name.</param>
    /// <param name="status">The status.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/users/{id}/{group}/{status}")]
    Task<HttpResponseMessage> MultiSegmentAsync(int id, string group, string status);

    /// <summary>A request with several scalar query parameters.</summary>
    /// <param name="q">The query text.</param>
    /// <param name="page">The page number.</param>
    /// <param name="includeArchived">Whether archived entries are included.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/search")]
    Task<HttpResponseMessage> ScalarQueryAsync(string q, int page, bool includeArchived);

    /// <summary>A request with a multi-expanded collection query parameter.</summary>
    /// <param name="tags">The tag identifiers.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/search/tags")]
    Task<HttpResponseMessage> CollectionQueryAsync([Query(CollectionFormat.Multi)] int[] tags);

    /// <summary>A request whose object argument is flattened into the query string.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/list")]
    Task<HttpResponseMessage> ObjectQueryAsync(ReflectionQueryModel query);

    /// <summary>A request whose route is bound to an object property.</summary>
    /// <param name="request">The request object.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/users/{request.id}/detail")]
    Task<HttpResponseMessage> ObjectPathAsync(ReflectionQueryModel request);

    /// <summary>A request whose route is bound to a nested object property chain.</summary>
    /// <param name="request">The request object.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/orgs/{request.inner.code}/audit")]
    Task<HttpResponseMessage> NestedPathAsync(ReflectionQueryModel request);

    /// <summary>A request with a static method header, a header parameter, a header collection and an authorization parameter.</summary>
    /// <param name="apiKey">The API key header value.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="token">The bearer token.</param>
    /// <returns>The HTTP response message.</returns>
    [Headers("X-Feature: on")]
    [Get("/traced")]
    Task<HttpResponseMessage> HeaderAsync(
        [Header("X-Api-Key")] string apiKey,
        [HeaderCollection] IDictionary<string, string> headers,
        [Authorize("Bearer")] string token);

    /// <summary>A request that carries a request property alongside a dynamic route.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="traceId">The trace identifier stored as a request property.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/props/{id}")]
    Task<HttpResponseMessage> PropertyAsync(int id, [Property("trace-id")] string traceId);

    /// <summary>A request that serializes an object argument as the JSON body.</summary>
    /// <param name="user">The user body.</param>
    /// <returns>The HTTP response message.</returns>
    [Post("/users")]
    Task<HttpResponseMessage> CreateUserAsync([Body] ReflectionUserModel user);

    /// <summary>A multipart request with a text field, a byte array and a stream.</summary>
    /// <param name="title">The title text field.</param>
    /// <param name="payload">The byte-array part.</param>
    /// <param name="content">The stream part.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/upload")]
    Task<HttpResponseMessage> UploadAsync(string title, byte[] payload, Stream content);

    /// <summary>A generic method whose type argument is closed at call time.</summary>
    /// <typeparam name="T">The probe type used to close the method.</typeparam>
    /// <param name="probe">The probe argument supplying the type parameter.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/items")]
    Task<HttpResponseMessage> TypedItemAsync<T>(T probe);

    /// <summary>A request whose absolute URI is supplied by a <c>[Url]</c> parameter, with an extra query parameter.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="page">The page number appended as a query parameter.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("")]
    Task<HttpResponseMessage> AbsoluteUrlAsync([Url] string url, int page);

    /// <summary>A request against a dynamic route that also accepts a cancellation token.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/cancelable/{id}")]
    Task<HttpResponseMessage> CancelableAsync(int id, CancellationToken cancellationToken);
}
