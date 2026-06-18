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
    private Compilation _coldCompilation = null!;
    private CSharpGeneratorDriver _coldDriver = null!;
    private Compilation _cachedCompilation = null!;
    private CSharpGeneratorDriver _cachedDriver = null!;

    [GlobalSetup(Target = nameof(CompileMany))]
    public void SetupCold() =>
        (_coldCompilation, _coldDriver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.ManyInterfaces
        );

    [Benchmark]
    public GeneratorDriver CompileMany() =>
        _coldDriver.RunGeneratorsAndUpdateCompilation(_coldCompilation, out _, out _);

    [GlobalSetup(Target = nameof(CachedMany))]
    public void SetupCached() =>
        (_cachedCompilation, _cachedDriver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.ManyInterfaces
        );

    [Benchmark]
    public GeneratorDriver CachedMany() =>
        _cachedDriver.RunGeneratorsAndUpdateCompilation(_cachedCompilation, out _, out _);
}

/// <summary>
/// CPU-profile benchmarks for the Refit source generator using an EventPipe CPU-sampling trace.
/// The exported *.speedscope.json shows where parse/emit time is spent.
/// </summary>
[ShortRunJob]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class SourceGeneratorProfiledCpuBenchmarks
{
    private Compilation _compilation = null!;
    private CSharpGeneratorDriver _driver = null!;

    [GlobalSetup]
    public void Setup() =>
        (_compilation, _driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.ManyInterfaces
        );

    [Benchmark]
    public GeneratorDriver CompileMany() =>
        _driver.RunGeneratorsAndUpdateCompilation(_compilation, out _, out _);
}
