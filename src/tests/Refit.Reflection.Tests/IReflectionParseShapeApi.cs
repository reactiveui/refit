// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>An interface whose methods span the full attribute surface the reflection request builder parses per method:
/// static headers on the interface and method, an aliased dynamic path segment, a nested-object path chain, scalar and
/// multi-expanded collection queries, a header parameter, a header collection, an authorization parameter, a request
/// property, a serialized body, a multipart upload, an absolute <c>[Url]</c> parameter, a cancellation token and a generic
/// method. It pins the parsed metadata (and the requests built from it) so the per-parameter attribute classification the
/// constructor performs cannot silently regress.</summary>
[Headers("X-Interface: iface")]
public interface IReflectionParseShapeApi
{
    /// <summary>A constant route with no parameters, carrying a method-level static header.</summary>
    /// <returns>The HTTP response message.</returns>
    [Headers("X-Method: method")]
    [Get("/constant")]
    Task<HttpResponseMessage> Constant();

    /// <summary>A dynamic route whose segment is bound by an aliased parameter.</summary>
    /// <param name="key">The identifier bound to the <c>{id}</c> segment through its alias.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/users/{id}")]
    Task<HttpResponseMessage> AliasedSegment([AliasAs("id")] int key);

    /// <summary>A route bound to a nested object property chain, with the remaining property flattened into the query.</summary>
    /// <param name="request">The request object supplying the path chain and query values.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/orgs/{request.Inner.Code}/audit")]
    Task<HttpResponseMessage> NestedPath(ReflectionParseShapeModel request);

    /// <summary>A route carrying several scalar query parameters.</summary>
    /// <param name="q">The query text.</param>
    /// <param name="page">The page number.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/search")]
    Task<HttpResponseMessage> ScalarQuery(string q, int page);

    /// <summary>A route carrying a multi-expanded collection query parameter.</summary>
    /// <param name="tags">The tag identifiers expanded one key per element.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/tags")]
    Task<HttpResponseMessage> CollectionQuery([Query(CollectionFormat.Multi)] int[] tags);

    /// <summary>A route whose parameters each carry a distinct request-shaping attribute, exercising the per-parameter
    /// attribute classification the constructor performs.</summary>
    /// <param name="apiKey">The API key emitted as a header.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="token">The bearer token emitted as the authorization header.</param>
    /// <param name="traceId">The trace identifier stored as a request property.</param>
    /// <param name="filter">A scalar query parameter.</param>
    /// <returns>The HTTP response message.</returns>
    [Headers("X-Method: dense")]
    [Get("/dense")]
    Task<HttpResponseMessage> DenseAttributes(
        [Header("X-Api-Key")] string apiKey,
        [HeaderCollection] IDictionary<string, string> headers,
        [Authorize("Bearer")] string token,
        [Property("trace-id")] string traceId,
        string filter);

    /// <summary>A route that serializes an object argument as the JSON body.</summary>
    /// <param name="payload">The body payload.</param>
    /// <returns>The HTTP response message.</returns>
    [Post("/body")]
    Task<HttpResponseMessage> SerializedBody([Body] BodyPayload payload);

    /// <summary>A multipart route with a text field, a byte-array part and a stream part.</summary>
    /// <param name="title">The text field.</param>
    /// <param name="payload">The byte-array part.</param>
    /// <param name="content">The stream part.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/upload")]
    Task<HttpResponseMessage> Upload(string title, byte[] payload, Stream content);

    /// <summary>A route whose absolute URI is supplied by a <c>[Url]</c> parameter.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("")]
    Task<HttpResponseMessage> AbsoluteUrl([Url] string url);

    /// <summary>A dynamic route that also accepts a cancellation token.</summary>
    /// <param name="id">The identifier bound to the path segment.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/cancelable/{id}")]
    Task<HttpResponseMessage> Cancelable(int id, CancellationToken cancellationToken);

    /// <summary>A generic method whose type argument is closed at call time.</summary>
    /// <typeparam name="T">The probe type used to close the method.</typeparam>
    /// <param name="probe">The probe argument supplying the type parameter.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/items")]
    Task<HttpResponseMessage> TypedItem<T>(T probe);
}
