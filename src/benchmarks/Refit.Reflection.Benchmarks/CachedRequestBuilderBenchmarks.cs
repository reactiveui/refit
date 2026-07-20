// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of the caching request-builder wrapper's cache-hit path: forming the
/// method-table key and returning the already-built delegate for a parameterless, a parameter-typed, and a generic
/// method lookup.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class CachedRequestBuilderBenchmarks
{
    /// <summary>The parameter types of the dynamic-route method.</summary>
    private static readonly Type[] _intParameters = [typeof(int)];

    /// <summary>The parameter types of the closed generic method.</summary>
    private static readonly Type[] _stringParameters = [typeof(string)];

    /// <summary>The generic argument types closing the generic method.</summary>
    private static readonly Type[] _genericArguments = [typeof(string)];

    /// <summary>The caching request builder with a primed cache.</summary>
    private CachedRequestBuilderImplementation _cached = null!;

    /// <summary>Builds the caching wrapper and primes its cache before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var settings = ReflectionBenchmarkFixtures.CreateSettings();
        var inner = new RequestBuilderImplementation(typeof(IReflectionRequestService), settings);
        _cached = new(inner);

        // Prime the cache so the benchmarks exercise the cache-hit path rather than the first-build path.
        _ = _cached.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.ConstantRouteAsync));
        _ = _cached.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.UserByIdAsync), _intParameters);
        _ = _cached.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.TypedItemAsync), _stringParameters, _genericArguments);
    }

    /// <summary>Returns the cached delegate for a parameterless method.</summary>
    /// <returns>The cached result delegate.</returns>
    [Benchmark]
    public Func<HttpClient, object[], object?> CacheHitParameterless() =>
        _cached.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.ConstantRouteAsync));

    /// <summary>Returns the cached delegate for a method keyed by parameter types.</summary>
    /// <returns>The cached result delegate.</returns>
    [Benchmark]
    public Func<HttpClient, object[], object?> CacheHitParameterTypes() =>
        _cached.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.UserByIdAsync), _intParameters);

    /// <summary>Returns the cached delegate for a generic method keyed by parameter and generic argument types.</summary>
    /// <returns>The cached result delegate.</returns>
    [Benchmark]
    public Func<HttpClient, object[], object?> CacheHitGeneric() =>
        _cached.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.TypedItemAsync), _stringParameters, _genericArguments);
}
