// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Refit interface stubs used to exercise REST method metadata parsing.</summary>
[Headers("User-Agent: RefitTestClient", "Api-Version: 1")]
public interface IRestMethodInfoTests
{
    /// <summary>Defines a GET route with a deliberately malformed path to test error handling.</summary>
    /// <returns>A task that returns the response body.</returns>
    [Get("@)!@_!($_!@($\\\\|||::::")]
    Task<string> GarbagePath();

    /// <summary>Defines a GET route whose path placeholder has no matching parameter.</summary>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffMissingParameters();

    /// <summary>Defines a GET route that substitutes a single path parameter.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuff(int id);

    /// <summary>Defines a GET route that reuses the same parameter across the path and query.</summary>
    /// <param name="id">The identifier substituted into the path and query.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}?param1={id}&param2={id}")]
    Task<string> FetchSomeStuffWithTheSameId(int id);

    /// <summary>Defines a GET route that references one parameter multiple times in a query value.</summary>
    /// <param name="id">The identifier referenced multiple times in the query value.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar?param=first {id} and second {id}")]
    Task<string> FetchSomeStuffWithTheIdInAParameterMultipleTimes(int id);

    /// <summary>Defines a GET route that round-trips a catch-all path segment.</summary>
    /// <param name="path">The catch-all path segment.</param>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{**path}/{id}")]
    Task<string> FetchSomeStuffWithRoundTrippingParam(string path, int id);

    /// <summary>Defines a GET route round-tripping a non-string catch-all path segment.</summary>
    /// <param name="path">The non-string catch-all path segment.</param>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{**path}/{id}")]
    Task<string> FetchSomeStuffWithNonStringRoundTrippingParam(int path, int id);

    /// <summary>Defines a GET route with a hardcoded query string parameter.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}?baz=bamf")]
    Task<string> FetchSomeStuffWithHardcodedQueryParam(int id);

    /// <summary>Defines a GET route mixing a hardcoded query value with a dynamic one.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="search">The dynamic query value.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}?baz=bamf")]
    Task<string> FetchSomeStuffWithQueryParam(int id, string search);

    /// <summary>Defines a GET route that maps a parameter to a path placeholder via an alias.</summary>
    /// <param name="id">The identifier aliased into the path placeholder.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithAlias([AliasAs("id")] int id);

    /// <summary>Defines a GET route that composes two parameters into a single path segment.</summary>
    /// <param name="width">The width composed into the path segment.</param>
    /// <param name="height">The height composed into the path segment.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{width}x{height}")]
    Task<string> FetchAnImage(int width, int height);

    /// <summary>Defines a GET route that sends an aliased path parameter alongside a request body.</summary>
    /// <param name="id">The identifier aliased into the path.</param>
    /// <param name="theData">The request body data.</param>
    /// <returns>An observable that emits the response body.</returns>
    [Get("/foo/bar/{id}")]
    IObservable<string> FetchSomeStuffWithBody(
        [AliasAs("id")] int id,
        [Body] Dictionary<int, string> theData);

    /// <summary>Defines a POST route that submits a URL-encoded body with an aliased path parameter.</summary>
    /// <param name="id">The identifier aliased into the path.</param>
    /// <param name="theData">The URL-encoded request body data.</param>
    /// <returns>An observable that emits the response body.</returns>
    [Post("/foo/bar/{id}")]
    IObservable<string> PostSomeUrlEncodedStuff(
        [AliasAs("id")] int id,
        [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> theData);

    /// <summary>Defines a GET route that applies an explicit authorization scheme.</summary>
    /// <param name="id">The identifier aliased into the path.</param>
    /// <param name="token">The authorization token.</param>
    /// <returns>An observable that emits the response body.</returns>
    [Get("/foo/bar/{id}")]
    IObservable<string> FetchSomeStuffWithAuthorizationSchemeSpecified(
        [AliasAs("id")] int id,
        [Authorize("Bearer")] string token);

    /// <summary>Defines a GET route carrying hardcoded request headers.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Api-Version: 2", "Accept: application/json")]
    Task<string> FetchSomeStuffWithHardcodedHeaders(int id);

    /// <summary>Defines a GET route that supplies an Authorization header dynamically.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="authorization">The dynamic Authorization header value.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicHeader(
        int id,
        [Header("Authorization")] string authorization);

