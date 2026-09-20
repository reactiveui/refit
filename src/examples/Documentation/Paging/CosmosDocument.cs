// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Paging;

/// <summary>A document returned by an Azure Cosmos DB query.</summary>
[System.Diagnostics.DebuggerDisplay("{Id}: {Category}")]
public sealed class CosmosDocument
{
    /// <summary>Gets or sets the document identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the document's category.</summary>
    public string Category { get; set; } = string.Empty;
}
