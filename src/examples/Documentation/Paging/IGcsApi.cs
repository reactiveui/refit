// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Lists Google Cloud Storage objects, which pages with a <c>pageToken</c> and <c>nextPageToken</c> in a JSON body.</summary>
internal interface IGcsApi
{
    /// <summary>Lists the objects in a bucket.</summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="prefix">Limits the listing to objects that begin with this text.</param>
    /// <param name="maxResults">The most objects on a page.</param>
    /// <param name="pageToken">The token of the page to start at, or <see langword="null"/> to start at the first object.</param>
    /// <returns>A lazy sequence of objects, which is also a sequence of the pages Google returned.</returns>
    [Get("/storage/v1/b/{bucket}/o")]
    [Paged(Next = nameof(GcsObjectList.NextPageToken))]
    PagedEnumerable<GcsObjectList, GcsObject> ListObjects(string bucket, string? prefix, int maxResults, [PageToken] string? pageToken = null);
}
