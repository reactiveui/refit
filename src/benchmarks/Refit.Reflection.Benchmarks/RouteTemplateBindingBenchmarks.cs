// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of route-template binding: expanding a path template into the parameter
/// map and ordered URL fragments across route shapes, building the direct and nested-object validation lookups,
/// resolving a dotted <c>{a.b.c}</c> chain, and combining a client path prefix.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class RouteTemplateBindingBenchmarks
{
    /// <summary>The path template for a single dynamic segment.</summary>
    private const string SingleSegmentPath = "/users/{id}";

    /// <summary>The path template for several dynamic segments.</summary>
    private const string MultiSegmentPath = "/users/{id}/{group}/{status}";

    /// <summary>The path template for an object-property segment.</summary>
    private const string ObjectPropertyPath = "/users/{request.id}/detail";

    /// <summary>The path template for a nested object-property chain.</summary>
    private const string NestedPropertyPath = "/orgs/{request.inner.code}/audit";

    /// <summary>The mapped parameters of the single-segment method.</summary>
    private ParameterInfo[] _singleSegmentParameters = null!;

    /// <summary>The mapped parameters of the multi-segment method.</summary>
    private ParameterInfo[] _multiSegmentParameters = null!;

    /// <summary>The mapped parameters of the object-property method.</summary>
    private ParameterInfo[] _objectPropertyParameters = null!;

    /// <summary>The mapped parameters of the nested-property method.</summary>
    private ParameterInfo[] _nestedPropertyParameters = null!;

    /// <summary>The client path prefix combined with a method path template.</summary>
    private string _clientPathPrefix = null!;

    /// <summary>The method path template combined with the client path prefix.</summary>
    private string _methodPathTemplate = null!;

    /// <summary>Prepares the mapped parameters before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _singleSegmentParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.UserByIdAsync));
        _multiSegmentParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.MultiSegmentAsync));
        _objectPropertyParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.ObjectPathAsync));
        _nestedPropertyParameters = ReflectionBenchmarkFixtures.MappedParameters(nameof(IReflectionRequestService.NestedPathAsync));
        _clientPathPrefix = "/api/v2/";
        _methodPathTemplate = "/users/{id}";
    }

    /// <summary>Binds a single dynamic route segment.</summary>
    /// <returns>The combined map and fragment count, returned to retain the built result.</returns>
    [Benchmark]
    public int BuildParameterMapSingleSegment()
    {
        var (map, fragments) = RestMethodInfoInternal.BuildParameterMap(SingleSegmentPath, _singleSegmentParameters, false);
        return map.Count + fragments.Count;
    }

    /// <summary>Binds several dynamic route segments.</summary>
    /// <returns>The combined map and fragment count, returned to retain the built result.</returns>
    [Benchmark]
    public int BuildParameterMapMultiSegment()
    {
        var (map, fragments) = RestMethodInfoInternal.BuildParameterMap(MultiSegmentPath, _multiSegmentParameters, false);
        return map.Count + fragments.Count;
    }

    /// <summary>Binds an object-property route segment.</summary>
    /// <returns>The combined map and fragment count, returned to retain the built result.</returns>
    [Benchmark]
    public int BuildParameterMapObjectProperty()
    {
        var (map, fragments) = RestMethodInfoInternal.BuildParameterMap(ObjectPropertyPath, _objectPropertyParameters, false);
        return map.Count + fragments.Count;
    }

    /// <summary>Binds a nested object-property route chain.</summary>
    /// <returns>The combined map and fragment count, returned to retain the built result.</returns>
    [Benchmark]
    public int BuildParameterMapNestedProperty()
    {
        var (map, fragments) = RestMethodInfoInternal.BuildParameterMap(NestedPropertyPath, _nestedPropertyParameters, false);
        return map.Count + fragments.Count;
    }

    /// <summary>Builds the direct URL-name-to-parameter validation lookup.</summary>
    /// <returns>The parameter validation lookup.</returns>
    [Benchmark]
    public Dictionary<string, ParameterInfo> BuildParamValidationDict() =>
        RestMethodInfoInternal.BuildParamValidationDict(_multiSegmentParameters);

    /// <summary>Builds the nested object-property validation lookup.</summary>
    /// <returns>The object-property validation lookup.</returns>
    [Benchmark]
    public Dictionary<string, (ParameterInfo Parameter, PropertyInfo Property)> BuildObjectParamValidationDict() =>
        RestMethodInfoInternal.BuildObjectParamValidationDict(_objectPropertyParameters);

    /// <summary>Resolves a dotted <c>{a.b.c}</c> placeholder into its parameter and property chain.</summary>
    /// <returns>The resolved parameter and property chain.</returns>
    [Benchmark]
    public (ParameterInfo Parameter, IReadOnlyList<PropertyInfo> Chain)? TryResolveNestedPropertyChain() =>
        RestMethodInfoInternal.TryResolveNestedPropertyChain(_nestedPropertyParameters, "request.inner.code");

    /// <summary>Combines a client path prefix with a method path template.</summary>
    /// <returns>The combined path.</returns>
    [Benchmark]
    public string CombineWithPathPrefix() =>
        RestMethodInfoInternal.CombineWithPathPrefix(_clientPathPrefix, _methodPathTemplate);
}
