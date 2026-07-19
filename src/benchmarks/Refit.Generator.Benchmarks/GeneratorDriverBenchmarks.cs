// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator.Benchmarks;

/// <summary>End-to-end generator throughput through <see cref="CSharpGeneratorDriver"/> for cold and incremental runs.</summary>
/// <remarks>Cold is a first-time run from a fresh driver; incremental re-runs a primed driver after an unrelated edit,
/// which is what the IDE pays per keystroke. CPU sampling attributes wall-clock to parser vs emitter vs Roslyn driver.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class GeneratorDriverBenchmarks
{
    /// <summary>The compilation the cold driver runs against.</summary>
    private Compilation _coldCompilation = null!;

    /// <summary>The fresh driver used for the cold run.</summary>
    private CSharpGeneratorDriver _coldDriver = null!;

    /// <summary>The compilation the primed driver re-runs against after an unrelated edit.</summary>
    private Compilation _incrementalCompilation = null!;

    /// <summary>The primed driver used for the incremental run.</summary>
    private CSharpGeneratorDriver _incrementalDriver = null!;

    /// <summary>Gets or sets the corpus size under benchmark.</summary>
    [ParamsAllValues]
    public CorpusSize Size { get; set; }

    /// <summary>Builds the cold and primed incremental driver states for the current corpus size.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var source = GeneratorCorpus.SourceFor(Size);

        var cold = GeneratorHarness.CreateColdState(source);
        _coldCompilation = cold.Compilation;
        _coldDriver = cold.Driver;

        var primed = GeneratorHarness.CreatePrimedState(source);
        _incrementalCompilation = primed.Compilation;
        _incrementalDriver = primed.Driver;
    }

    /// <summary>Runs the generator from a cold driver.</summary>
    /// <returns>The updated generator driver.</returns>
    [Benchmark]
    public GeneratorDriver Cold() =>
        _coldDriver.RunGeneratorsAndUpdateCompilation(_coldCompilation, out _, out _);

    /// <summary>Re-runs a primed driver after an unrelated edit, exercising the incremental cache.</summary>
    /// <returns>The updated generator driver.</returns>
    [Benchmark]
    public GeneratorDriver Incremental() =>
        _incrementalDriver.RunGeneratorsAndUpdateCompilation(_incrementalCompilation, out _, out _);
}