    /// <summary>Defines a GET route combining a dynamic header, query params and a request property.</summary>
    /// <param name="authorization">The dynamic Authorization header value.</param>
    /// <param name="id">The identifier sent as a query parameter.</param>
    /// <param name="someArray">The array sent as a multi-value query parameter.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo")]
    Task<string> FetchSomeStuffWithDynamicHeaderQueryParamAndArrayQueryParam(
        [Header("Authorization")] string authorization,
        int id,
        [Query(CollectionFormat.Multi)] string[] someArray,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a GET route that merges a dynamic header collection with hardcoded headers.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    [Headers(
        "Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==",
        "Accept: application/json")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a PUT route that sends a body with a dynamic header collection.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="body">The request body.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithCustomHeaderCollection(
        int id,
        [Body] object body,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a POST route that sends a body with a dynamic header collection.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="body">The request body.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithCustomHeaderCollection(
        int id,
        [Body] object body,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a PATCH route that sends a body with a dynamic header collection.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="body">The request body.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Patch("/foo/bar/{id}")]
    Task<string> PatchSomeStuffWithCustomHeaderCollection(
        int id,
        [Body] object body,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a PUT route with a dynamic header collection and no body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithoutBodyAndCustomHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a POST route with a dynamic header collection and no body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithoutBodyAndCustomHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a PATCH route with a dynamic header collection and no body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Patch("/foo/bar/{id}")]
    Task<string> PatchSomeStuffWithoutBodyAndCustomHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a PUT route that infers the body alongside a dynamic header collection.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="inferredBody">The complex parameter inferred as the body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithInferredBodyAndWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        object inferredBody);

    /// <summary>Defines a POST route that infers the body alongside a dynamic header collection.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="inferredBody">The complex parameter inferred as the body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithInferredBodyAndWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        object inferredBody);

    /// <summary>Defines a PATCH route that infers the body alongside a dynamic header collection.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="inferredBody">The complex parameter inferred as the body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Patch("/foo/bar/{id}")]
    Task<string> PatchSomeStuffWithInferredBodyAndWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        object inferredBody);

    /// <summary>Defines a GET route combining a dynamic header collection with an authorize value.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="value">The authorize value.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndAuthorize(
        int id,
        [Authorize] string value,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a POST route combining a dynamic header collection with an authorize value.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="value">The authorize value.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithDynamicHeaderCollectionAndAuthorize(
        int id,
        [Authorize] string value,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a GET route combining a dynamic header collection with a dynamic header.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="value">The dynamic header value.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeader(
        int id,
        [Header("Authorization")] string value,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a POST route combining a dynamic header collection with a dynamic header.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="value">The dynamic header value.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithDynamicHeaderCollectionAndDynamicHeader(
        int id,
        [Header("Authorization")] string value,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a GET route with the header collection declared before a dynamic header.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="value">The dynamic header value.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeaderOrderFlipped(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        [Header("Authorization")] string value);

    /// <summary>Defines a GET route that maps a path member to a custom header with a header collection.</summary>
    /// <param name="id">The identifier mapped to a custom header.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection(
        [Header("X-PathMember")] int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a POST route that maps a path member to a custom header with a header collection.</summary>
    /// <param name="id">The identifier mapped to a custom header.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection(
        [Header("X-PathMember")] int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Defines a GET route that mixes a header collection with an extra query parameter.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="baz">The extra query parameter.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        int baz);

    /// <summary>Defines a POST route that mixes a header collection with an extra query parameter.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="baz">The extra query parameter.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        int baz);

    /// <summary>Defines a GET route declaring two header collections to test duplicate detection.</summary>
    /// <param name="headers">The first dynamic header collection.</param>
    /// <param name="headers2">The second dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar")]
    Task<string> FetchSomeStuffWithDuplicateHeaderCollection(
        [HeaderCollection] IDictionary<string, string> headers,
        [HeaderCollection] IDictionary<string, string> headers2);

    /// <summary>Defines a POST route declaring two header collections to test duplicate detection.</summary>
    /// <param name="headers">The first dynamic header collection.</param>
    /// <param name="headers2">The second dynamic header collection.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar")]
    Task<string> PostSomeStuffWithDuplicateHeaderCollection(
        [HeaderCollection] IDictionary<string, string> headers,
        [HeaderCollection] IDictionary<string, string> headers2);

    /// <summary>Defines a GET route mixing a header collection, query params and a request property.</summary>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="id">The identifier sent as a query parameter.</param>
    /// <param name="someArray">The array sent as a multi-value query parameter.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo")]
    Task<string> FetchSomeStuffWithHeaderCollectionQueryParamAndArrayQueryParam(
        [HeaderCollection] IDictionary<string, string> headers,
        int id,
        [Query(CollectionFormat.Multi)] string[] someArray,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a POST route mixing a header collection, query params and a request property.</summary>
    /// <param name="headers">The dynamic header collection.</param>
    /// <param name="id">The identifier sent as a query parameter.</param>
    /// <param name="someArray">The array sent as a multi-value query parameter.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo")]
    Task<string> PostSomeStuffWithHeaderCollectionQueryParamAndArrayQueryParam(
        [HeaderCollection] IDictionary<string, string> headers,
        int id,
        [Query(CollectionFormat.Multi)] string[] someArray,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a GET route with a header collection of an unsupported parameter type.</summary>
    /// <param name="headers">The header collection declared with an unsupported type.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo")]
    Task<string> FetchSomeStuffWithHeaderCollectionOfUnsupportedType(
        [HeaderCollection] string headers);

    /// <summary>Defines a POST route with a header collection of an unsupported parameter type.</summary>
    /// <param name="headers">The header collection declared with an unsupported type.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo")]
    Task<string> PostSomeStuffWithHeaderCollectionOfUnsupportedType(
        [HeaderCollection] string headers);

    /// <summary>Defines a GET route that attaches a dynamic request property.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a POST route that attaches a dynamic request property alongside a body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="body">The request body.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithDynamicRequestProperty(
        int id,
        [Body] object body,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a POST route that attaches several dynamic request properties.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="body">The request body.</param>
    /// <param name="someValue">The first value sent as a request property.</param>
    /// <param name="someOtherValue">The second value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithDynamicRequestProperties(
        int id,
        [Body] object body,
        [Property("SomeProperty")] object someValue,
        [Property("SomeOtherProperty")] object someOtherValue);

    /// <summary>Defines a PUT route attaching a dynamic request property without a body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithoutBodyAndWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a POST route attaching a dynamic request property without a body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithoutBodyAndWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a PATCH route attaching a dynamic request property without a body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <returns>A task that returns the response body.</returns>
    [Patch("/foo/bar/{id}")]
    Task<string> PatchSomeStuffWithoutBodyAndWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue);

    /// <summary>Defines a PUT route that infers the body alongside a dynamic request property.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <param name="inferredBody">The complex parameter inferred as the body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithInferredBodyAndWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue,
        object inferredBody);

    /// <summary>Defines a POST route that infers the body alongside a dynamic request property.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <param name="inferredBody">The complex parameter inferred as the body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithInferredBodyAndWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue,
        object inferredBody);

    /// <summary>Defines a PATCH route that infers the body alongside a dynamic request property.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property.</param>
    /// <param name="inferredBody">The complex parameter inferred as the body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Patch("/foo/bar/{id}")]
    Task<string> PatchSomeStuffWithInferredBodyAndWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someValue,
        object inferredBody);

