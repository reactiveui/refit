// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Compares the two shapes a generated parameter's cached attribute provider can take: the keyed dictionary used when a
/// parameter carries attributes of more than one type, and the flat array used when every attribute has the same type.
/// Construction is what a generated static field pays once; the query benchmarks are what a request pays whenever a
/// non-default URL parameter formatter forces the formatter path.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ParameterAttributeProviderBenchmarks
{
    /// <summary>The attribute type the construction benchmarks build a provider for.</summary>
    private readonly Type _attributeType = typeof(QueryAttribute);

    /// <summary>The single-type provider for a parameter declaring one <c>[Query]</c> attribute.</summary>
    private GeneratedSingleTypeParameterAttributeProvider _single = null!;

    /// <summary>The dictionary-backed provider for the same parameter.</summary>
    private GeneratedParameterAttributeProvider _many = null!;

    /// <summary>The single-type provider for a parameter whose only attribute is not the one being looked up.</summary>
    private GeneratedSingleTypeParameterAttributeProvider _singleMiss = null!;

    /// <summary>The dictionary-backed provider for the same non-matching parameter.</summary>
    private GeneratedParameterAttributeProvider _manyMiss = null!;

    /// <summary>Builds the providers compared by the lookup benchmarks.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _single = new(typeof(QueryAttribute), [new QueryAttribute()]);
        _many = new(new Dictionary<Type, object[]> { [typeof(QueryAttribute)] = [new QueryAttribute()], });
        _singleMiss = new(typeof(AliasAsAttribute), [new AliasAsAttribute("field")]);
        _manyMiss = new(new Dictionary<Type, object[]> { [typeof(AliasAsAttribute)] = [new AliasAsAttribute("field")], });
    }

    /// <summary>Builds the single-type provider a generated static field initializes.</summary>
    /// <returns>The constructed provider.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Construction")]
    public ICustomAttributeProvider CreateSingleTypeProvider() =>
        new GeneratedSingleTypeParameterAttributeProvider(_attributeType, [new QueryAttribute()]);

    /// <summary>Builds the dictionary-backed provider for the same one-attribute parameter.</summary>
    /// <returns>The constructed provider.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Construction")]
    public ICustomAttributeProvider CreateManyTypeProvider() =>
        new GeneratedParameterAttributeProvider(new Dictionary<Type, object[]> { [_attributeType] = [new QueryAttribute()], });

    /// <summary>Resolves the query attribute through the single-type provider.</summary>
    /// <returns>The resolved attribute.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("QueryAttributeHit")]
    public QueryAttribute? ResolveQueryAttributeFromSingleType() =>
        DefaultUrlParameterFormatter.GetFirstQueryAttribute(_single);

    /// <summary>Resolves the query attribute through the dictionary-backed provider.</summary>
    /// <returns>The resolved attribute.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("QueryAttributeHit")]
    public QueryAttribute? ResolveQueryAttributeFromManyType() =>
        DefaultUrlParameterFormatter.GetFirstQueryAttribute(_many);

    /// <summary>Probes for an absent query attribute through the single-type provider.</summary>
    /// <returns>The resolved attribute, which is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("QueryAttributeMiss")]
    public QueryAttribute? ProbeMissingQueryAttributeOnSingleType() =>
        DefaultUrlParameterFormatter.GetFirstQueryAttribute(_singleMiss);

    /// <summary>Probes for an absent query attribute through the dictionary-backed provider.</summary>
    /// <returns>The resolved attribute, which is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("QueryAttributeMiss")]
    public QueryAttribute? ProbeMissingQueryAttributeOnManyType() =>
        DefaultUrlParameterFormatter.GetFirstQueryAttribute(_manyMiss);

    /// <summary>Reads every attribute from the single-type provider.</summary>
    /// <returns>The attribute count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("AllAttributes")]
    public int GetAllAttributesFromSingleType() => _single.GetCustomAttributes(true).Length;

    /// <summary>Reads every attribute from the dictionary-backed provider, which memoizes the flattened array.</summary>
    /// <returns>The attribute count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AllAttributes")]
    public int GetAllAttributesFromManyType() => _many.GetCustomAttributes(true).Length;
}
