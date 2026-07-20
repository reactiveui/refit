// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for <see cref="PooledStringBuilder"/>, the emitter's fluent accumulation buffer.</summary>
/// <remarks>The emitter builds many transient fragments through this buffer; the pooled backing array is meant to be
/// reused across emissions instead of allocating fresh chunks. This measures the rent, append, grow, and return cycle.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class PooledBufferBenchmarks
{
    /// <summary>A representative generated statement appended repeatedly to exercise growth beyond the default rent.</summary>
    private readonly string _statement = "                ______parts.Add(new global::System.Collections.Generic.KeyValuePair<string, object?>(\"key\", value));\n";

    /// <summary>The number of statements appended, chosen to force at least one buffer growth.</summary>
    private readonly int _statementCount = 24;

    /// <summary>Rents a pooled buffer, appends a block of statements forcing growth, and returns the buffer.</summary>
    /// <returns>The accumulated source length, consumed so the work is not elided.</returns>
    [Benchmark]
    public int AppendBlock()
    {
        var builder = new PooledStringBuilder();
        for (var i = 0; i < _statementCount; i++)
        {
            _ = builder.Append(_statement);
        }

        return builder.ToString().Length;
    }
}
