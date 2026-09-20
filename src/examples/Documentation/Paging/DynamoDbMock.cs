// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Globalization;
using System.Text.Json;

namespace Refit.Documentation.Paging;

/// <summary>A stand-in for DynamoDB Scan that serves seven items and resumes after the key the client sends back.</summary>
internal static class DynamoDbMock
{
    /// <summary>The origin of the regional DynamoDB endpoint.</summary>
    internal const string Origin = "https://dynamodb.us-east-1.amazonaws.com";

    /// <summary>The number of items the table holds.</summary>
    internal const int ItemCount = 7;

    /// <summary>The name of the key attribute.</summary>
    private const string KeyAttribute = "OrderId";

    /// <summary>Answers a Scan request.</summary>
    /// <param name="request">The request, whose body is the scan.</param>
    /// <returns>The JSON response.</returns>
    internal static async Task<HttpResponseMessage> ServeAsync(HttpRequestMessage request)
    {
        DynamoScanRequest scan = JsonSerializer.Deserialize(await request.Content!.ReadAsStringAsync(), PagingJsonContext.Default.DynamoScanRequest)!;
        int start = scan.ExclusiveStartKey is null ? 0 : int.Parse(scan.ExclusiveStartKey[KeyAttribute].S!, CultureInfo.InvariantCulture);
        int end = Math.Min(start + scan.Limit, ItemCount);

        DynamoScanResponse response = new() { Count = end - start };
        for (int index = start; index < end; index++)
        {
            response.Items.Add(new() { [KeyAttribute] = new() { S = (index + 1).ToString(CultureInfo.InvariantCulture) } });
        }

        // DynamoDB names the last key it evaluated, and stops naming one once the table is exhausted.
        if (end < ItemCount)
        {
            response.LastEvaluatedKey = new() { [KeyAttribute] = new() { S = end.ToString(CultureInfo.InvariantCulture) } };
        }

        return PagingReplies.Json(response, PagingJsonContext.Default.DynamoScanResponse);
    }
}
