// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the parser's internal-namespace construction.</summary>
/// <remarks>The internal namespace is built once per generation pass. It is not a per-method hot path, but its
/// <see cref="System.Text.StringBuilder"/>-per-segment shape is a candidate for allocation reduction.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class NamespaceNormalizationBenchmarks
{
    /// <summary>An absent consumer prefix, exercising the default-segment-only path.</summary>
    private readonly string? _defaultPrefix;

    /// <summary>A representative multi-segment consumer namespace prefix.</summary>
    private readonly string _consumerNamespace = "Contoso.Api.Clients.Generated";

    /// <summary>Builds the internal namespace from an empty prefix (default segment only).</summary>
    /// <returns>The normalized internal namespace.</returns>
    [Benchmark]
    public string BuildDefault() => Parser.BuildRefitInternalNamespace(_defaultPrefix);

    /// <summary>Builds the internal namespace from a multi-segment consumer prefix.</summary>
    /// <returns>The normalized internal namespace.</returns>
    [Benchmark]
    public string BuildFromPrefix() => Parser.BuildRefitInternalNamespace(_consumerNamespace);
}
