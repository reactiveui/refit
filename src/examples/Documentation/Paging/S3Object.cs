// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Refit.Documentation.Paging;

/// <summary>An object listed by S3 ListObjectsV2, in the namespace S3 puts on every element.</summary>
[XmlType(Namespace = ListBucketResult.XmlNamespace)]
[System.Diagnostics.DebuggerDisplay("{Key}: {Size} bytes")]
public sealed class S3Object
{
    /// <summary>Gets or sets the object key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the object size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Gets or sets the storage class.</summary>
    public string StorageClass { get; set; } = string.Empty;
}
