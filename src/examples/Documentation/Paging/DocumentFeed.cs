// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>The body of an Azure Cosmos DB query response; the continuation travels in a response header.</summary>
[System.Diagnostics.DebuggerDisplay("{Count} documents")]
public sealed class DocumentFeed
{
    /// <summary>Gets or sets the documents on the page.</summary>
    [JsonPropertyName("Documents")]
    public List<CosmosDocument> Documents { get; set; } = [];

    /// <summary>Gets or sets the number of documents on the page.</summary>
    [JsonPropertyName("_count")]
    public int Count { get; set; }
}
