// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A deserialization fixture mirroring the npmjs registry document used by the Refit tests.</summary>
public class RootObject
{
    /// <summary>Gets or sets the document identifier (the npmjs <c>_id</c> field).</summary>
    public string? _id { get; set; }

    /// <summary>Gets or sets the document revision (the npmjs <c>_rev</c> field).</summary>
    public string? _rev { get; set; }

    /// <summary>Gets or sets the package name (the npmjs <c>name</c> field).</summary>
    public string? name { get; set; }
}
