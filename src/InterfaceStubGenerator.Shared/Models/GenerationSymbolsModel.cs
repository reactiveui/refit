// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Carries the symbols used to classify request and response types.</summary>
/// <param name="DisposableInterfaceSymbol">The <c>IDisposable</c> symbol, if available.</param>
/// <param name="HttpMethodBaseAttributeSymbol">The Refit HTTP method attribute symbol.</param>
/// <param name="FormattableSymbol">The <c>System.IFormattable</c> symbol used to classify inline-eligible value types, if available.</param>
/// <param name="SpanFormattableSymbol">The <c>System.ISpanFormattable</c> symbol (net6+ consumers only) enabling the path fast path, or null.</param>
/// <param name="Compilation">The compilation, used to resolve types behind an <c>extern alias</c>, or null.</param>
/// <param name="ReturnTypeAdapterInterface">The <c>Refit.IReturnTypeAdapter`2</c> symbol, or null when Refit is unavailable.</param>
/// <param name="ReturnTypeAdapters">The types implementing <c>IReturnTypeAdapter</c> discovered in the compilation.</param>
/// <param name="IndexedCollectionFormatValue">The underlying integer value of <c>CollectionFormat.Indexed</c> resolved once from the compilation, or <see
/// langword="null"/> when the <c>Refit.CollectionFormat</c> type cannot be found.</param>
internal readonly record struct GenerationSymbolsModel(
    ISymbol? DisposableInterfaceSymbol,
    INamedTypeSymbol HttpMethodBaseAttributeSymbol,
    INamedTypeSymbol? FormattableSymbol,
    INamedTypeSymbol? SpanFormattableSymbol,
    CSharpCompilation? Compilation,
    INamedTypeSymbol? ReturnTypeAdapterInterface,
    INamedTypeSymbol[] ReturnTypeAdapters,
    int? IndexedCollectionFormatValue);
