// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Benchmarks;

/// <summary>EventPipe CPU-sampling profile of the Refit source generator (where parse/emit time is spent).</summary>
[ShortRunJob]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class SourceGeneratorProfiledCpuBenchmarks
{
    /// <summary>The compilation under benchmark.</summary>
    private Compilation _compilation = null!;

    /// <summary>The generator driver under benchmark.</summary>
    private CSharpGeneratorDriver _driver = null!;

    /// <summary>Sets up the compilation and driver.</summary>
    [GlobalSetup]
    public void Setup() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.ManyInterfaces);

    /// <summary>Runs the generator against the compilation.</summary>
    /// <returns>The resulting generator driver.</returns>
    [Benchmark]
    public GeneratorDriver CompileMany() =>
        _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);
}
