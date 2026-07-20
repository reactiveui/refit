// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the value-equality that drives incremental-generator caching.</summary>
/// <remarks>After an unrelated edit the driver re-runs the transform and compares the new model against the cached one
/// through <see cref="ImmutableEquatableArray{T}"/> equality. That comparison is the per-keystroke gate that decides
/// whether emission is skipped, so its cost (and the fact that equal models produce no allocation) is measured here.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class ModelEqualityBenchmarks
{
    /// <summary>The parsed interface models from the first pass (the cached side of the comparison).</summary>
    private ImmutableEquatableArray<InterfaceModel> _cached;

    /// <summary>An equal-but-distinct set of interface models from a second pass (the recomputed side).</summary>
    private ImmutableEquatableArray<InterfaceModel> _recomputed;

    /// <summary>Gets or sets the corpus size under benchmark.</summary>
    [ParamsAllValues]
    public CorpusSize Size { get; set; }

    /// <summary>Parses the corpus twice, producing two equal-but-distinct model sets.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var source = GeneratorCorpus.SourceFor(Size);

        var firstCompilation = GeneratorHarness.BuildCompilation(source);
        _cached = GeneratorHarness.Parse(firstCompilation, GeneratorHarness.CollectCandidates(firstCompilation)).Interfaces;

        var secondCompilation = GeneratorHarness.BuildCompilation(source);
        _recomputed = GeneratorHarness.Parse(secondCompilation, GeneratorHarness.CollectCandidates(secondCompilation)).Interfaces;
    }

    /// <summary>Compares the recomputed model set against the cached one, as the driver does per keystroke.</summary>
    /// <returns><see langword="true"/> when the models are equal (the expected cache-hit result).</returns>
    [Benchmark]
    public bool Equals() => _cached.Equals(_recomputed);

    /// <summary>Computes the hash code of the cached model set.</summary>
    /// <returns>The hash code.</returns>
    [Benchmark]
    public int HashCode() => _cached.GetHashCode();
}
