// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>The body of a DynamoDB Scan response, which names the continuation as <c>LastEvaluatedKey</c>.</summary>
[System.Diagnostics.DebuggerDisplay("{Count} items")]
public sealed class DynamoScanResponse
{
    /// <summary>Gets or sets the items on the page, each a map of attribute names to values.</summary>
    [JsonPropertyName("Items")]
    public List<Dictionary<string, DynamoAttributeValue>> Items { get; set; } = [];

    /// <summary>Gets or sets the number of items on the page.</summary>
    [JsonPropertyName("Count")]
    public int Count { get; set; }

    /// <summary>Gets or sets the key to resume after; absent when the scan has reached the end of the table.</summary>
    [JsonPropertyName("LastEvaluatedKey")]
    public Dictionary<string, DynamoAttributeValue>? LastEvaluatedKey { get; set; }
}
