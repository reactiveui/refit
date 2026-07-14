// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Refit.Tests;

/// <summary>A sealed query object exercising every flattening rule the generator implements.</summary>
public sealed class SealedQueryObject
{
    /// <summary>Gets or sets a plain scalar rendered under its CLR name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets a scalar renamed by an alias, which bypasses the key formatter.</summary>
    [AliasAs("n")]
    public int Number { get; set; }

    /// <summary>Gets or sets a scalar rendered through the form-url-encoded formatter's format string.</summary>
    [Query(Format = "0.00")]
    public double Price { get; set; }

    /// <summary>Gets or sets a null-valued scalar that opts in to being emitted as a bare <c>key=</c>.</summary>
    [Query(SerializeNull = true)]
    public string? Kept { get; set; }

    /// <summary>Gets or sets a null-valued scalar that is omitted from the query string.</summary>
    public string? Skipped { get; set; }

    /// <summary>Gets or sets an enum rendered through its <see cref="EnumMemberAttribute"/> value.</summary>
    public QuerySort Sort { get; set; }

    /// <summary>Gets or sets a property excluded from the query string by an ignore attribute.</summary>
    [IgnoreDataMember]
    public string? Hidden { get; set; }

    /// <summary>Gets or sets a non-public property, which has no public getter and so is never flattened.</summary>
    internal string? Secret { get; set; }
}
