// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the emitter's indentation primitives.</summary>
/// <remarks><c>Indent</c> is called for every generated line; the cached-level path must not allocate, while deeper
/// levels allocate a fresh string.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class LayoutEmissionBenchmarks
{
    /// <summary>A cached indentation level (within the shared indent cache).</summary>
    private readonly int _cachedLevel = 3;

    /// <summary>An indentation level beyond the cache that allocates a fresh string.</summary>
    private readonly int _uncachedLevel = 20;

    /// <summary>Returns a cached indentation string (no-allocation fast path).</summary>
    /// <returns>The indentation string.</returns>
    [Benchmark]
    public string IndentCached() => Emitter.Indent(_cachedLevel);

    /// <summary>Builds an indentation string beyond the cache (allocation path).</summary>
    /// <returns>The indentation string.</returns>
    [Benchmark]
    public string IndentUncached() => Emitter.Indent(_uncachedLevel);
}
