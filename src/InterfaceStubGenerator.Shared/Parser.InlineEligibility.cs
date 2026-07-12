// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>
/// The inline-eligibility entry point shared with the RF006 analyzer. The analyzer assemblies compile the request
/// parsing sources directly, so the fallback diagnostic is always the generator's own <c>CanGenerateInline</c>
/// decision rather than a hand-maintained mirror.
/// </content>
internal static partial class Parser
{
    /// <summary>Determines whether generated request building can construct a method's request inline.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="httpMethodBaseAttributeSymbol">The resolved <c>Refit.HttpMethodAttribute</c> symbol.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <returns><see langword="true"/> when the method's request is inline-eligible.</returns>
    internal static bool CanBuildRequestInline(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol httpMethodBaseAttributeSymbol,
        INamedTypeSymbol? formattableSymbol) =>
        CanBuildRequestInline(methodSymbol, httpMethodBaseAttributeSymbol, formattableSymbol, null, []);

    /// <summary>Determines whether generated request building can construct a method's request inline.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="httpMethodBaseAttributeSymbol">The resolved <c>Refit.HttpMethodAttribute</c> symbol.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="returnTypeAdapterInterface">The resolved <c>Refit.IReturnTypeAdapter`2</c> symbol, or null.</param>
    /// <param name="returnTypeAdapters">The discovered <c>IReturnTypeAdapter</c> implementations, so the analyzer agrees
    /// with the generator that an adapter-backed return type is inline-eligible.</param>
    /// <returns><see langword="true"/> when the method's request is inline-eligible.</returns>
    internal static bool CanBuildRequestInline(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol httpMethodBaseAttributeSymbol,
        INamedTypeSymbol? formattableSymbol,
        INamedTypeSymbol? returnTypeAdapterInterface,
        IReadOnlyList<INamedTypeSymbol> returnTypeAdapters)
    {
        if (FindHttpMethodAttribute(methodSymbol, httpMethodBaseAttributeSymbol) is null)
        {
            return false;
        }

        // The diagnostics collected here duplicate what the generator itself reports, so they are discarded.
        var context = new InterfaceGenerationContext(
            [],
            string.Empty,
            null,
            httpMethodBaseAttributeSymbol,
            formattableSymbol,
            SpanFormattableSymbol: null,
            SupportsSpanEscape: false,
            GeneratedRequestBuilding: true,
            EmitGeneratedCodeMarkers: false,
            SupportsNullable: false,
            SupportsStaticLambdas: false,
            Compilation: null,
            returnTypeAdapterInterface,
            returnTypeAdapters,
            ExternAliases: []);
        return ParseRequest(methodSymbol, ClassifyInlineReturnShape(methodSymbol.ReturnType), context)
            .CanGenerateInline;
    }

    /// <summary>Classifies a return type into the shape buckets inline eligibility distinguishes.</summary>
    /// <param name="returnType">The declared return type.</param>
    /// <returns>The return shape; unsupported shapes map to <see cref="ReturnTypeInfo.Return"/>.</returns>
    private static ReturnTypeInfo ClassifyInlineReturnShape(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol namedType)
        {
            return ReturnTypeInfo.Return;
        }

        var ns = namedType.ContainingNamespace.ToDisplayString();
        return namedType.MetadataName switch
        {
            "Task" when ns == "System.Threading.Tasks" => ReturnTypeInfo.AsyncVoid,
            "Task`1" or "ValueTask`1" when ns == "System.Threading.Tasks" => ReturnTypeInfo.AsyncResult,
            "IAsyncEnumerable`1" when ns == "System.Collections.Generic" => ReturnTypeInfo.AsyncEnumerable,
            _ => ReturnTypeInfo.Return
        };
    }
}
