// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>The body of a DynamoDB Scan request, which carries the continuation as <c>ExclusiveStartKey</c>.</summary>
[System.Diagnostics.DebuggerDisplay("{TableName}: limit {Limit}")]
public sealed class DynamoScanRequest
{
    /// <summary>Gets or sets the table to scan.</summary>
    [JsonPropertyName("TableName")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the most items to evaluate.</summary>
    [JsonPropertyName("Limit")]
    public int Limit { get; set; }

    /// <summary>Gets or sets the key of the item after which the scan resumes; <see langword="null"/> starts at the beginning.</summary>
    [JsonPropertyName("ExclusiveStartKey")]
    public Dictionary<string, DynamoAttributeValue>? ExclusiveStartKey { get; set; }
}
