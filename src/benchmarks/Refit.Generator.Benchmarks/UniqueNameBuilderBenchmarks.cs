// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for <see cref="UniqueNameBuilder"/>, allocated fresh per emitted interface.</summary>
/// <remarks>Each interface reserves its member names then draws unique field and local names, so the reserve-then-draw
/// cycle (and its backing <see cref="System.Collections.Generic.HashSet{T}"/>) runs once per generated client.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class UniqueNameBuilderBenchmarks
{
    /// <summary>Representative interface member names reserved before names are drawn.</summary>
    private readonly string[] _memberNames =
    [
        "Client", "Dispose", "Get0", "List1", "Create2", "Update3", "Delete4", "Search5",
    ];

    /// <summary>Reserves the member names then draws the generated field and local names one interface would need.</summary>
    /// <returns>The last drawn name, consumed so the work is not elided.</returns>
    [Benchmark]
    public string ReserveThenDraw()
    {
        var builder = new UniqueNameBuilder();
        builder.Reserve(_memberNames);
        _ = builder.New("_requestBuilder");
        _ = builder.New("_settings");

        // Draw a name that collides with a reserved member, forcing the disambiguation loop.
        return builder.New("Client");
    }
}
