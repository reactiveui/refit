// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Bundles the generation context shared by every interface processed in a single pass.</summary>
/// <param name="Diagnostics">The list that collects diagnostics produced during processing.</param>
/// <param name="PreserveAttributeDisplayName">The display name of the generated preserve attribute.</param>
/// <param name="DisposableInterfaceSymbol">The <c>IDisposable</c> symbol, if available.</param>
/// <param name="HttpMethodBaseAttributeSymbol">The Refit HTTP method attribute symbol.</param>
/// <param name="FormattableSymbol">The <c>System.IFormattable</c> symbol used to classify inline-eligible path parameter types, if available.</param>
/// <param name="GeneratedRequestBuilding">Whether generated request construction is enabled.</param>
/// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
/// <param name="SupportsNullable">Whether the compilation supports nullable reference types.</param>
/// <param name="SupportsStaticLambdas">Whether the compilation supports the <c>static</c> lambda modifier (C# 9).</param>
/// <param name="Compilation">The compilation, used to resolve extern aliases for types behind an <c>extern alias</c>, or null.</param>
/// <param name="ReturnTypeAdapterInterface">The <c>Refit.IReturnTypeAdapter`2</c> symbol, or null when Refit is unavailable.</param>
/// <param name="ReturnTypeAdapters">The types implementing <c>IReturnTypeAdapter</c> discovered in the compilation.</param>
/// <param name="ExternAliases">The per-interface collector recording the extern aliases used while qualifying its types.</param>
internal readonly record struct InterfaceGenerationContext(
    List<Diagnostic> Diagnostics,
    string PreserveAttributeDisplayName,
    ISymbol? DisposableInterfaceSymbol,
    INamedTypeSymbol HttpMethodBaseAttributeSymbol,
    INamedTypeSymbol? FormattableSymbol,
    bool GeneratedRequestBuilding,
    bool EmitGeneratedCodeMarkers,
    bool SupportsNullable,
    bool SupportsStaticLambdas,
    CSharpCompilation? Compilation,
    INamedTypeSymbol? ReturnTypeAdapterInterface,
    IReadOnlyList<INamedTypeSymbol> ReturnTypeAdapters,
    HashSet<string> ExternAliases);
