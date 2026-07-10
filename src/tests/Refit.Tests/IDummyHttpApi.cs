// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>A broad Refit fixture interface exercising routing, headers, queries, and body handling.</summary>
[Headers("User-Agent: RefitTestClient", "Api-Version: 1")]
public interface IDummyHttpApi
{
    /// <summary>Gets foo while passing a culture as a query parameter.</summary>
    /// <param name="culture">The culture supplied as a query parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task QueryWithCultureInfo([Query] System.Globalization.CultureInfo culture);

    /// <summary>Gets a string by id and returns the full API response with metadata.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the API response with metadata.</returns>
    [Get("/foo/bar/{id}")]
    Task<ApiResponse<string>> FetchSomeStringWithMetadata(int id);

    /// <summary>Gets some stuff by id.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuff(int id);

    /// <summary>Gets some stuff using a round-tripping catch-all path segment plus an id.</summary>
    /// <param name="path">The round-tripping catch-all path segment.</param>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{**path}/{id}")]
    Task<string> FetchSomeStuffWithRoundTrippingParam(string path, int id);

    /// <summary>Gets some stuff by id with a hardcoded query parameter in the route.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}?baz=bamf")]
    Task<string> FetchSomeStuffWithHardcodedQueryParameter(int id);

    /// <summary>Gets some stuff by id combining a hardcoded query parameter with an aliased search query.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="searchQuery">The aliased search query value.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}?baz=bamf")]
    Task<string> FetchSomeStuffWithHardcodedAndOtherQueryParameters(
        int id,
        [AliasAs("search_for")] string searchQuery);

    /// <summary>Gets something where multiple parameters appear within a single URL segment.</summary>
    /// <param name="id">The identifier appearing in the segment.</param>
    /// <param name="width">The width value appearing in the segment.</param>
    /// <param name="height">The height value appearing in the segment.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/{id}/{width}x{height}/foo")]
    Task<string> FetchSomethingWithMultipleParametersPerSegment(int id, int width, int height);

    /// <summary>Gets some stuff by id with hardcoded version and accept headers.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Api-Version: 2", "Accept: application/json")]
    Task<string> FetchSomeStuffWithHardcodedHeaders(int id);

    /// <summary>Gets some stuff by id with a hardcoded header that has no value.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Api-Version")]
    Task<string> FetchSomeStuffWithNullHardcodedHeader(int id);

    /// <summary>Gets some stuff by id with a hardcoded header that has an empty value.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Api-Version: ")]
    Task<string> FetchSomeStuffWithEmptyHardcodedHeader(int id);

    /// <summary>Gets some stuff where the same id is substituted into the URL more than once.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}?param1={id}&param2={id}")]
    Task<string> FetchSomeStuffWithTheSameId(int id);

    /// <summary>Gets some stuff where the id appears multiple times within one query parameter value.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar?param=first {id} and second {id}")]
    Task<string> FetchSomeStuffWithTheIdInAParameterMultipleTimes(int id);

    /// <summary>Gets some stuff where the id is embedded inside double quotes in the URL.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo?q=app_metadata.id:\"{id}\"")]
    Task<string> FetchSomeStuffWithDoubleQuotesInUrl(int id);

    /// <summary>Gets some stuff where the id is wrapped in literal parentheses in the route.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/({id})")]
    Task<string> GetWithTrainingParenthesis(int id);

    /// <summary>Gets some stuff by id where the route ends with a trailing slash.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}/")]
    Task<string> GetWithTrailingSlash(int id);

    /// <summary>Posts content for an id with a hardcoded content-type header.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="content">The body content to post.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    [Headers("Content-Type: literally/anything")]
    Task<string> PostSomeStuffWithHardCodedContentTypeHeader(int id, [Body] string content);

    /// <summary>Posts content for an id with a non-canonical content-type header casing.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="content">The body content to post.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    [Headers("Content-type: application/soap+xml")]
    Task<string> PostSomeStuffWithNonCanonicalContentTypeHeader(int id, [Body] string content);

