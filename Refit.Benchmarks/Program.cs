using System;

using BenchmarkDotNet.Running;

namespace Refit.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            }
            else
            {
                BenchmarkRunner.Run<EndToEndBenchmark>();
            }
        }
    }
}
