using BenchmarkDotNet.Running;

namespace Refit.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<EndToEndBenchmark>();
        }
    }
}
