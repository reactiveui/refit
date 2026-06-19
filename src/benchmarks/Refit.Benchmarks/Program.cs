// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Running;
using Refit.Benchmarks;

if (args is { Length: > 0 })
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
else
{
    BenchmarkRunner.Run<EndToEndBenchmark>();

    // To run a different suite by default, swap the type above for StartupBenchmark,
    // PerformanceBenchmark, or SourceGeneratorBenchmark (or pass a filter on the command line).
}
