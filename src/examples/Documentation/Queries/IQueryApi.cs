// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Builds requests for scalar, grouped, collection and flag query forms.</summary>
internal interface IQueryApi
{
    /// <summary>Combines a fixed query with aliased values and omits a null city.</summary>
    /// <param name="name">The search text sent as the aliased q query value.</param>
    /// <param name="page">The page number sent as a scalar query value.</param>
    /// <param name="city">The optional city; null omits its query field.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people?active=true")]
    Task<HttpRequestMessage> SearchAsync([AliasAs("q")] string name, int page, string? city);

    /// <summary>Flattens properties beneath the filter prefix with the configured key formatter.</summary>
    /// <param name="filter">The values flattened under the filter prefix.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people")]
    Task<HttpRequestMessage> FilterAsync([Query(".", "filter")] SearchFilter filter);

    /// <summary>Combines repeated numbers, comma-separated tags and indexed person properties.</summary>
    /// <param name="ids">The identifiers emitted as repeated query fields.</param>
    /// <param name="tags">The tags joined into one comma-separated query value.</param>
    /// <param name="people">The people emitted as JSON lines or indexed query entries, as declared.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people")]
    Task<HttpRequestMessage> CollectionsAsync([Query(CollectionFormat.Multi)] int[] ids, [Query(CollectionFormat.Csv)] string[] tags, [Query(CollectionFormat.Indexed)] List<Person> people);

    /// <summary>Adds name-only flags and preserves the already encoded search value.</summary>
    /// <param name="flags">The name-only query flags; null entries are omitted.</param>
    /// <param name="encoded">The already escaped search value, preserved without another encoding pass.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people")]
    Task<HttpRequestMessage> FlagsAsync([QueryName] string?[] flags, [AliasAs("q")] [Encoded] string encoded);

    /// <summary>Builds a request whose original URI string preserves unescaped query text.</summary>
    /// <param name="q">The search value preserved in the request's unescaped original URI string.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people")]
    [QueryUriFormat(UriFormat.Unescaped)]
    Task<HttpRequestMessage> UnescapedAsync(string q);

    /// <summary>Builds a query with an explicitly JSON-named model property.</summary>
    /// <param name="value">The property whose JSON name can be honored or ignored.</param>
    /// <returns>The unsent request owned by the caller.</returns>
    [Get("/people")]
    Task<HttpRequestMessage> JsonNamesAsync([Query] JsonNamedValue value);
}
