// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>The body of an Azure Cosmos DB SQL query request.</summary>
[System.Diagnostics.DebuggerDisplay("{Query}")]
public sealed class CosmosQuery
{
    /// <summary>Gets or sets the SQL query text.</summary>
    public string Query { get; set; } = string.Empty;
}
