// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator.Benchmarks;

/// <summary>Measures parser and emitter costs for a path parameter with and without an additional header binding.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PathHeaderBindingGenerationBenchmarks
{
    /// <summary>The source containing a path-only parameter.</summary>
    private const string PathOnlySource =
        """
        using System.Threading.Tasks;
        using Refit;

        public interface IPathOnlyApi
        {
            [Get("/users/{userId}")]
            Task<string> Get([AliasAs("userId")] int id);
        }
        """;

    /// <summary>The source containing one parameter bound to both the path and a header.</summary>
    private const string PathAndHeaderSource =
        """
        using System.Threading.Tasks;
        using Refit;

        public interface IPathAndHeaderApi
        {
            [Get("/users/{userId}")]
            Task<string> Get([AliasAs("userId"), Header("X-User-Id")] int id);
        }
        """;

    /// <summary>The path-only compilation.</summary>
    private CSharpCompilation _pathOnlyCompilation = null!;

    /// <summary>The path-only syntax candidates.</summary>
    private (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces) _pathOnlyCandidates;

    /// <summary>The path-and-header compilation.</summary>
    private CSharpCompilation _pathAndHeaderCompilation = null!;

    /// <summary>The path-and-header syntax candidates.</summary>
    private (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces) _pathAndHeaderCandidates;

    /// <summary>The parsed path-only interface.</summary>
    private InterfaceModel _pathOnlyInterface = null!;

    /// <summary>The parsed path-and-header interface.</summary>
    private InterfaceModel _pathAndHeaderInterface = null!;

    /// <summary>Builds the compilations, syntax candidates, and pre-parsed emitter inputs.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _pathOnlyCompilation = GeneratorHarness.BuildCompilation(PathOnlySource);
        _pathOnlyCandidates = GeneratorHarness.CollectCandidates(_pathOnlyCompilation);
        _pathOnlyInterface = GeneratorHarness.Parse(_pathOnlyCompilation, _pathOnlyCandidates).Interfaces.AsArray()[0];

        _pathAndHeaderCompilation = GeneratorHarness.BuildCompilation(PathAndHeaderSource);
        _pathAndHeaderCandidates = GeneratorHarness.CollectCandidates(_pathAndHeaderCompilation);
        _pathAndHeaderInterface = GeneratorHarness.Parse(_pathAndHeaderCompilation, _pathAndHeaderCandidates).Interfaces.AsArray()[0];
    }

    /// <summary>Parses a path-only method.</summary>
    /// <returns>The parsed interface count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parser")]
    public int ParsePathOnly() =>
        GeneratorHarness.Parse(_pathOnlyCompilation, _pathOnlyCandidates).Interfaces.Count;

    /// <summary>Parses a method whose path parameter also contributes a header.</summary>
    /// <returns>The parsed interface count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Parser")]
    public int ParsePathAndHeader() =>
        GeneratorHarness.Parse(_pathAndHeaderCompilation, _pathAndHeaderCandidates).Interfaces.Count;

    /// <summary>Emits a path-only method.</summary>
    /// <returns>The generated source length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Emitter")]
    public int EmitPathOnly() => Emitter.EmitInterface(_pathOnlyInterface).Length;

    /// <summary>Emits a method whose path parameter also contributes a header.</summary>
    /// <returns>The generated source length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Benchmark]
    [BenchmarkCategory("Emitter")]
    public int EmitPathAndHeader() => Emitter.EmitInterface(_pathAndHeaderInterface).Length;
}
