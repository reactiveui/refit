// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the parser's assembly-scoped generated-name construction.</summary>
/// <remarks>The internal namespace and container name are built once per generation pass. They are not a per-method
/// hot path, but their <see cref="System.Text.StringBuilder"/>-per-segment shape is a candidate for allocation
/// reduction.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class NamespaceNormalizationBenchmarks
{
    /// <summary>A representative multi-segment consumer namespace prefix.</summary>
    private readonly string _consumerNamespace = "Contoso.Api.Clients.Generated";

    /// <summary>A representative compilation assembly name folded into the generated names.</summary>
    private readonly string _assemblyName = "Contoso.Api.Client";

    /// <summary>Builds the internal namespace from an absent prefix (default segment only).</summary>
    /// <returns>The normalized internal namespace.</returns>
    [Benchmark]
    public string BuildDefault() => Parser.BuildRefitInternalNamespace(null, _assemblyName);

    /// <summary>Builds the internal namespace from a multi-segment consumer prefix.</summary>
    /// <returns>The normalized internal namespace.</returns>
    [Benchmark]
    public string BuildFromPrefix() => Parser.BuildRefitInternalNamespace(_consumerNamespace, _assemblyName);

    /// <summary>Builds the assembly-scoped generated implementation container name.</summary>
    /// <returns>The container name.</returns>
    [Benchmark]
    public string BuildContainerName() => Parser.BuildGeneratedContainerName(_assemblyName);

    /// <summary>Reduces an assembly name to the identifier fragment folded into generated names.</summary>
    /// <returns>The sanitized assembly-scope fragment.</returns>
    [Benchmark]
    public string SanitizeAssembly() => Parser.SanitizeAssemblyScope(_assemblyName);
}
