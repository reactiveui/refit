// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Refit.LiveQuery;

/// <summary>Contains the query models and API generated into the test assembly.</summary>
public static partial class LiveQueryApi
{
    /// <summary>Sort values used by enum formatting tests.</summary>
    public enum SearchSort
    {
        /// <summary>Sorts by descending date.</summary>
        [EnumMember(Value = "date-desc")]
        DateDescending = 0,

        /// <summary>Sorts by name.</summary>
        Name = 1,
    }

    /// <summary>Exercises generated query, path, body, and custom-verb request construction.</summary>
    public interface ILiveQueryApi
    {
        /// <summary>Sends a plain query value.</summary>
        /// <param name="q">The query value.</param>
        /// <returns>The response body.</returns>
        [Get("/search")]
        Task<string> Plain(string q);

        /// <summary>Sends a custom route token.</summary>
        /// <param name="token">The route token.</param>
        /// <returns>The response body.</returns>
        [Get("/token/{token}")]
        Task<string> TokenPath(RouteToken token);

        /// <summary>Sends a dotted route object.</summary>
        /// <param name="info">The route information.</param>
        /// <returns>The response body.</returns>
        [Get("/docs/{info.Slug}/rev/{info.Version}")]
        Task<string> DottedPath(RouteInfo info);

        /// <summary>Sends a dotted route object with residual properties.</summary>
        /// <param name="info">The route information.</param>
        /// <returns>The response body.</returns>
        [Get("/tags/{info.Slug}")]
        Task<string> DottedPathResidual(RouteInfo info);

        /// <summary>Sends a nested dotted route object.</summary>
        /// <param name="order">The order.</param>
        /// <returns>The response body.</returns>
        [Get("/orders/{order.Customer.Id}")]
        Task<string> NestedPath(NestedOrder order);

        /// <summary>Sends aliased query values.</summary>
        /// <param name="user">The user value.</param>
        /// <param name="kind">The kind value.</param>
        /// <returns>The response body.</returns>
        [Get("/signin")]
        Task<string> Alias([AliasAs("login")] string user, [AliasAs("kind")] string kind);

        /// <summary>Sends multiple query values.</summary>
        /// <param name="a">The string value.</param>
        /// <param name="b">The integer value.</param>
        /// <param name="c">The Boolean value.</param>
        /// <returns>The response body.</returns>
        [Get("/multi")]
        Task<string> Multiple(string a, int b, bool c);

        /// <summary>Skips a null query value.</summary>
        /// <param name="a">The optional value.</param>
        /// <param name="b">The required value.</param>
        /// <returns>The response body.</returns>
        [Get("/nullskip")]
        Task<string> NullSkip(string? a, string b);

        /// <summary>Sends a formatted numeric query value.</summary>
        /// <param name="price">The price.</param>
        /// <returns>The response body.</returns>
        [Get("/fmt")]
        Task<string> Formatted([Query(Format = "0.00")] double price);

        /// <summary>Sends a comma-separated collection.</summary>
        /// <param name="ids">The identifiers.</param>
        /// <returns>The response body.</returns>
        [Get("/csv")]
        Task<string> Csv([Query(CollectionFormat.Csv)] int[] ids);

        /// <summary>Sends a repeated-key collection.</summary>
        /// <param name="ids">The identifiers.</param>
        /// <returns>The response body.</returns>
        [Get("/expand")]
        Task<string> Expanded([Query(CollectionFormat.Multi)] int[] ids);

        /// <summary>Sends a pipe-separated collection.</summary>
        /// <param name="values">The values.</param>
        /// <returns>The response body.</returns>
        [Get("/pipes")]
        Task<string> Pipes([Query(CollectionFormat.Pipes)] string[] values);

        /// <summary>Sends a list using the default collection format.</summary>
        /// <param name="ids">The identifiers.</param>
        /// <returns>The response body.</returns>
        [Get("/list")]
        Task<string> DefaultList(List<int> ids);

        /// <summary>Sends an enum query value.</summary>
        /// <param name="sort">The sort order.</param>
        /// <returns>The response body.</returns>
        [Get("/enum")]
        Task<string> Sorted(SearchSort sort);

        /// <summary>Sends a nullable query value.</summary>
        /// <param name="page">The page.</param>
        /// <returns>The response body.</returns>
        [Get("/page")]
        Task<string> Paged(int? page);

        /// <summary>Sends a long query value.</summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The response body.</returns>
        [Get("/big")]
        Task<string> Big(long id);

        /// <summary>Sends a query value treated as a string.</summary>
        /// <param name="raw">The raw value.</param>
        /// <returns>The response body.</returns>
        [Get("/treat")]
        Task<string> Treated([Query(TreatAsString = true)] double raw);

