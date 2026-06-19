// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Refit.Tests;

/// <summary>A test object exercising complex query parameter serialization.</summary>
public class ComplexQueryObject
{
    /// <summary>Gets or sets the first aliased test value.</summary>
    [AliasAs("test-query-alias")]
    public string? TestAlias1 { get; set; }

    /// <summary>Gets or sets the second test value.</summary>
    public string? TestAlias2 { get; set; }

    /// <summary>Gets or sets the test collection.</summary>
    public IEnumerable<int>? TestCollection { get; set; }

    /// <summary>Gets or sets the aliased dictionary.</summary>
    [AliasAs("test-dictionary-alias")]
    public Dictionary<TestEnum, string>? TestAliasedDictionary { get; init; }

    /// <summary>Gets or sets the test dictionary.</summary>
    public Dictionary<TestEnum, string>? TestDictionary { get; init; }

    /// <summary>Gets or sets the multi-formatted enum collection.</summary>
    [AliasAs("listOfEnumMulti")]
    [Query(CollectionFormat.Multi)]
    public List<TestEnum>? EnumCollectionMulti { get; init; }

    /// <summary>Gets or sets the multi-formatted object collection.</summary>
    [Query(CollectionFormat.Multi)]
    public List<object>? ObjectCollectionMulti { get; init; }

    /// <summary>Gets or sets the CSV-formatted enum collection.</summary>
    [Query(CollectionFormat.Csv)]
    public List<TestEnum>? EnumCollectionCsv { get; init; }

    /// <summary>Gets or sets the CSV-formatted object collection.</summary>
    [AliasAs("listOfObjectsCsv")]
    [Query(CollectionFormat.Csv)]
    public List<object>? ObjectCollectionCcv { get; init; }

    /// <summary>Gets or sets a value ignored via IgnoreDataMember.</summary>
    [IgnoreDataMember]
    public string? InternalUseOnlyIgnoredByDataMember { get; set; }

    /// <summary>Gets or sets a value ignored via System.Text.Json JsonIgnore.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? InternalUseOnlyIgnoredBySystemTextJson { get; set; }
}
