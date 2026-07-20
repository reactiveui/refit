// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the emitter's identifier and type-name formatting primitives.</summary>
/// <remarks>These are invoked per emitted method, parameter, and HTTP verb, so their allocation profile scales with
/// interface size. Inputs are instance fields so the JIT cannot constant-fold them into the call.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class IdentifierEmissionBenchmarks
{
    /// <summary>A representative already-prefixed type name (the no-op fast path for <c>EnsureGlobalPrefix</c>).</summary>
    private readonly string _prefixedTypeName = "global::System.Net.Http.HttpClient";

    /// <summary>A representative unprefixed type name that must receive the global alias prefix.</summary>
    private readonly string _unprefixedTypeName = "System.Net.Http.HttpClient";

    /// <summary>A representative explicitly-implemented member name carrying an interface prefix.</summary>
    private readonly string _explicitMemberName = "global::MyNamespace.IMyInterface.MyMethod";

    /// <summary>A representative HTTP verb string.</summary>
    private readonly string _httpVerb = "GET";

    /// <summary>Adds the global alias prefix to an unprefixed type name.</summary>
    /// <returns>The prefixed type name.</returns>
    [Benchmark]
    public string EnsureGlobalPrefixUnprefixed() => Emitter.EnsureGlobalPrefix(_unprefixedTypeName);

    /// <summary>Returns an already-prefixed type name unchanged (fast path).</summary>
    /// <returns>The type name.</returns>
    [Benchmark]
    public string EnsureGlobalPrefixPrefixed() => Emitter.EnsureGlobalPrefix(_prefixedTypeName);

    /// <summary>Maps an HTTP verb to its generated <c>HttpMethod</c> expression.</summary>
    /// <returns>The generated expression.</returns>
    [Benchmark]
    public string HttpMethodExpression() => Emitter.ToHttpMethodExpression(_httpVerb);

    /// <summary>Strips the explicit-interface prefix from a member name.</summary>
    /// <returns>The bare member name.</returns>
    [Benchmark]
    public string StripExplicitPrefix() => Emitter.StripExplicitInterfacePrefix(_explicitMemberName);
}
