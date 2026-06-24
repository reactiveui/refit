// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Benchmarks;

/// <summary>A representative URL-encoded form payload used by the form serialization benchmark.</summary>
public sealed class FormBenchmarkModel
{
    /// <summary>Gets or sets the aliased first name.</summary>
    [AliasAs("first_name")]
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the aliased last name.</summary>
    [AliasAs("last_name")]
    public string? LastName { get; set; }

    /// <summary>Gets or sets the email.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the age.</summary>
    public int Age { get; set; }

    /// <summary>Gets or sets a note serialized even when null.</summary>
    [Query(SerializeNull = true)]
    public string? Note { get; set; }

    /// <summary>Gets the multi-value roles collection.</summary>
    [Query(CollectionFormat.Multi)]
    public List<string> Roles { get; } = [];
}
