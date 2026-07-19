// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Refit.Generator.Benchmarks;

/// <summary>Micro-benchmarks for the emitter's literal and XML-documentation text primitives.</summary>
/// <remarks>These run once per emitted query key, header, path, and doc line, so their clean (no-escape) fast paths
/// dominate emission. Both the common no-escape input and the rarer escaping input are measured. Inputs are instance
/// fields so the JIT cannot constant-fold them into the call.</remarks>
[ShortRunJob]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class LiteralEmissionBenchmarks
{
    /// <summary>A representative identifier or key needing no C# or XML escaping (the common case).</summary>
    private readonly string _cleanValue = "application/json";

    /// <summary>A representative value containing characters that force the escaping path.</summary>
    private readonly string _escapedValue = "line\"one\"\n<tag & more>";

    /// <summary>Renders a clean value as a C# string literal (no-escape fast path).</summary>
    /// <returns>The emitted literal.</returns>
    [Benchmark]
    public string CSharpLiteralClean() => Emitter.ToCSharpStringLiteral(_cleanValue);

    /// <summary>Renders a value needing escaping as a C# string literal (escaping path).</summary>
    /// <returns>The emitted literal.</returns>
    [Benchmark]
    public string CSharpLiteralEscaped() => Emitter.ToCSharpStringLiteral(_escapedValue);

    /// <summary>Renders a nullable value as a C# string literal.</summary>
    /// <returns>The emitted literal.</returns>
    [Benchmark]
    public string NullableCSharpLiteral() => Emitter.ToNullableCSharpStringLiteral(_cleanValue);

    /// <summary>Escapes a clean value for XML documentation (no-escape fast path).</summary>
    /// <returns>The emitted documentation text.</returns>
    [Benchmark]
    public string XmlDocClean() => Emitter.ToXmlDocumentationText(_cleanValue);

    /// <summary>Escapes a value containing XML metacharacters for documentation (escaping path).</summary>
    /// <returns>The emitted documentation text.</returns>
    [Benchmark]
    public string XmlDocEscaped() => Emitter.ToXmlDocumentationText(_escapedValue);
}
