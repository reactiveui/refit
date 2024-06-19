using BenchmarkDotNet.Running;
using Refit.Benchmarks;

if (args is { Length: > 0 })
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
else
{
    BenchmarkRunner.Run<EndToEndBenchmark>();
}
