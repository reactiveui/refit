// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>The XML body of an Azure Blob Storage List Blobs response: one page of blobs and the marker for the next.</summary>
[XmlRoot("EnumerationResults")]
[System.Diagnostics.DebuggerDisplay("{ContainerName}: {Blobs.Items.Count} blobs, next = {NextMarker}")]
public sealed class EnumerationResults
{
    /// <summary>Gets or sets the container name.</summary>
    [XmlAttribute]
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the prefix the listing was limited to.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the marker that requested this page.</summary>
    public string Marker { get; set; } = string.Empty;

    /// <summary>Gets or sets the most blobs a page can hold.</summary>
    public int MaxResults { get; set; }

    /// <summary>Gets or sets the blobs on this page.</summary>
    public BlobList Blobs { get; set; } = new();

    /// <summary>Gets or sets the marker that requests the next page; the last page carries an empty element.</summary>
    public string? NextMarker { get; set; }
}
