// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>Compares the chained concatenation and interpolated-string forms emitted for nested query-object keys.</summary>
/// <remarks>The inputs mirror generated code: the parent key is known only at runtime, while the delimiter, property
/// prefix, alias, and CLR property name are compile-time literals.</remarks>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class NestedQueryKeyCompositionBenchmarks
{
    /// <summary>The compile-time delimiter and property prefix emitted before a nested property name.</summary>
    private const string NestedPrefix = ".billing-";

    /// <summary>The compile-time CLR property name passed to the key formatter.</summary>
    private const string PropertyName = "PostalCode";

    /// <summary>The runtime key of the enclosing query object.</summary>
    private readonly string _parentKey = "customer";

    /// <summary>Settings used by the key-formatter path.</summary>
    private readonly RefitSettings _settings = new();

    /// <summary>Builds an aliased nested key using the current generated concatenation.</summary>
    /// <returns>The composed query key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Alias")]
    [SuppressMessage("Roslynator", "RCS1190", Justification = "The benchmark intentionally measures the generated concatenation shape.")]
    [SuppressMessage("Style", "SST2249", Justification = "The benchmark intentionally measures the generated concatenation shape.")]
    public string ConcatenationAlias() => _parentKey + NestedPrefix + "postal_code";

    /// <summary>Builds an aliased nested key using the proposed generated interpolation.</summary>
    /// <returns>The composed query key.</returns>
    [Benchmark]
    [BenchmarkCategory("Alias")]
    public string InterpolationAlias() => $"{_parentKey}.billing-postal_code";

    /// <summary>Builds a pre-escaped nested CLR-name key using the current generated concatenation.</summary>
    /// <returns>The composed query key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("PreEscaped")]
    [SuppressMessage("Roslynator", "RCS1190", Justification = "The benchmark intentionally measures the generated concatenation shape.")]
    [SuppressMessage("Style", "SST2249", Justification = "The benchmark intentionally measures the generated concatenation shape.")]
    public string ConcatenationPreEscaped() => _parentKey + NestedPrefix + PropertyName;

    /// <summary>Builds a pre-escaped nested CLR-name key using the proposed generated interpolation.</summary>
    /// <returns>The composed query key.</returns>
    [Benchmark]
    [BenchmarkCategory("PreEscaped")]
    public string InterpolationPreEscaped() => $"{_parentKey}.billing-PostalCode";

    /// <summary>Builds the nested prefix passed to the key formatter using the current generated concatenation.</summary>
    /// <returns>The formatted query key.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Formatted")]
    [SuppressMessage("Style", "SST2249", Justification = "The benchmark intentionally measures the generated concatenation shape.")]
    public string ConcatenationFormatted() =>
        GeneratedRequestRunner.BuildQueryKey(_settings, PropertyName, null, _parentKey + NestedPrefix);

    /// <summary>Builds the nested prefix passed to the key formatter using the proposed generated interpolation.</summary>
    /// <returns>The formatted query key.</returns>
    [Benchmark]
    [BenchmarkCategory("Formatted")]
    public string InterpolationFormatted() =>
        GeneratedRequestRunner.BuildQueryKey(_settings, PropertyName, null, $"{_parentKey}.billing-");
}
