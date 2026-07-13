// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>The Refit interface exercised by the query request-building benchmarks; every method is inline-eligible.</summary>
public interface IQueryRequestService
{
    /// <summary>Sends a request with one auto-appended query parameter.</summary>
    /// <param name="q">The query text.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/search")]
    Task<HttpResponseMessage> SingleQueryAsync(string q);

    /// <summary>Sends a request with several scalar query parameters.</summary>
    /// <param name="q">The query text.</param>
    /// <param name="page">The page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="includeArchived">Whether archived entries are included.</param>
    /// <param name="sort">The sort order.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/search/full")]
    Task<HttpResponseMessage> MultiParameterAsync(string q, int page, int size, bool includeArchived, QuerySort sort);

    /// <summary>Sends a request with a csv-joined collection.</summary>
    /// <param name="ids">The identifiers.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/csv")]
    Task<HttpResponseMessage> CsvCollectionAsync([Query(CollectionFormat.Csv)] int[] ids);

    /// <summary>Sends a request with a multi-expanded collection.</summary>
    /// <param name="ids">The identifiers.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/multi")]
    Task<HttpResponseMessage> MultiCollectionAsync([Query(CollectionFormat.Multi)] int[] ids);

    /// <summary>Sends a request with a valueless query flag.</summary>
    /// <param name="flag">The flag name.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/flags")]
    Task<HttpResponseMessage> FlagAsync([QueryName] string flag);

    /// <summary>Sends a request with a caller-encoded query value.</summary>
    /// <param name="cursor">The pre-encoded continuation cursor.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/cursor")]
    Task<HttpResponseMessage> EncodedAsync([Encoded] string cursor);

    /// <summary>Sends a request with a span-formattable timestamp query value that requires percent-encoding.</summary>
    /// <param name="at">The timestamp, whose invariant form contains reserved characters.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/events")]
    Task<HttpResponseMessage> TimestampQueryAsync(DateTimeOffset at);

    /// <summary>Sends a request with a span-formattable timestamp path value that requires percent-encoding.</summary>
    /// <param name="at">The timestamp substituted into the path, whose invariant form contains reserved characters.</param>
    /// <returns>The HTTP response message.</returns>
    [Get("/events/{at}")]
    Task<HttpResponseMessage> TimestampPathAsync(DateTimeOffset at);

    /// <summary>Sends a request with a custom HTTP verb, exercising the cached verb instance.</summary>
    /// <param name="q">The query text.</param>
    /// <returns>The HTTP response message.</returns>
    [QueryVerb("/documents")]
    Task<HttpResponseMessage> CustomVerbAsync(string q);
}
