// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A query object exercising null serialization and custom property formatting.</summary>
public class QuerySerializationObject
{
    /// <summary>Gets or sets a property that is emitted even when its value is null.</summary>
    [Query(SerializeNull = true)]
    public string? SerializedNull { get; set; }

    /// <summary>Gets or sets a property whose value is rendered by the form-url-encoded formatter.</summary>
    [Query(Format = "custom")]
    public string? Formatted { get; set; }
}
