using BenchmarkDotNet.Running;


namespace Refit.Profiler
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<RefitBenchmark>();
        }
    }
}