        /// <summary>Appends a value to a template with an existing query.</summary>
        /// <param name="extra">The extra value.</param>
        /// <returns>The response body.</returns>
        [Get("/tmpl?fixed=1")]
        Task<string> Templated(string extra);

        /// <summary>Sends an unescaped query value.</summary>
        /// <param name="q">The query value.</param>
        /// <returns>The response body.</returns>
        [QueryUriFormat(UriFormat.Unescaped)]
        [Get("/soql")]
        Task<string> UnescapedQuery(string q);

        /// <summary>Flattens a range query object.</summary>
        /// <param name="query">The range query.</param>
        /// <returns>The response body.</returns>
        [Get("/range")]
        Task<string> RangeSearch([Query] RangeQuery query);

        /// <summary>Flattens a nullable struct query object.</summary>
        /// <param name="point">The point.</param>
        /// <returns>The response body.</returns>
        [Get("/point")]
        Task<string> NullableStructQuery([Query] GeoPoint? point);

        /// <summary>Flattens dictionary query values.</summary>
        /// <param name="facets">The facets.</param>
        /// <returns>The response body.</returns>
        [Get("/facets")]
        Task<string> Facets(Dictionary<string, Facet> facets);

        /// <summary>Sends a body with the QUERY verb.</summary>
        /// <param name="body">The request body.</param>
        /// <returns>The response body.</returns>
        [QueryVerb("/documents")]
        Task<string> QueryDocuments([Body] CreatePayload body);

        /// <summary>Sends query values with the QUERY verb.</summary>
        /// <param name="filter">The query filter.</param>
        /// <returns>The response body.</returns>
        [QueryVerb("/rows")]
        Task<string> QueryRows([Query] RangeQuery filter);

        /// <summary>Sends a date-time query value.</summary>
        /// <param name="at">The date and time.</param>
        /// <returns>The response body.</returns>
        [Get("/when")]
        Task<string> When(DateTimeOffset at);

        /// <summary>Sends an inferred body and query value.</summary>
        /// <param name="payload">The request body.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>The response body.</returns>
        [Post("/create")]
        Task<string> Create(CreatePayload payload, string tag);

        /// <summary>Sends one valueless query flag.</summary>
        /// <param name="flag">The flag.</param>
        /// <returns>The response body.</returns>
        [Get("/flags")]
        Task<string> Flag([QueryName] string flag);

        /// <summary>Sends multiple valueless query flags.</summary>
        /// <param name="flags">The flags.</param>
        /// <returns>The response body.</returns>
        [Get("/flags/many")]
        Task<string> Flags([QueryName] string[] flags);

        /// <summary>Sends a caller-encoded query value.</summary>
        /// <param name="v">The encoded value.</param>
        /// <returns>The response body.</returns>
        [Get("/encq")]
        Task<string> EncodedQuery([Encoded] string v);

        /// <summary>Sends a caller-encoded path value.</summary>
        /// <param name="id">The encoded identifier.</param>
        /// <returns>The response body.</returns>
        [Get("/encp/{id}")]
        Task<string> EncodedPath([Encoded] string id);

        /// <summary>Sends a caller-encoded catch-all path.</summary>
        /// <param name="rest">The encoded path.</param>
        /// <returns>The response body.</returns>
        [Get("/cal/{**rest}")]
        Task<string> EncodedRoundTrip([Encoded] string rest);

        /// <summary>Sends an optional trailing path value.</summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="notifMsgId">The notification identifier.</param>
        /// <returns>The response body.</returns>
        [Get("/push/{deviceId}/{notifMsgId?}")]
        Task<string> TrailingOptional(string deviceId, string? notifMsgId);

        /// <summary>Sends indexed object query values.</summary>
        /// <param name="items">The items.</param>
        /// <returns>The response body.</returns>
        [Get("/indexed")]
        Task<string> IndexedSearch([Query(CollectionFormat.Indexed)] List<Item>? items);

        /// <summary>Sends indexed integer query values.</summary>
        /// <param name="items">The items.</param>
        /// <returns>The response body.</returns>
        [Get("/indexedListInt")]
        Task<string> IndexedListSearch([Query(CollectionFormat.Indexed)] List<int>? items);

        /// <summary>Sends indexed names with serialized property names.</summary>
        /// <param name="items">The names.</param>
        /// <returns>The response body.</returns>
        [Get("/indexedNameWithSerialized")]
        Task<string> IndexedNameWithSerialized([Query(CollectionFormat.Indexed)] List<Name> items);

        /// <summary>Sends a single indexed object.</summary>
        /// <param name="item">The item.</param>
        /// <returns>The response body.</returns>
        [Get("/indexedSimpleType")]
        Task<string> IndexedSimpleType([Query(CollectionFormat.Indexed)] Item item);
    }
}