    /// <summary>Gets some stuff by id where a parameter is both a request property and a query value.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="someValue">The value used as both a request property and a query parameter.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithPropertyAndQuery(
        int id,
        [Property("SomeProperty")][Query] string someValue);

    /// <summary>Gets some stuff by id with an authorization header supplied dynamically.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="authorization">The authorization header value supplied dynamically.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
    Task<string> FetchSomeStuffWithDynamicHeader(
        int id,
        [Header("Authorization")] string authorization);

    /// <summary>Gets some stuff by id with a custom emoji header supplied dynamically.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="custom">The custom emoji header value.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithCustomHeader(int id, [Header("X-Emoji")] string custom);

    /// <summary>Gets some stuff where the id is passed via a custom header rather than the route.</summary>
    /// <param name="id">The identifier passed via a custom header.</param>
    /// <param name="custom">The custom emoji header value.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithPathMemberInCustomHeader(
        [Header("X-PathMember")] int id,
        [Header("X-Emoji")] string custom);

    /// <summary>Posts a body for an id together with a custom emoji header.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="body">The body content to post.</param>
    /// <param name="emoji">The custom emoji header value.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithCustomHeader(
        int id,
        [Body] object body,
        [Header("X-Emoji")] string emoji);

    /// <summary>Gets some stuff by id with a dynamic collection of headers.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers(
        "Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==",
        "Accept: application/json")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Deletes some stuff by id with a dynamic collection of headers.</summary>
    /// <param name="id">The identifier of the resource to delete.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Delete("/foo/bar/{id}")]
    [Headers(
        "Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==",
        "Accept: application/json")]
    Task<string> DeleteSomeStuffWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Puts some stuff by id with a dynamic collection of headers.</summary>
    /// <param name="id">The identifier of the resource to put to.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Put("/foo/bar/{id}")]
    [Headers(
        "Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==",
        "Accept: application/json")]
    Task<string> PutSomeStuffWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Posts some stuff by id with a dynamic collection of headers.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    [Headers(
        "Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==",
        "Accept: application/json")]
    Task<string> PostSomeStuffWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Patches some stuff by id with a dynamic collection of headers.</summary>
    /// <param name="id">The identifier of the resource to patch.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Patch("/foo/bar/{id}")]
    [Headers(
        "Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==",
        "Accept: application/json")]
    Task<string> PatchSomeStuffWithDynamicHeaderCollection(
        int id,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Gets some stuff by id with both a dynamic header collection and an explicit dynamic header.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="value">The explicit dynamic authorization header value.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeader(
        int id,
        [Header("Authorization")] string value,
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Gets some stuff by id with a dynamic header collection and explicit header in flipped order.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="headers">The dynamic collection of headers.</param>
    /// <param name="value">The explicit dynamic authorization header value.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    [Headers("Authorization: SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==")]
    Task<string> FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeaderOrderFlipped(
        int id,
        [HeaderCollection] IDictionary<string, string> headers,
        [Header("Authorization")] string value);

    /// <summary>Gets some stuff by id while attaching a dynamic request property.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="someProperty">The dynamic request property value.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someProperty);

    /// <summary>Deletes some stuff by id while attaching a dynamic request property.</summary>
    /// <param name="id">The identifier of the resource to delete.</param>
    /// <param name="someProperty">The dynamic request property value.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Delete("/foo/bar/{id}")]
    Task<string> DeleteSomeStuffWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someProperty);

    /// <summary>Puts some stuff by id while attaching a dynamic request property.</summary>
    /// <param name="id">The identifier of the resource to put to.</param>
    /// <param name="someProperty">The dynamic request property value.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someProperty);

    /// <summary>Posts some stuff by id while attaching a dynamic request property.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="someProperty">The dynamic request property value.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeStuffWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someProperty);

    /// <summary>Patches some stuff by id while attaching a dynamic request property.</summary>
    /// <param name="id">The identifier of the resource to patch.</param>
    /// <param name="someProperty">The dynamic request property value.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Patch("/foo/bar/{id}")]
    Task<string> PatchSomeStuffWithDynamicRequestProperty(
        int id,
        [Property("SomeProperty")] object someProperty);

    /// <summary>Gets some stuff by id with two request properties sharing a duplicate key.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="someValue1">The first request property value sharing the duplicate key.</param>
    /// <param name="someValue2">The second request property value sharing the duplicate key.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey(
        int id,
        [Property("SomeProperty")] object someValue1,
        [Property("SomeProperty")] object someValue2);

    /// <summary>Gets some stuff by id with request properties that omit explicit keys.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="someValue">The request property value with an inferred key.</param>
    /// <param name="someOtherValue">The request property value with an empty key.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithDynamicRequestPropertyWithoutKey(
        int id,
        [Property] object someValue,
        [Property("")] object someOtherValue);

    /// <summary>Gets a string from a relative path without a full URL.</summary>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/string")]
    Task<string> FetchSomeStuffWithoutFullPath();

    /// <summary>Gets the void endpoint returning no content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/void")]
    Task FetchSomeStuffWithVoid();

    /// <summary>Gets the void endpoint with aliased query parameters.</summary>
    /// <param name="id">The identifier appearing in the route.</param>
    /// <param name="valueA">The aliased query value mapped to 'a'.</param>
    /// <param name="valueB">The aliased query value mapped to 'b'.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/void/{id}/path")]
    Task FetchSomeStuffWithVoidAndQueryAlias(
        string id,
        [AliasAs("a")] string valueA,
        [AliasAs("b")] string valueB);

    /// <summary>Gets foo with non-formattable query parameter types such as bool and char.</summary>
    /// <param name="b">The boolean query value.</param>
    /// <param name="c">The character query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task FetchSomeStuffWithNonFormattableQueryParams(bool b, char c);

    /// <summary>Posts URL-encoded content for an id.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="content">The URL-encoded body content.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeUrlEncodedStuff(
        int id,
        [Body(BodySerializationMethod.UrlEncoded)] object content);

    /// <summary>Posts aliased URL-encoded request data for an id.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="content">The aliased URL-encoded request data.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Post("/foo/bar/{id}")]
    Task<string> PostSomeAliasedUrlEncodedStuff(
        int id,
        [Body(BodySerializationMethod.UrlEncoded)] SomeRequestData content);

    /// <summary>An intentional non-Refit member used to verify generator diagnostics.</summary>
    /// <returns>An arbitrary string.</returns>
    [SuppressMessage("Refit", "RF001", Justification = "Intentional non-Refit fixture used to verify generator diagnostics.")]
    string SomeOtherMethod();

    /// <summary>Puts content for an id together with an authorization header.</summary>
    /// <param name="id">The identifier of the resource to put to.</param>
    /// <param name="content">The body content to put.</param>
    /// <param name="authorization">The authorization header value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Put("/foo/bar/{id}")]
    Task PutSomeContentWithAuthorization(
        int id,
        [Body] object content,
        [Header("Authorization")] string authorization);

    /// <summary>Puts string content for an id with a dynamically supplied content type.</summary>
    /// <param name="id">The identifier of the resource to put to.</param>
    /// <param name="content">The body content to put.</param>
    /// <param name="contentType">The content type supplied dynamically.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Put("/foo/bar/{id}")]
    Task<string> PutSomeStuffWithDynamicContentType(
        int id,
        [Body] string content,
        [Header("Content-Type")] string contentType);

    /// <summary>Posts a nullable value-type body for an id and returns a boolean.</summary>
    /// <param name="id">The identifier of the resource to post to.</param>
    /// <param name="content">The nullable value-type body content.</param>
    /// <returns>A task whose result indicates whether the operation succeeded.</returns>
    [Post("/foo/bar/{id}")]
    Task<bool> PostAValueType(int id, [Body] Guid? content);

    /// <summary>Patches something for an id and returns an observable of the response.</summary>
    /// <param name="id">The identifier of the resource to patch.</param>
    /// <param name="someAttribute">The body content to patch.</param>
    /// <returns>An observable that yields the response string.</returns>
    [Patch("/foo/bar/{id}")]
    IObservable<string> PatchSomething(int id, [Body] string someAttribute);

    /// <summary>Sends an OPTIONS request for an id with a body.</summary>
    /// <param name="id">The identifier of the resource to send options for.</param>
    /// <param name="someAttribute">The body content to send.</param>
    /// <returns>A task whose result is the response string.</returns>
    [Options("/foo/bar/{id}")]
    Task<string> SendOptions(int id, [Body] string someAttribute);

    /// <summary>Gets some stuff by id with a formatted query value.</summary>
    /// <param name="id">The identifier formatted as a query value.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo/bar/{id}")]
    Task<string> FetchSomeStuffWithQueryFormat([Query(Format = "0.0")] int id);

    /// <summary>Queries with an enumerable of integers.</summary>
    /// <param name="numbers">The enumerable of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnumerable(IEnumerable<int> numbers);

    /// <summary>Queries with an array of integers.</summary>
    /// <param name="numbers">The array of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithArray(int[] numbers);

    /// <summary>Queries with explicit named parameters mapped into the route.</summary>
    /// <param name="param1">The first explicit query parameter.</param>
    /// <param name="param2">The second explicit query parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query?q1={param1}&q2={param2}")]
    Task QueryWithExplicitParameters(string param1, string param2);

    /// <summary>Queries with an integer array formatted as multiple values.</summary>
    /// <param name="numbers">The array of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithArrayFormattedAsMulti([Query(CollectionFormat.Multi)] int[] numbers);

    /// <summary>Queries with an integer array formatted as comma-separated values.</summary>
    /// <param name="numbers">The array of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithArrayFormattedAsCsv([Query(CollectionFormat.Csv)] int[] numbers);

    /// <summary>Queries with an integer array formatted as space-separated values.</summary>
    /// <param name="numbers">The array of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithArrayFormattedAsSsv([Query(CollectionFormat.Ssv)] int[] numbers);

    /// <summary>Queries with an integer array formatted as tab-separated values.</summary>
    /// <param name="numbers">The array of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithArrayFormattedAsTsv([Query(CollectionFormat.Tsv)] int[] numbers);

    /// <summary>Queries with an integer array formatted as pipe-separated values.</summary>
    /// <param name="numbers">The array of integer query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithArrayFormattedAsPipes([Query(CollectionFormat.Pipes)] int[] numbers);

    /// <summary>Gets foo with a complex query object containing a dictionary.</summary>
    /// <param name="query">The complex query object.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task ComplexQueryObjectWithDictionary([Query] ComplexQueryObject query);

    /// <summary>Gets foo with a query dictionary keyed by an enum.</summary>
    /// <param name="query">The query dictionary keyed by an enum.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task QueryWithDictionaryWithEnumKey([Query] IDictionary<TestEnum, string> query);

    /// <summary>Gets foo with a query dictionary using a key prefix.</summary>
    /// <param name="query">The query dictionary supplied with a key prefix.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task QueryWithDictionaryWithPrefix(
        [Query(".", "dictionary")] IDictionary<TestEnum, string> query);

    /// <summary>Posts a body whose serialization method is not a declared enum value.</summary>
    /// <param name="body">The body that must be left unserialized.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task PostWithUndeclaredBodySerializationMethod([Body((BodySerializationMethod)99)] string body);

    /// <summary>Posts a body using the obsolete JSON serialization method, kept for legacy callers.</summary>
    /// <param name="body">The body to serialize.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
