// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Transient scaffolding shared by every interface processed in a single generation pass.</summary>
/// <remarks>This carries the compilation and well-known symbols used only while a pass runs. It is created inside the
/// parse transform and discarded before any model is produced, so it must never flow through the incremental pipeline.
/// It is a reference type (passed by reference, not copied) and its collections are plain concrete types - the emitted
/// models are the equatable, cache-safe values, never this.</remarks>
/// <param name="Diagnostics">The list that collects diagnostics produced during processing.</param>
/// <param name="PreserveAttributeDisplayName">The display name of the generated preserve attribute.</param>
/// <param name="DisposableInterfaceSymbol">The <c>IDisposable</c> symbol, if available.</param>
/// <param name="HttpMethodBaseAttributeSymbol">The Refit HTTP method attribute symbol.</param>
/// <param name="FormattableSymbol">The <c>System.IFormattable</c> symbol used to classify inline-eligible value types, if available.</param>
/// <param name="SpanFormattableSymbol">The <c>System.ISpanFormattable</c> symbol (net6+ consumers only) enabling the path fast path, or null.</param>
/// <param name="SupportsSpanEscape">Whether the target exposes <c>Uri.EscapeDataString(ReadOnlySpan&lt;char&gt;)</c> (net9+), enabling the escaping span fast path.</param>
/// <param name="GeneratedRequestBuilding">Whether generated request construction is enabled.</param>
/// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
/// <param name="SupportsNullable">Whether the compilation supports nullable reference types.</param>
/// <param name="SupportsStaticLambdas">Whether the compilation supports the <c>static</c> lambda modifier (C# 9).</param>
/// <param name="Compilation">The compilation, used to resolve types behind an <c>extern alias</c>, or null.</param>
/// <param name="ReturnTypeAdapterInterface">The <c>Refit.IReturnTypeAdapter`2</c> symbol, or null when Refit is unavailable.</param>
/// <param name="ReturnTypeAdapters">The types implementing <c>IReturnTypeAdapter</c> discovered in the compilation.</param>
/// <param name="ExternAliases">The per-interface collector recording the extern aliases used while qualifying its types.</param>
/// <param name="AssemblyAliasCache">A pass-wide cache mapping an assembly symbol to its resolved extern alias (or null),
/// shared across every interface so the extern-alias metadata-reference lookup runs once per assembly, not per type node.</param>
internal sealed record InterfaceGenerationContext(
    List<Diagnostic> Diagnostics,
    string PreserveAttributeDisplayName,
    ISymbol? DisposableInterfaceSymbol,
    INamedTypeSymbol HttpMethodBaseAttributeSymbol,
    INamedTypeSymbol? FormattableSymbol,
    INamedTypeSymbol? SpanFormattableSymbol,
    bool SupportsSpanEscape,
    bool GeneratedRequestBuilding,
    bool EmitGeneratedCodeMarkers,
    bool SupportsNullable,
    bool SupportsStaticLambdas,
    CSharpCompilation? Compilation,
    INamedTypeSymbol? ReturnTypeAdapterInterface,
    INamedTypeSymbol[] ReturnTypeAdapters,
    HashSet<string> ExternAliases,
    Dictionary<ISymbol, string?> AssemblyAliasCache);
