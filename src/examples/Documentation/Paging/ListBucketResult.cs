// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>The XML body of an S3 ListObjectsV2 response: one page of keys and the token for the next.</summary>
[XmlRoot("ListBucketResult", Namespace = XmlNamespace)]
[XmlType(Namespace = XmlNamespace)]
[System.Diagnostics.DebuggerDisplay("{Name}: {KeyCount} keys, truncated = {IsTruncated}")]
public sealed class ListBucketResult
{
    /// <summary>The XML namespace S3 declares on every response.</summary>
    internal const string XmlNamespace = "http://s3.amazonaws.com/doc/2006-03-01/";

    /// <summary>Gets or sets the bucket name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the prefix the listing was limited to.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of keys on this page.</summary>
    public int KeyCount { get; set; }

    /// <summary>Gets or sets the most keys a page can hold.</summary>
    public int MaxKeys { get; set; }

    /// <summary>Gets or sets a value indicating whether more keys follow this page.</summary>
    public bool IsTruncated { get; set; }

    /// <summary>Gets or sets the token that requests the next page; absent on the last page.</summary>
    public string? NextContinuationToken { get; set; }

    /// <summary>Gets or sets the objects on this page.</summary>
    [XmlElement("Contents")]
    public List<S3Object> Contents { get; set; } = [];
}
