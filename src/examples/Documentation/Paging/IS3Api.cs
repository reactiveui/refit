// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Lists S3 objects with ListObjectsV2, which pages with an opaque continuation token.</summary>
internal interface IS3Api
{
    /// <summary>Lists the objects in a bucket.</summary>
    /// <param name="bucket">The bucket name.</param>
    /// <param name="prefix">Limits the listing to keys that begin with this text.</param>
    /// <param name="maxKeys">The most keys on a page.</param>
    /// <param name="continuationToken">The token of the page to start at, or <see langword="null"/> to start at the first key.</param>
    /// <returns>A lazy sequence of objects, which is also a sequence of the pages S3 returned.</returns>
    [Get("/{bucket}?list-type=2")]
    [Paged(Next = nameof(ListBucketResult.NextContinuationToken))]
    PagedEnumerable<ListBucketResult, S3Object> ListObjects(
        string bucket,
        string? prefix,
        [AliasAs("max-keys")] int maxKeys,
        [PageToken] [AliasAs("continuation-token")] string? continuationToken = null);
}
