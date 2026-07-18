// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the emitter's indentation and fragment-concatenation layout primitives.</summary>
/// <remarks><c>Indent</c> is called for every generated line; the cached-level path must not allocate, while deeper
/// levels allocate a fresh string. <c>ConcatParts</c> joins the per-member source fragments of an interface.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class LayoutEmissionBenchmarks
{
    /// <summary>A cached indentation level (within the shared indent cache).</summary>
    private readonly int _cachedLevel = 3;

    /// <summary>An indentation level beyond the cache that allocates a fresh string.</summary>
    private readonly int _uncachedLevel = 20;

    /// <summary>Representative per-member source fragments joined into one interface body.</summary>
    private readonly string[] _parts =
    [
        "        public Task<Entity> Get0(int id) { }\n",
        "        public Task<List<Entity>> List1() { }\n",
        "        public Task<Entity> Create2(Entity e) { }\n",
        "        public Task Update3(int id, Entity e) { }\n",
        "        public Task Delete4(int id) { }\n",
    ];

    /// <summary>Returns a cached indentation string (no-allocation fast path).</summary>
    /// <returns>The indentation string.</returns>
    [Benchmark]
    public string IndentCached() => Emitter.Indent(_cachedLevel);

    /// <summary>Builds an indentation string beyond the cache (allocation path).</summary>
    /// <returns>The indentation string.</returns>
    [Benchmark]
    public string IndentUncached() => Emitter.Indent(_uncachedLevel);

    /// <summary>Concatenates the interface's per-member source fragments.</summary>
    /// <returns>The joined source.</returns>
    [Benchmark]
    public string ConcatParts() => Emitter.ConcatParts(_parts, _parts.Length);
}
