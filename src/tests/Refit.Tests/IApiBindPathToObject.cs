// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that binds path segments to properties of a request object.</summary>
public interface IApiBindPathToObject
{
    /// <summary>Gets foo bars binding the request object's properties to the path.</summary>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task GetFooBars(PathBoundObject request);

    /// <summary>Gets foo bars binding the request, with an additional path parameter taking precedence.</summary>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <param name="someProperty">An explicit path parameter that takes precedence.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{someProperty}/bar/{request.someProperty2}")]
    Task GetFooBars(PathBoundObject request, string someProperty);

    /// <summary>Gets foo bars binding a request that also carries a query property.</summary>
    /// <param name="request">The request whose properties bind to the path and query.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task GetFooBars(PathBoundObjectWithQuery request);

    /// <summary>Gets foo bars where the path tokens use different casing from the property names.</summary>
    /// <param name="requestParams">The request whose properties bind to the path.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{Requestparams.SomeProperty}/bar/{requestParams.SoMeProPerty2}")]
    Task GetFooBarsWithDifferentCasing(PathBoundObject requestParams);

    /// <summary>Gets foo bars binding a derived request object.</summary>
    /// <param name="request">The derived request whose properties bind to the path.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{request.someProperty}/bar/{request.someProperty3}")]
    Task GetFooBarsDerived(PathBoundDerivedObject request);

    /// <summary>Gets bars by foo using an explicit id and a path-bound request.</summary>
    /// <param name="id">An explicit path identifier.</param>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{id}/{request.someProperty}/bar/{request.someProperty2}")]
    Task GetBarsByFoo(string id, PathBoundObject request);

    /// <summary>Gets bars by foo binding only the first request property to the path.</summary>
    /// <param name="request">The request whose property binds to the path.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{request.someProperty}/bar")]
    Task GetBarsByFoo(PathBoundObject request);

    /// <summary>Gets bars formatting the query using the property's custom format.</summary>
    /// <param name="request">The request carrying a custom-formatted query property.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo")]
    Task GetBarsWithCustomQueryFormat(PathBoundObjectWithQueryFormat request);

    /// <summary>Gets foos binding a list property to the path.</summary>
    /// <param name="request">The request carrying the list to bind.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos/{request.values}")]
    Task GetFoos(PathBoundList request);

    /// <summary>Gets foos binding a list path parameter directly.</summary>
    /// <param name="values">The values to bind to the path.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foos2/{values}")]
    Task GetFoos2(List<int> values);

    /// <summary>Posts foo bar binding the request to the path with a body.</summary>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <param name="someObject">The request body.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task PostFooBar(PathBoundObject request, [Body] object someObject);

    /// <summary>Posts foo bar binding the request to the path with query parameters.</summary>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <param name="someQueryParams">The query parameter object.</param>
    /// <returns>The raw HTTP response.</returns>
    [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task<HttpResponseMessage> PostFooBar(
        PathBoundObject request,
        [Query] ModelObject someQueryParams);

    /// <summary>Posts a multipart request with a path-bound request, query, and stream part.</summary>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <param name="someQueryParams">The query parameter object.</param>
    /// <param name="stream">The stream part to upload.</param>
    /// <returns>The raw HTTP response.</returns>
    [Multipart]
    [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task<HttpResponseMessage> PostFooBarStreamPart(
        PathBoundObject request,
        [Query] ModelObject someQueryParams,
        StreamPart stream);

    /// <summary>Posts a multipart request with a path-bound request and stream part.</summary>
    /// <param name="request">The request whose properties bind to the path.</param>
    /// <param name="stream">The stream part to upload.</param>
    /// <returns>The raw HTTP response.</returns>
    [Multipart]
    [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task<HttpResponseMessage> PostFooBarStreamPart(PathBoundObject request, StreamPart stream);

    /// <summary>Posts a multipart request with a path-and-query-bound request and stream part.</summary>
    /// <param name="request">The request whose properties bind to the path and query.</param>
    /// <param name="stream">The stream part to upload.</param>
    /// <returns>The raw HTTP response.</returns>
    [Multipart]
    [Post("/foos/{request.someProperty}/bar/{request.someProperty2}")]
    Task<HttpResponseMessage> PostFooBarStreamPart(
        PathBoundObjectWithQuery request,
        StreamPart stream);
}
