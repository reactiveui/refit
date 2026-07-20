// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator.Benchmarks;

/// <summary>Profiles the parser transform (<see cref="Parser.GenerateInterfaceStubs"/>) in isolation from the driver.</summary>
/// <remarks>This is the semantic-model-bound half of a cold run: symbol resolution, member walks, and request parsing.
/// It excludes syntax-provider collection and driver bookkeeping, so it attributes cost to the generator's own parse.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class ParserTransformBenchmarks
{
    /// <summary>The compilation the parser transform runs against.</summary>
    private CSharpCompilation _compilation = null!;

    /// <summary>The pre-collected syntactic candidates fed to the transform.</summary>
    private (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces) _candidates;

    /// <summary>Gets or sets the corpus size under benchmark.</summary>
    [ParamsAllValues]
    public CorpusSize Size { get; set; }

    /// <summary>Builds the compilation and collects syntactic candidates once for the current corpus size.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _compilation = GeneratorHarness.BuildCompilation(GeneratorCorpus.SourceFor(Size));
        _candidates = GeneratorHarness.CollectCandidates(_compilation);
    }

    /// <summary>Runs the parser transform over the collected candidates.</summary>
    /// <returns>The parsed interface count, consumed so the transform is not elided and no model is boxed.</returns>
    [Benchmark]
    public int Parse() => GeneratorHarness.Parse(_compilation, _candidates).Interfaces.Count;
}
