// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of the value equality and hashing of the internal cache-key and
/// request-shape structs: <c>CloseGenericMethodKey</c>, <c>MethodTableKey</c>, <c>ParameterFragment</c>,
/// <c>QueryMapEntry</c>, and <c>QueryParameterEntry</c>, plus the parameter-fragment factories.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class MethodKeyEqualityBenchmarks
{
    /// <summary>The argument index used to build a dynamic parameter fragment.</summary>
    private const int ArgumentIndex = 1;

    /// <summary>The property index used to build an object-property parameter fragment.</summary>
    private const int PropertyIndex = 2;

    /// <summary>The first type-argument array of the closed-generic key.</summary>
    private static readonly Type[] _closeTypesA = [typeof(int)];

    /// <summary>The second, equal-valued type-argument array of the closed-generic key.</summary>
    private static readonly Type[] _closeTypesB = [typeof(int)];

    /// <summary>The first parameter-type array of the method-table key.</summary>
    private static readonly Type[] _methodParametersA = [typeof(int), typeof(string)];

    /// <summary>The second, equal-valued parameter-type array of the method-table key.</summary>
    private static readonly Type[] _methodParametersB = [typeof(int), typeof(string)];

    /// <summary>The empty generic-argument array of the method-table key.</summary>
    private static readonly Type[] _methodGenerics = [];

    /// <summary>The argument index supplied to the parameter-fragment factories.</summary>
    private int _argumentIndex;

    /// <summary>The property index supplied to the parameter-fragment factories.</summary>
    private int _propertyIndex;

    /// <summary>The first closed-generic key.</summary>
    private CloseGenericMethodKey _closeKeyA;

    /// <summary>The second, equal closed-generic key.</summary>
    private CloseGenericMethodKey _closeKeyB;

    /// <summary>The first method-table key.</summary>
    private MethodTableKey _methodKeyA;

    /// <summary>The second, equal method-table key.</summary>
    private MethodTableKey _methodKeyB;

    /// <summary>The first parameter fragment.</summary>
    private ParameterFragment _fragmentA;

    /// <summary>The second, equal parameter fragment.</summary>
    private ParameterFragment _fragmentB;

    /// <summary>The first query-map entry.</summary>
    private QueryMapEntry _mapEntryA;

    /// <summary>The second, equal query-map entry.</summary>
    private QueryMapEntry _mapEntryB;

    /// <summary>The first query-parameter entry.</summary>
    private QueryParameterEntry _paramEntryA;

    /// <summary>The second, equal query-parameter entry.</summary>
    private QueryParameterEntry _paramEntryB;

    /// <summary>Builds the key and request-shape structs before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var method = ReflectionBenchmarkFixtures.Method(nameof(IReflectionRequestService.UserByIdAsync));
        _closeKeyA = new(method, _closeTypesA);
        _closeKeyB = new(method, _closeTypesB);

        _methodKeyA = new(nameof(IReflectionRequestService.UserByIdAsync), _methodParametersA, _methodGenerics);
        _methodKeyB = new(nameof(IReflectionRequestService.UserByIdAsync), _methodParametersB, _methodGenerics);

        _fragmentA = ParameterFragment.DynamicObject(ArgumentIndex, PropertyIndex);
        _fragmentB = ParameterFragment.DynamicObject(ArgumentIndex, PropertyIndex);

        _mapEntryA = new("category", "widgets");
        _mapEntryB = new("category", "widgets");

        _paramEntryA = new("page", "recent");
        _paramEntryB = new("page", "recent");

        _argumentIndex = ArgumentIndex;
        _propertyIndex = PropertyIndex;
    }

    /// <summary>Compares two closed-generic keys for equality.</summary>
    /// <returns><see langword="true"/> when the keys are equal.</returns>
    [Benchmark]
    public bool CloseGenericMethodKeyEquals() => _closeKeyA.Equals(_closeKeyB);

    /// <summary>Hashes a closed-generic key.</summary>
    /// <returns>The hash code.</returns>
    [Benchmark]
    public int CloseGenericMethodKeyGetHashCode() => _closeKeyA.GetHashCode();

    /// <summary>Compares two method-table keys for equality.</summary>
    /// <returns><see langword="true"/> when the keys are equal.</returns>
    [Benchmark]
    public bool MethodTableKeyEquals() => _methodKeyA.Equals(_methodKeyB);

    /// <summary>Hashes a method-table key.</summary>
    /// <returns>The hash code.</returns>
    [Benchmark]
    public int MethodTableKeyGetHashCode() => _methodKeyA.GetHashCode();

    /// <summary>Compares two parameter fragments for equality.</summary>
    /// <returns><see langword="true"/> when the fragments are equal.</returns>
    [Benchmark]
    public bool ParameterFragmentEquals() => _fragmentA.Equals(_fragmentB);

    /// <summary>Hashes a parameter fragment.</summary>
    /// <returns>The hash code.</returns>
    [Benchmark]
    public int ParameterFragmentGetHashCode() => _fragmentA.GetHashCode();

    /// <summary>Builds the three parameter-fragment shapes and reads their classification.</summary>
    /// <returns>The number of correctly classified fragment shapes.</returns>
    [Benchmark]
    public int ParameterFragmentFactories()
    {
        var constant = ParameterFragment.Constant("segment");
        var dynamic = ParameterFragment.Dynamic(_argumentIndex);
        var objectProperty = ParameterFragment.DynamicObject(_argumentIndex, _propertyIndex);
        return (constant.IsConstant ? 1 : 0)
            + (dynamic.IsDynamicRoute ? 1 : 0)
            + (objectProperty.IsObjectProperty ? 1 : 0);
    }

    /// <summary>Compares two query-map entries for equality.</summary>
    /// <returns><see langword="true"/> when the entries are equal.</returns>
    [Benchmark]
    public bool QueryMapEntryEquals() => _mapEntryA.Equals(_mapEntryB);

    /// <summary>Hashes a query-map entry.</summary>
    /// <returns>The hash code.</returns>
    [Benchmark]
    public int QueryMapEntryGetHashCode() => _mapEntryA.GetHashCode();

    /// <summary>Compares two query-parameter entries for equality.</summary>
    /// <returns><see langword="true"/> when the entries are equal.</returns>
    [Benchmark]
    public bool QueryParameterEntryEquals() => _paramEntryA.Equals(_paramEntryB);

    /// <summary>Hashes a query-parameter entry.</summary>
    /// <returns>The hash code.</returns>
    [Benchmark]
    public int QueryParameterEntryGetHashCode() => _paramEntryA.GetHashCode();
}
