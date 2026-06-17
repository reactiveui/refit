using BenchmarkDotNet.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Benchmarks;

[MemoryDiagnoser]
public class SourceGeneratorBenchmark
{
    private Compilation compilation = null!;
    private CSharpGeneratorDriver driver = null!;

    [GlobalSetup(Target = nameof(Compile))]
    public void SetupSmall() =>
        (compilation, driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.SmallInterface
        );

    [Benchmark]
    public GeneratorDriver Compile()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(Cached))]
    public void SetupCached() =>
        (compilation, driver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.SmallInterface
        );

    [Benchmark]
    public GeneratorDriver Cached()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(CompileMany))]
    public void SetupMany() =>
        (compilation, driver) = GeneratorBenchmarkHarness.Create(
            SourceGeneratorBenchmarksProjects.ManyInterfaces
        );

    [Benchmark]
    public GeneratorDriver CompileMany()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    [GlobalSetup(Target = nameof(CachedMany))]
    public void SetupCachedMany() =>
        (compilation, driver) = GeneratorBenchmarkHarness.CreatePrimedForCachedRun(
            SourceGeneratorBenchmarksProjects.ManyInterfaces
        );

    [Benchmark]
    public GeneratorDriver CachedMany()
    {
        return driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }
}
