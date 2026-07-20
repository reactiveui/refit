// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Refit.Reflection.Tests;

/// <summary>A query model combining a scalar, an aliased scalar, an ignored property, a multi-expanded collection and a
/// nested object, exercising every branch of the reflection request builder's per-type query-property metadata.</summary>
public sealed class ReflectionCachingQueryModel
{
    /// <summary>Gets or sets a plain scalar rendered under its CLR name.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets a scalar renamed by an alias, which bypasses the key formatter.</summary>
    [AliasAs("handle")]
    public string? Name { get; set; }

    /// <summary>Gets or sets a second plain scalar.</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets a property excluded from the query string by an ignore attribute.</summary>
    [IgnoreDataMember]
    public string? Ignored { get; set; }

    /// <summary>Gets or sets a multi-expanded collection flattened to one repeated key per element.</summary>
    [Query(CollectionFormat.Multi)]
    public int[]? Tags { get; set; }

    /// <summary>Gets or sets the nested model flattened recursively under a dotted key.</summary>
    public ReflectionCachingInnerModel? Inner { get; set; }
}
