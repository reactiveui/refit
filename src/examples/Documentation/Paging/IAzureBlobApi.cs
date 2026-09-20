// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Lists Azure Blob Storage blobs, which pages with a marker in an XML body.</summary>
internal interface IAzureBlobApi
{
    /// <summary>Lists the blobs in a container.</summary>
    /// <param name="container">The container name.</param>
    /// <param name="prefix">Limits the listing to blobs that begin with this text.</param>
    /// <param name="maxResults">The most blobs on a page.</param>
    /// <param name="marker">The marker of the page to start at, or <see langword="null"/> to start at the first blob.</param>
    /// <returns>A lazy sequence of blobs, which is also a sequence of the pages Azure returned.</returns>
    [Get("/{container}?restype=container&comp=list")]
    [Paged(Items = "Blobs.Items", Next = nameof(EnumerationResults.NextMarker))]
    PagedEnumerable<EnumerationResults, Blob> ListBlobs(
        string container,
        string? prefix,
        [AliasAs("maxresults")] int maxResults,
        [PageToken] string? marker = null);
}
