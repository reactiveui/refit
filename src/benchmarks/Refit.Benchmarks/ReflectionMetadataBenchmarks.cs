// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation micro-benchmarks for the runtime reflection metadata helpers: <c>ReflectionPropertyHelpers</c> readable
/// property enumeration (all-readable fast path and the filtered slow path) and <see cref="GeneratedParameterAttributeProvider"/>
/// attribute flattening and lookups (uncached first access, memoized access, and type-filtered access).
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ReflectionMetadataBenchmarks
{
    /// <summary>The per-type attribute arrays supplied to a parameter attribute provider.</summary>
    private readonly Dictionary<Type, object[]> _attributes = new()
    {
        [typeof(QueryAttribute)] = [new QueryAttribute()],
        [typeof(EncodedAttribute)] = [new EncodedAttribute()],
        [typeof(AliasAsAttribute)] = [new AliasAsAttribute("field")],
    };

    /// <summary>The all-readable type enumerated by the fast-path benchmark.</summary>
    private readonly Type _allReadableType = typeof(User);

    /// <summary>The mixed-readability type enumerated by the filtered-path benchmark.</summary>
    private readonly Type _filteredType = typeof(PartiallyReadableModel);

    /// <summary>A pre-built provider whose attributes are already flattened, used by the memoized-access benchmark.</summary>
    private GeneratedParameterAttributeProvider _cachedProvider = null!;

    /// <summary>Warms the memoized provider before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _cachedProvider = new(_attributes);
        _ = _cachedProvider.GetCustomAttributes(true);
    }

    /// <summary>Enumerates the readable public properties of an all-readable type (fast path).</summary>
    /// <returns>The readable property count.</returns>
    [Benchmark]
    [BenchmarkCategory("Properties")]
    public int GetReadablePropertiesFastPath() =>
        ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(_allReadableType).Length;

    /// <summary>Enumerates the readable public properties of a mixed-readability type (filtered path).</summary>
    /// <returns>The readable property count.</returns>
    [Benchmark]
    [BenchmarkCategory("Properties")]
    public int GetReadablePropertiesFiltered() =>
        ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(_filteredType).Length;

    /// <summary>Flattens the per-type attribute arrays into a single array.</summary>
    /// <returns>The flattened attribute count.</returns>
    [Benchmark]
    [BenchmarkCategory("Attributes")]
    public int FlattenAttributes() => GeneratedParameterAttributeProvider.FlattenAttributes(_attributes).Length;

    /// <summary>Flattens all attributes on first access through a fresh provider (uncached).</summary>
    /// <returns>The flattened attribute count.</returns>
    [Benchmark]
    [BenchmarkCategory("Attributes")]
    public int GetAllAttributesUncached() => new GeneratedParameterAttributeProvider(_attributes).GetCustomAttributes(true).Length;

    /// <summary>Returns the memoized flattened attributes on a warmed provider.</summary>
    /// <returns>The flattened attribute count.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Attributes")]
    public int GetAllAttributesCached() => _cachedProvider.GetCustomAttributes(true).Length;

    /// <summary>Returns the attributes of a single requested type (dictionary lookup).</summary>
    /// <returns>The matching attribute count.</returns>
    [Benchmark]
    [BenchmarkCategory("Attributes")]
    public int GetAttributesByType() => _cachedProvider.GetCustomAttributes(typeof(QueryAttribute), true).Length;
}
