// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Reflection.Benchmarks;

/// <summary>
/// EventPipe GC-verbose allocation profile of return-type adapter resolution: matching a return type against
/// registered adapters (closed, generic-definition, and no-match), resolving the closed adapter type to instantiate,
/// and mapping a wrapper's type arguments onto the adapter's type parameters.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ReturnTypeAdapterResolverBenchmarks
{
    /// <summary>The registered open generic adapter definitions.</summary>
    private readonly List<Type> _genericAdapters = [typeof(ReflectionResultAdapter<>)];

    /// <summary>The registered closed adapter types.</summary>
    private readonly List<Type> _closedAdapters = [typeof(ReflectionClosedResultAdapter)];

    /// <summary>The return type a registered adapter surfaces.</summary>
    private readonly Type _matchingReturnType = typeof(ReflectionResult<string>);

    /// <summary>A return type no registered adapter surfaces.</summary>
    private readonly Type _nonMatchingReturnType = typeof(Task<string>);

    /// <summary>The open generic adapter definition matched directly.</summary>
    private readonly Type _genericAdapterDefinition = typeof(ReflectionResultAdapter<>);

    /// <summary>The adapter's declared open-constructed wrapper return type.</summary>
    private Type _templateReturn = null!;

    /// <summary>Resolves the adapter's template return type before the benchmarks run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var adapterInterface = Array.Find(
            _genericAdapterDefinition.GetInterfaces(),
            static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReturnTypeAdapter<,>))!;
        _templateReturn = adapterInterface.GetGenericArguments()[0];
    }

    /// <summary>Resolves the result type through a registered generic adapter definition.</summary>
    /// <returns><see langword="true"/> when a registered adapter surfaces the return type.</returns>
    [Benchmark]
    public bool TryResolveResultTypeGeneric() =>
        ReturnTypeAdapterResolver.TryResolveResultType(_matchingReturnType, _genericAdapters, out _);

    /// <summary>Resolves the result type through a registered closed adapter.</summary>
    /// <returns><see langword="true"/> when a registered adapter surfaces the return type.</returns>
    [Benchmark]
    public bool TryResolveResultTypeClosed() =>
        ReturnTypeAdapterResolver.TryResolveResultType(_matchingReturnType, _closedAdapters, out _);

    /// <summary>Scans the registered adapters for a return type none surface.</summary>
    /// <returns><see langword="true"/> when a registered adapter surfaces the return type.</returns>
    [Benchmark]
    public bool TryResolveResultTypeNoMatch() =>
        ReturnTypeAdapterResolver.TryResolveResultType(_nonMatchingReturnType, _genericAdapters, out _);

    /// <summary>Resolves the closed adapter type to instantiate, closing the generic definition.</summary>
    /// <returns>The closed adapter type.</returns>
    [Benchmark]
    public Type? ResolveClosedAdapterTypeGeneric() =>
        ReturnTypeAdapterResolver.ResolveClosedAdapterType(_matchingReturnType, _genericAdapters);

    /// <summary>Matches an open generic adapter definition against the return type.</summary>
    /// <returns><see langword="true"/> when the adapter surfaces the return type.</returns>
    [Benchmark]
    public bool TryMatchGenericDefinition() =>
        ReturnTypeAdapterResolver.TryMatchGenericDefinition(_matchingReturnType, _genericAdapterDefinition, out _, out _);

    /// <summary>Maps the wrapper return type's arguments onto the adapter's type parameters.</summary>
    /// <returns><see langword="true"/> when every adapter type parameter binds consistently.</returns>
    [Benchmark]
    public bool TryMapTypeArguments() =>
        ReturnTypeAdapterResolver.TryMapTypeArguments(_templateReturn, _matchingReturnType, out _);
}