    /// <summary>Defines a GET route with two request properties sharing a duplicate key.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue1">The first value sent under the duplicate property key.</param>
    /// <param name="someValue2">The second value sent under the duplicate property key.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey(
        int id,
        [Property("SomeProperty")] object someValue1,
        [Property("SomeProperty")] object someValue2);

    /// <summary>Defines a GET route with request properties that omit an explicit key.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someValue">The value sent as a request property without an explicit key.</param>
    /// <param name="someOtherValue">The value sent as a request property with an empty key.</param>
    /// <returns>A task that returns the response body.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicRequestPropertyWithoutKey(
        int id,
        [Property] object someValue,
        [Property("")] object someOtherValue);

    /// <summary>Defines a POST route that sends a buffered value-type body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="whatever">The buffered value-type request body.</param>
    /// <returns>A task that returns whether the request succeeded.</returns>
    [Post("/foo/{id}")]
    Task<bool> OhYeahValueTypes(int id, [Body(buffered: true)] int whatever);

    /// <summary>Defines a POST route that sends an unbuffered value-type body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="whatever">The unbuffered value-type request body.</param>
    /// <returns>A task that returns whether the request succeeded.</returns>
    [Post("/foo/{id}")]
    Task<bool> OhYeahValueTypesUnbuffered(int id, [Body(buffered: false)] int whatever);

    /// <summary>Defines a POST route that streams a buffered dictionary body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="theData">The buffered dictionary request body.</param>
    /// <returns>A task that returns whether the request succeeded.</returns>
    [Post("/foo/{id}")]
    Task<bool> PullStreamMethod(int id, [Body(buffered: true)] Dictionary<int, string> theData);

    /// <summary>Defines a POST route that returns no value to test void task handling.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo/{id}")]
    Task VoidPost(int id);

    /// <summary>Defines a POST route with a synchronous return type to test invalid signatures.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>The synchronous response body.</returns>
    [Post("/foo/{id}")]
    string AsyncOnlyBuddy(int id);

    /// <summary>Defines a PATCH route that posts a string body and returns an observable.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someAttribute">The string request body.</param>
    /// <returns>An observable that emits the response body.</returns>
    [Patch("/foo/{id}")]
    IObservable<string> PatchSomething(int id, [Body] string someAttribute);

    /// <summary>Defines an OPTIONS route that posts a string body.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="someAttribute">The string request body.</param>
    /// <returns>A task that returns the response body.</returns>
    [Options("/foo/{id}")]
    Task<string> SendOptions(int id, [Body] string someAttribute);

    /// <summary>Defines a POST route that returns a wrapped API response.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns the wrapped API response.</returns>
    [Post("/foo/{id}")]
    Task<ApiResponse<bool>> PostReturnsApiResponse(int id);

    /// <summary>Defines a POST route that returns a plain value rather than an API response.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <returns>A task that returns whether the request succeeded.</returns>
    [Post("/foo/{id}")]
    Task<bool> PostReturnsNonApiResponse(int id);

    /// <summary>Defines a POST route whose complex parameter is inferred as the body.</summary>
    /// <param name="theData">The complex parameter inferred as the body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task PostWithBodyDetected(Dictionary<int, string> theData);

    /// <summary>Defines a GET route whose complex parameter is inferred as the body.</summary>
    /// <param name="theData">The complex parameter inferred as the body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task GetWithBodyDetected(Dictionary<int, string> theData);

    /// <summary>Defines a PUT route whose complex parameter is inferred as the body.</summary>
    /// <param name="theData">The complex parameter inferred as the body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Put("/foo")]
    Task PutWithBodyDetected(Dictionary<int, string> theData);

    /// <summary>Defines a PATCH route whose complex parameter is inferred as the body.</summary>
    /// <param name="theData">The complex parameter inferred as the body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Patch("/foo")]
    Task PatchWithBodyDetected(Dictionary<int, string> theData);

    /// <summary>Defines a POST route with two complex parameters to test ambiguous body detection.</summary>
    /// <param name="theData">The first complex parameter.</param>
    /// <param name="theData1">The second complex parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task TooManyComplexTypes(Dictionary<int, string> theData, Dictionary<int, string> theData1);

    /// <summary>Defines a POST route with multiple complex parameters and an explicit body.</summary>
    /// <param name="theData">The first complex parameter.</param>
    /// <param name="theData1">The complex parameter marked as the body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task ManyComplexTypes(
        Dictionary<int, string> theData,
        [Body] Dictionary<int, string> theData1);

    /// <summary>Defines a POST route that serializes a dictionary as query parameters.</summary>
    /// <param name="theData">The dictionary serialized as query parameters.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task PostWithDictionaryQuery([Query] Dictionary<int, string> theData);

    /// <summary>Defines a POST route that serializes a complex type as query parameters.</summary>
    /// <param name="queryParams">The complex type serialized as query parameters.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task PostWithComplexTypeQuery([Query] ComplexQueryObject queryParams);

    /// <summary>Defines a POST route with an implied complex query type and an explicit body.</summary>
    /// <param name="queryParams">The complex type serialized as query parameters.</param>
    /// <param name="theData1">The complex parameter marked as the body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task ImpliedComplexQueryType(
        ComplexQueryObject queryParams,
        [Body] Dictionary<int, string> theData1);

    /// <summary>Defines a GET route exercising multiple optional query attributes.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="text">The optional text query parameter.</param>
    /// <param name="optionalId">The optional identifier query parameter.</param>
    /// <param name="filters">The optional multi-value filter query parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Refit interface stub intentionally uses optional parameters to exercise default-value handling.")]
    [Get("/api/{id}")]
    Task MultipleQueryAttributes(
        int id,
        [Query] string? text = null,
        [Query] int? optionalId = null,
        [Query(CollectionFormat = CollectionFormat.Multi)] string[]? filters = null);

    /// <summary>Defines a GET route exercising nullable and optional parameter values.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="text">The optional text query parameter.</param>
    /// <param name="optionalId">The optional identifier query parameter.</param>
    /// <param name="filters">The optional multi-value filter query parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Refit interface stub intentionally uses optional parameters to exercise default-value handling.")]
    [Get("/api/{id}")]
    Task NullableValues(
        int id,
        string? text = null,
        int? optionalId = null,
        [Query(CollectionFormat = CollectionFormat.Multi)] string[]? filters = null);

    /// <summary>Defines a GET route with an unsupported enumerable query parameter.</summary>
    /// <param name="values">The unsupported enumerable query parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/api/{id}")]
    Task IEnumerableThrowingError([Query(CollectionFormat.Multi)] IEnumerable<string> values);

    /// <summary>Defines a GET route with an invalid non-task generic return type.</summary>
    /// <returns>The synchronous list response body.</returns>
    [Get("/foo")]
    List<string> InvalidGenericReturnType();
}
