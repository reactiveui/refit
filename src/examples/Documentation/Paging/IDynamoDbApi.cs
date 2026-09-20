// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>Scans a DynamoDB table with an ordinary call whose continuation lives inside the request body.</summary>
internal interface IDynamoDbApi
{
    /// <summary>Scans one page of a table.</summary>
    /// <param name="request">The scan request; its <c>ExclusiveStartKey</c> names where the page starts.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The page.</returns>
    [Post("/")]
    [Headers("X-Amz-Target: DynamoDB_20120810.Scan")]
    Task<DynamoScanResponse> Scan([Body] DynamoScanRequest request, CancellationToken cancellationToken);
}
