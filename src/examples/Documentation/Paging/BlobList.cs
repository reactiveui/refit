// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>The <c>Blobs</c> element of an Azure Blob Storage listing.</summary>
[System.Diagnostics.DebuggerDisplay("{Items.Count} blobs")]
public sealed class BlobList
{
    /// <summary>Gets or sets the blobs on the page.</summary>
    [XmlElement("Blob")]
    public List<Blob> Items { get; set; } = [];
}
