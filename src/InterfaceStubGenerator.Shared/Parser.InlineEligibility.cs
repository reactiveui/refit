// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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
    /// <summary>The <c>System.Threading.Tasks</c> namespace name, matched structurally to identify task return shapes.</summary>
    private const string TasksNamespace = "System.Threading.Tasks";

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
        INamedTypeSymbol[] returnTypeAdapters)
    {
        if (FindHttpMethodAttribute(methodSymbol, httpMethodBaseAttributeSymbol) is null)
        {
            return false;
        }

        // The diagnostics collected here duplicate what the generator itself reports, so they are discarded.
        var context = new InterfaceGenerationContext(
            [],
            string.Empty,
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
            SupportsCollectionExpressions: false,
            Compilation: null,
            returnTypeAdapterInterface,
            returnTypeAdapters,
            ExternAliases: [],
            AssemblyAliasCache: new Dictionary<ISymbol, string?>(SymbolEqualityComparer.Default),
            QualifiedTypeCache: new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default),
            FormattableClassificationCache: new Dictionary<ISymbol, (bool Formattable, bool SpanFormattable)>(SymbolEqualityComparer.Default));
        return ParseRequest(methodSymbol, ClassifyInlineReturnShape(methodSymbol.ReturnType), context)
            .CanGenerateInline;
    }

    /// <summary>Classifies a return type into the shape buckets inline eligibility distinguishes.</summary>
    /// <param name="returnType">The declared return type.</param>
    /// <returns>The return shape; unsupported shapes map to <see cref="ReturnTypeInfo.Return"/>.</returns>
    internal static ReturnTypeInfo ClassifyInlineReturnShape(ITypeSymbol returnType)
    {
        return returnType is not INamedTypeSymbol namedType
            ? ReturnTypeInfo.Return
            : namedType.MetadataName switch
            {
                "Task" when IsInNamespace(namedType, TasksNamespace) => ReturnTypeInfo.AsyncVoid,
                "Task`1" when IsHttpRequestMessageType(namedType.TypeArguments[0]) => ReturnTypeInfo.RequestMessage,
                "Task`1" or "ValueTask`1" when IsInNamespace(namedType, TasksNamespace) => ReturnTypeInfo.AsyncResult,
                "IAsyncEnumerable`1" when IsInNamespace(namedType, "System.Collections.Generic") => ReturnTypeInfo.AsyncEnumerable,
                "IObservable`1" when IsInNamespace(namedType, "System") => ReturnTypeInfo.Observable,
                _ => ReturnTypeInfo.Return
            };
    }

    /// <summary>Determines whether a type is <c>System.Net.Http.HttpRequestMessage</c>, the type argument of the
    /// build-and-return <c>Task&lt;HttpRequestMessage&gt;</c> shape.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is <c>HttpRequestMessage</c>.</returns>
    /// <remarks>Only the <c>Task</c> wrapper is supported: request building is asynchronous (body serialization and the
    /// authorization token getter), so a synchronous <c>HttpRequestMessage</c> return would force a blocking build.</remarks>
    internal static bool IsHttpRequestMessageType(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "HttpRequestMessage" } named
        && IsInNamespace(named, "System.Net.Http");
}
