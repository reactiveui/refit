// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Benchmarks;

/// <summary>Benchmarks measuring the source generator throughput for cold and cached runs.</summary>
[MemoryDiagnoser]
public class SourceGeneratorBenchmark
{
    /// <summary>The compilation that the generator runs against.</summary>
    private Compilation _compilation = null!;

    /// <summary>The generator driver used to run the generators.</summary>
    private CSharpGeneratorDriver _driver = null!;

    /// <summary>Sets up the compilation and driver for the small-interface compile benchmark.</summary>
    [GlobalSetup(Target = nameof(Compile))]
    public void SetupSmall() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.SmallInterface);

    /// <summary>Runs the generator against the small interface from a cold state.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver Compile() => _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);

    /// <summary>Sets up the compilation and driver for the small-interface cached benchmark.</summary>
    [GlobalSetup(Target = nameof(Cached))]
    public void SetupCached() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.SmallInterface);

    /// <summary>Runs the generator against the small interface from a primed cached state.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver Cached() => _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);

    /// <summary>Sets up the compilation and driver for the many-interfaces compile benchmark.</summary>
    [GlobalSetup(Target = nameof(CompileMany))]
    public void SetupMany() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.ManyInterfaces);

    /// <summary>Runs the generator against many interfaces from a cold state.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CompileMany() => _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);

    /// <summary>Sets up the compilation and driver for the many-interfaces cached benchmark.</summary>
    [GlobalSetup(Target = nameof(CachedMany))]
    public void SetupCachedMany() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.ManyInterfaces);

    /// <summary>Runs the generator against many interfaces from a primed cached state.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CachedMany() => _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);

    /// <summary>Prepares the query-heavy compilation for a cold generator run.</summary>
    [GlobalSetup(Target = nameof(CompileQueryHeavy))]
    public void SetupQueryHeavy() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.QueryHeavyInterface);

    /// <summary>Benchmarks a cold generator run over the query-heavy interface.</summary>
    /// <returns>The updated generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CompileQueryHeavy() => _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);

    /// <summary>Prepares the query-heavy compilation for a cached generator run.</summary>
    [GlobalSetup(Target = nameof(CachedQueryHeavy))]
    public void SetupCachedQueryHeavy() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.QueryHeavyInterface);

    /// <summary>Benchmarks a cached generator run over the query-heavy interface.</summary>
    /// <returns>The updated generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CachedQueryHeavy() => _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);
}
