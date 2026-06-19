// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Benchmarks;

/// <summary>
/// Allocation-profile benchmarks for the Refit source generator. Uses an EventPipe GC-verbose
/// trace, which captures real GC/allocation events and is more accurate than the sampling
/// MemoryDiagnoser. The exported *.speedscope.json (under BenchmarkDotNet.Artifacts/) can be
/// opened to inspect per-call-site allocations.
/// </summary>
[ShortRunJob]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class SourceGeneratorProfiledAllocBenchmarks
{
    /// <summary>The compilation used for the cold run.</summary>
    private Compilation _coldCompilation = null!;

    /// <summary>The generator driver used for the cold run.</summary>
    private CSharpGeneratorDriver _coldDriver = null!;

    /// <summary>The compilation used for the cached run.</summary>
    private Compilation _cachedCompilation = null!;

    /// <summary>The generator driver used for the cached run.</summary>
    private CSharpGeneratorDriver _cachedDriver = null!;

    /// <summary>Sets up the cold-run compilation and driver.</summary>
    [GlobalSetup(Target = nameof(CompileMany))]
    public void SetupCold() =>
        (_coldCompilation, _coldDriver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.ManyInterfaces);

    /// <summary>Runs the generator against the cold compilation.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CompileMany() =>
        _coldDriver.RunGeneratorsAndUpdateCompilation(_coldCompilation, out _, out _);

    /// <summary>Sets up the cached-run compilation and driver.</summary>
    [GlobalSetup(Target = nameof(CachedMany))]
    public void SetupCached() =>
        (_cachedCompilation, _cachedDriver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.ManyInterfaces);

    /// <summary>Runs the generator against the primed cached compilation.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CachedMany() =>
        _cachedDriver.RunGeneratorsAndUpdateCompilation(_cachedCompilation, out _, out _);
}
