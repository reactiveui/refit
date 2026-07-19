// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Profiles the emitter over parsed models, isolated from parsing.</summary>
/// <remarks>This is the build-time half of a cold run: string interpolation, member concatenation, and pooled-buffer
/// churn. Models are parsed once in setup, so the benchmark attributes cost purely to source emission.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class InterfaceEmissionBenchmarks
{
    /// <summary>The parsed generation model whose shared code is emitted.</summary>
    private ContextGenerationModel _model = null!;

    /// <summary>The parsed interface models emitted one by one.</summary>
    private InterfaceModel[] _interfaces = null!;

    /// <summary>Gets or sets the corpus size under benchmark.</summary>
    [ParamsAllValues]
    public CorpusSize Size { get; set; }

    /// <summary>Parses the corpus once and captures the interface models for the current size.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var compilation = GeneratorHarness.BuildCompilation(GeneratorCorpus.SourceFor(Size));
        var candidates = GeneratorHarness.CollectCandidates(compilation);
        _model = GeneratorHarness.Parse(compilation, candidates);
        _interfaces = [.. _model.Interfaces];
    }

    /// <summary>Emits the implementation source for every parsed interface.</summary>
    /// <returns>The total emitted source length, consumed so the emit is not elided.</returns>
    [Benchmark]
    public int EmitInterfaces()
    {
        var total = 0;
        foreach (var interfaceModel in _interfaces)
        {
            total += Emitter.EmitInterface(interfaceModel).Length;
        }

        return total;
    }

    /// <summary>Emits the shared preserve-attribute and factory-registration source.</summary>
    /// <returns>The total emitted source length, consumed so the emit is not elided.</returns>
    [Benchmark]
    public int EmitSharedCode()
    {
        var total = 0;
        Emitter.EmitSharedCode(_model, (_, text) => total += text.Length);
        return total;
    }
}