#pragma warning disable CS0618 // The reflection path must keep accepting legacy BodySerializationMethod.Json callers.
    Task PostWithObsoleteJsonBody([Body(BodySerializationMethod.Json)] string body);
#pragma warning restore CS0618

    /// <summary>Gets foo with a query object exercising null serialization and property formatting.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task QueryWithSerializationObject([Query] QuerySerializationObject query);

    /// <summary>Gets foo with a query dictionary whose values may be complex objects or null.</summary>
    /// <param name="query">The query dictionary keyed by strings.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task QueryWithObjectDictionary([Query] IDictionary<string, object?> query);

    /// <summary>Gets foo with a query dictionary keyed by integers.</summary>
    /// <param name="query">The query dictionary keyed by integers.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task QueryWithDictionaryWithNumericKey([Query] IDictionary<int, string> query);

    /// <summary>Queries with an enumerable formatted as multiple values.</summary>
    /// <param name="lines">The enumerable of query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnumerableFormattedAsMulti(
        [Query(CollectionFormat.Multi)] IEnumerable<string> lines);

    /// <summary>Queries with an enumerable formatted as comma-separated values.</summary>
    /// <param name="lines">The enumerable of query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnumerableFormattedAsCsv(
        [Query(CollectionFormat.Csv)] IEnumerable<string> lines);

    /// <summary>Queries with an enumerable formatted as space-separated values.</summary>
    /// <param name="lines">The enumerable of query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnumerableFormattedAsSsv(
        [Query(CollectionFormat.Ssv)] IEnumerable<string> lines);

    /// <summary>Queries with an enumerable formatted as tab-separated values.</summary>
    /// <param name="lines">The enumerable of query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnumerableFormattedAsTsv(
        [Query(CollectionFormat.Tsv)] IEnumerable<string> lines);

    /// <summary>Queries with an enumerable formatted as pipe-separated values.</summary>
    /// <param name="lines">The enumerable of query values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnumerableFormattedAsPipes(
        [Query(CollectionFormat.Pipes)] IEnumerable<string> lines);

    /// <summary>Queries with an object whose getters are private.</summary>
    /// <param name="person">The person object whose getters are private.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithObjectWithPrivateGetters(Person person);

    /// <summary>Posts a multipart file with a name supplied as a query string parameter.</summary>
    /// <param name="source">The file to upload.</param>
    /// <param name="name">The file name supplied as a query string parameter.</param>
    /// <returns>A task whose result is the HTTP response message.</returns>
    [Multipart]
    [Post("/foo?&name={name}")]
    Task<HttpResponseMessage> PostWithQueryStringParameters(FileInfo source, string name);

    /// <summary>Queries with an enum member as the query value.</summary>
    /// <param name="foo">The enum member used as the query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithEnum(FooWithEnumMember foo);

    /// <summary>Queries with a type containing an enum member.</summary>
    /// <param name="foo">The type containing an enum member.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    Task QueryWithTypeWithEnum(TypeFooWithEnumMember foo);

    /// <summary>Queries an api endpoint by id exercising several optional query parameters.</summary>
    /// <param name="id">The identifier of the resource to query.</param>
    /// <param name="text">The optional text query parameter.</param>
    /// <param name="optionalId">The optional identifier query parameter.</param>
    /// <param name="foo">The optional foo query parameter treated as a string.</param>
    /// <param name="filters">The optional filters formatted as multiple values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Refit interface stub intentionally uses optional parameters to exercise default-value handling.")]
    [Get("/api/{id}")]
    Task QueryWithOptionalParameters(
        int id,
        [Query] string? text = null,
        [Query] int? optionalId = null,
        [Query(TreatAsString = true)] Foo? foo = null,
        [Query(CollectionFormat = CollectionFormat.Multi)] string[]? filters = null);

    /// <summary>Deletes via an enum member supplied as a query value.</summary>
    /// <param name="foo">The enum member supplied as a query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Delete("/api/bar")]
    Task ClearWithEnumMember([Query] FooWithEnumMember foo);

    /// <summary>Deletes a video using a player index query value.</summary>
    /// <param name="playerIndex">The player index query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Delete("/api/v1/video")]
    Task Clear([Query] int playerIndex);

    /// <summary>Posts a byte array part to blob storage at a catch-all file path.</summary>
    /// <param name="filepath">The catch-all file path within blob storage.</param>
    /// <param name="byteArray">The byte array part to upload.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Multipart]
    [Post("/blobstorage/{**filepath}")]
    Task Blob_Post_Byte(string filepath, [AliasAs("attachment")] ByteArrayPart byteArray);

    /// <summary>Uploads a file as a multipart stream with optional overwrite and metadata.</summary>
    /// <param name="companyId">The identifier of the company.</param>
    /// <param name="path">The destination path for the upload.</param>
    /// <param name="stream">The stream part to upload.</param>
    /// <param name="authorization">The authorization header value.</param>
    /// <param name="overwrite">A value indicating whether to overwrite an existing file.</param>
    /// <param name="metadata">The optional file metadata.</param>
    /// <returns>A task whose result is the API response.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Refit interface stub intentionally uses optional parameters to exercise default-value handling.")]
    [Multipart]
    [Post("/companies/{companyId}/{path}")]
    Task<ApiResponse<object>> UploadFile(
        int companyId,
        string path,
        [AliasAs("file")] StreamPart stream,
        [Header("Authorization")] string authorization,
        bool overwrite = false,
        [AliasAs("fileMetadata")] string? metadata = null);

    /// <summary>Posts foo with a complex type supplied as a query object.</summary>
    /// <param name="queryParams">The complex type supplied as a query object.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("/foo")]
    Task PostWithComplexTypeQuery([Query] ComplexQueryObject queryParams);

    /// <summary>Gets foo with a complex query type containing an inner collection.</summary>
    /// <param name="queryParams">The complex query type containing an inner collection.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task ComplexTypeQueryWithInnerCollection([Query] ComplexQueryObject queryParams);

    /// <summary>Gets foo with a complex query type that has no query attribute.</summary>
    /// <param name="queryParams">The complex query type with no query attribute.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task ComplexTypeQueryWithoutQueryAttribute(ComplexQueryObject queryParams);

    /// <summary>Gets foo with a complex query type using parameter-level multi formatting.</summary>
    /// <param name="queryParams">The complex query type formatted as multiple values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/foo")]
    Task ComplexTypeQueryParameterLevelMulti(
        [Query(CollectionFormat.Multi)] ComplexQueryObject queryParams);

    /// <summary>Queries an api endpoint with a path-bound object plus optional query parameters.</summary>
    /// <param name="obj">The path-bound object.</param>
    /// <param name="text">The optional text query parameter.</param>
    /// <param name="optionalId">The optional identifier query parameter.</param>
    /// <param name="filters">The optional filters formatted as multiple values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "Refit interface stub intentionally uses optional parameters to exercise default-value handling.")]
    [Get("/api/{obj.someProperty}")]
    Task QueryWithOptionalParametersPathBoundObject(
        PathBoundObject obj,
        [Query] string? text = null,
        [Query] int? optionalId = null,
        [Query(CollectionFormat = CollectionFormat.Multi)] string[]? filters = null);

    /// <summary>Queries a device list where headers are declared before data parameters.</summary>
    /// <param name="authorization">The authorization header value.</param>
    /// <param name="twoLetterLang">The two-letter language header value.</param>
    /// <param name="search">The search query value.</param>
    /// <param name="controlId">The control identifier mapped into the route.</param>
    /// <param name="secret">The secret query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Headers("Accept:application/json", "X-API-V: 125")]
    [Get("/api/someModule/deviceList?controlId={control_id}")]
    Task QueryWithHeadersBeforeData(
        [Header("Authorization")] string authorization,
        [Header("X-Lng")] string twoLetterLang,
        string search,
        [AliasAs("control_id")] string controlId,
        string secret);

    /// <summary>Queries with an unescaped query URI format.</summary>
    /// <param name="q">The unescaped query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    [QueryUriFormat(UriFormat.Unescaped)]
    Task UnescapedQueryParams(string q);

    /// <summary>Queries with an unescaped query URI format plus a filter parameter.</summary>
    /// <param name="q">The unescaped query value.</param>
    /// <param name="filter">The filter query value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/query")]
    [QueryUriFormat(UriFormat.Unescaped)]
    Task UnescapedQueryParamsWithFilter(string q, string filter);

    /// <summary>Calls an api that substitutes the same parameter into the URL more than once.</summary>
    /// <param name="id">The identifier substituted into the URL more than once.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/api/foo/{id}/file_{id}?query={id}")]
    Task SomeApiThatUsesParameterMoreThanOnceInTheUrl(string id);
}
