// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Queries Azure Cosmos DB documents, which pages with an <c>x-ms-continuation</c> header in both directions.</summary>
internal interface ICosmosApi
{
    /// <summary>Runs a query against a collection.</summary>
    /// <param name="database">The database name.</param>
    /// <param name="collection">The collection name.</param>
    /// <param name="query">The SQL query, sent as the body of every page's request.</param>
    /// <param name="maxItemCount">The most documents on a page.</param>
    /// <param name="continuation">The continuation of the page to start at, or <see langword="null"/> to start at the first document.</param>
    /// <returns>A lazy sequence of documents, which is also a sequence of the responses Cosmos DB returned.</returns>
    [Post("/dbs/{database}/colls/{collection}/docs")]
    [Headers("x-ms-documentdb-isquery: true")]
    [Paged(NextHeader = "x-ms-continuation")]
    PagedEnumerable<ApiResponse<DocumentFeed>, CosmosDocument> Query(
        string database,
        string collection,
        [Body] CosmosQuery query,
        [Header("x-ms-max-item-count")] int maxItemCount,
        [PageToken] [Header("x-ms-continuation")] string? continuation = null);
}
