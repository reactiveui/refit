// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of building the reflection request builder and its per-method delegates:
/// discovering every interface method (constructor), resolving a method by name and parameter types, filtering
/// overload candidates, closing a generic method, resolving a private delegate factory, and building a result delegate.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ReflectionRequestBuilderConstructionBenchmarks
{
    /// <summary>The single-parameter types used to resolve and filter the dynamic-route method.</summary>
    private static readonly Type[] _singleIntParameter = [typeof(int)];

    /// <summary>The generic argument types used to close the generic method.</summary>
    private static readonly Type[] _genericArguments = [typeof(string)];

    /// <summary>The settings used across the benchmarks.</summary>
    private RefitSettings _settings = null!;

    /// <summary>A prebuilt request builder used by the per-method resolution and delegate benchmarks.</summary>
    private RequestBuilderImplementation _builder = null!;

    /// <summary>The open generic method metadata closed by the generic-closing benchmark.</summary>
    private RestMethodInfoInternal _genericMethod = null!;

    /// <summary>The candidate method list filtered by the overload-filtering benchmark.</summary>
    private List<RestMethodInfoInternal> _candidateMethods = null!;

    /// <summary>Prepares the settings, builder and method metadata before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _settings = ReflectionBenchmarkFixtures.CreateSettings();
        _builder = new(typeof(IReflectionRequestService), _settings);
        _genericMethod = ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.TypedItemAsync), _settings);
        _candidateMethods =
        [
            ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.UserByIdAsync), _settings),
            ReflectionBenchmarkFixtures.Build(nameof(IReflectionRequestService.ScalarQueryAsync), _settings),
        ];
    }

    /// <summary>Constructs the request builder, parsing metadata for every interface method.</summary>
    /// <returns>The constructed request builder.</returns>
    [Benchmark]
    public object ConstructBuilder() => new RequestBuilderImplementation(typeof(IReflectionRequestService), _settings);

    /// <summary>Resolves a method by name and parameter types.</summary>
    /// <returns>The resolved method metadata.</returns>
    [Benchmark]
    public object FindMatchingRestMethodInfo() =>
        _builder.FindMatchingRestMethodInfo(nameof(IReflectionRequestService.UserByIdAsync), _singleIntParameter, null);

    /// <summary>Filters the candidate methods by parameter count and generic arity.</summary>
    /// <returns>The number of matching candidates, returned to retain the filtered array.</returns>
    [Benchmark]
    public int FilterPossibleMethods() =>
        RequestBuilderImplementation.FilterPossibleMethods(_candidateMethods, _singleIntParameter, null).Length;

    /// <summary>Closes the generic method over a concrete type argument.</summary>
    /// <returns>The closed method metadata.</returns>
    [Benchmark]
    public object CloseGenericMethodIfNeeded() =>
        _builder.CloseGenericMethodIfNeeded(_genericMethod, _genericArguments);

    /// <summary>Resolves a private delegate factory method by name.</summary>
    /// <returns>The resolved factory method.</returns>
    [Benchmark]
    public object FindDeclaredMethod() =>
        RequestBuilderImplementation.FindDeclaredMethod(nameof(RequestBuilderImplementation.BuildTaskFuncForMethod));

    /// <summary>Builds a result delegate for a dynamic-route method.</summary>
    /// <returns>The built result delegate.</returns>
    [Benchmark]
    public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod() =>
        _builder.BuildRestResultFuncForMethod(nameof(IReflectionRequestService.UserByIdAsync));
}
