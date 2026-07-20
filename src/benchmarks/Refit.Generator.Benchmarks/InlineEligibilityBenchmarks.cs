// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;

namespace Refit.Generator.Benchmarks;

/// <summary>Profiles inline-eligibility classification (<see cref="Parser.CanBuildRequestInline(IMethodSymbol, INamedTypeSymbol, INamedTypeSymbol?)"/>) over varied request shapes.</summary>
/// <remarks>Each call parses the method's request through the shared classifier the generator and the RF006 analyzer both
/// use, so this exercises the request-parsing decision path across scalar, collection, object, converter, and format
/// query bindings. It is the analyzer's per-keystroke cost as well as part of the generator's parse.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class InlineEligibilityBenchmarks
{
    /// <summary>The Refit method symbols whose request shapes are classified.</summary>
    private List<IMethodSymbol> _methods = null!;

    /// <summary>The resolved Refit base HTTP method attribute symbol.</summary>
    private INamedTypeSymbol _httpAttribute = null!;

    /// <summary>The resolved <c>System.IFormattable</c> symbol used by the value-formatting fast path.</summary>
    private INamedTypeSymbol? _formattable;

    /// <summary>Compiles the query-heavy corpus and resolves the symbols the classifier needs.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var compilation = GeneratorHarness.BuildCompilation(GeneratorCorpus.QueryHeavy);
        _methods = GeneratorHarness.GetRefitMethodSymbols(compilation);
        _httpAttribute = GeneratorHarness.GetHttpMethodBaseAttributeSymbol(compilation);
        _formattable = GeneratorHarness.GetFormattableSymbol(compilation);
    }

    /// <summary>Classifies every method's request shape for inline eligibility.</summary>
    /// <returns>The count of inline-eligible methods, consumed so the work is not elided.</returns>
    [Benchmark]
    public int ClassifyAll()
    {
        var eligible = 0;
        foreach (var method in _methods)
        {
            if (Parser.CanBuildRequestInline(method, _httpAttribute, _formattable))
            {
                eligible++;
            }
        }

        return eligible;
    }
}
