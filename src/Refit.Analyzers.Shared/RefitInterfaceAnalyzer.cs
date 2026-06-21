// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refit.Analyzers;

/// <summary>Analyzes Refit interface contracts independently of the source generation path.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RefitInterfaceAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
#if ROSLYN_5
    [
        DiagnosticDescriptors.InvalidRefitMember,
        DiagnosticDescriptors.InvalidRouteBackslash,
        DiagnosticDescriptors.MultipleCancellationTokens,
        DiagnosticDescriptors.InvalidHeaderCollectionParameter
    ];
#else
        CreateSupportedDiagnostics();
#endif

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(RegisterCompilationAnalysis);
    }

    /// <summary>Gets the literal path from a Refit HTTP method attribute.</summary>
    /// <param name="attributeData">The attribute data.</param>
    /// <returns>The path literal.</returns>
    internal static string GetHttpPath(AttributeData? attributeData)
    {
        if (attributeData is null)
        {
            return string.Empty;
        }

        var arguments = attributeData.ConstructorArguments;
        return arguments.Length > 0 && arguments[0].Value is string path
            ? path
            : string.Empty;
    }

#if !ROSLYN_5
    /// <summary>Creates the immutable descriptor set without using Roslyn 5-only collection expressions.</summary>
    /// <returns>The supported diagnostics.</returns>
    private static ImmutableArray<DiagnosticDescriptor> CreateSupportedDiagnostics()
    {
        var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(4);
        builder.Add(DiagnosticDescriptors.InvalidRefitMember);
        builder.Add(DiagnosticDescriptors.InvalidRouteBackslash);
        builder.Add(DiagnosticDescriptors.MultipleCancellationTokens);
        builder.Add(DiagnosticDescriptors.InvalidHeaderCollectionParameter);
        return builder.MoveToImmutable();
    }
#endif

    /// <summary>Registers symbol actions after resolving the Refit HTTP method base attribute.</summary>
    /// <param name="context">The compilation-start context.</param>
    private static void RegisterCompilationAnalysis(CompilationStartAnalysisContext context)
    {
        var httpMethodAttribute = context.Compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute");
        if (httpMethodAttribute is null)
        {
            return;
        }

        var disposableInterface = context.Compilation.GetSpecialType(SpecialType.System_IDisposable);
        context.RegisterSymbolAction(
            symbolContext => AnalyzeInterface(
                (INamedTypeSymbol)symbolContext.Symbol,
                httpMethodAttribute,
                disposableInterface,
                symbolContext.ReportDiagnostic,
                symbolContext.CancellationToken),
            SymbolKind.NamedType);
    }

    /// <summary>Analyzes a single interface and reports Refit contract diagnostics.</summary>
    /// <param name="interfaceSymbol">The interface symbol.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute.</param>
    /// <param name="disposableInterface">The <see cref="IDisposable"/> interface symbol.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static void AnalyzeInterface(
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol httpMethodAttribute,
        INamedTypeSymbol disposableInterface,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        if (interfaceSymbol.TypeKind != TypeKind.Interface)
        {
            return;
        }

        var members = interfaceSymbol.GetMembers();
        var hasRefitMethods = false;

        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (!IsRefitMethod(method, httpMethodAttribute))
            {
                continue;
            }

            hasRefitMethods = true;
            AnalyzeRefitMethod(method, httpMethodAttribute, reportDiagnostic);
        }

        var hasInheritedRefitMethods = HasInheritedRefitMethods(interfaceSymbol, httpMethodAttribute);
        if (!hasRefitMethods && !hasInheritedRefitMethods)
        {
            return;
        }

        AnalyzeNonRefitMethods(
            members,
            httpMethodAttribute,
            disposableInterface,
            reportDiagnostic,
            cancellationToken);
        AnalyzeInheritedNonRefitMethods(
            interfaceSymbol,
            httpMethodAttribute,
            disposableInterface,
            reportDiagnostic,
            cancellationToken);
    }

    /// <summary>Reports diagnostics for invalid Refit method shapes.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    private static void AnalyzeRefitMethod(
        IMethodSymbol method,
        INamedTypeSymbol httpMethodAttribute,
        Action<Diagnostic> reportDiagnostic)
    {
        var httpMethod = FindHttpMethodAttribute(method, httpMethodAttribute);
        var path = GetHttpPath(httpMethod);
        if (path.IndexOf('\\') >= 0)
        {
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidRouteBackslash,
                httpMethod?.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? FirstLocation(method),
                method.ContainingType.Name,
                method.Name));
        }

        var cancellationTokenCount = 0;
        foreach (var parameter in method.Parameters)
        {
            if (IsCancellationToken(parameter.Type) && cancellationTokenCount++ > 0)
            {
                reportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MultipleCancellationTokens,
                    FirstLocation(parameter),
                    method.ContainingType.Name,
                    method.Name));
            }

            if (HasHeaderCollectionAttribute(parameter) && !IsSupportedHeaderCollectionType(parameter.Type))
            {
                reportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidHeaderCollectionParameter,
                    FirstLocation(parameter),
                    parameter.Name,
                    method.ContainingType.Name,
                    method.Name));
            }
        }
    }

    /// <summary>Reports diagnostics for directly declared non-Refit methods on a Refit interface.</summary>
    /// <param name="members">The directly declared interface members.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute.</param>
    /// <param name="disposableInterface">The <see cref="IDisposable"/> interface symbol.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static void AnalyzeNonRefitMethods(
        ImmutableArray<ISymbol> members,
        INamedTypeSymbol httpMethodAttribute,
        INamedTypeSymbol disposableInterface,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is IMethodSymbol method
                && IsEmittableNonRefitMethod(method, disposableInterface)
                && !IsRefitMethod(method, httpMethodAttribute))
            {
                ReportInvalidRefitMember(method, reportDiagnostic);
            }
        }
    }

    /// <summary>Reports diagnostics for inherited non-Refit methods that a generated client must still implement.</summary>
    /// <param name="interfaceSymbol">The Refit interface being analyzed.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute.</param>
    /// <param name="disposableInterface">The <see cref="IDisposable"/> interface symbol.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static void AnalyzeInheritedNonRefitMethods(
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol httpMethodAttribute,
        INamedTypeSymbol disposableInterface,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var member in baseInterface.GetMembers())
            {
                if (member is IMethodSymbol method
                    && IsEmittableNonRefitMethod(method, disposableInterface)
                    && !IsRefitMethod(method, httpMethodAttribute))
                {
                    ReportInvalidRefitMember(method, reportDiagnostic);
                }
            }
        }
    }

    /// <summary>Reports RF001 for a method.</summary>
    /// <param name="method">The invalid method.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    private static void ReportInvalidRefitMember(
        IMethodSymbol method,
        Action<Diagnostic> reportDiagnostic)
    {
        foreach (var location in method.Locations)
        {
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidRefitMember,
                location,
                method.ContainingType.Name,
                method.Name));
        }
    }

    /// <summary>Determines whether a non-Refit method needs a generated implementation.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <param name="disposableInterface">The <see cref="IDisposable"/> interface symbol.</param>
    /// <returns><see langword="true"/> when the method requires implementation.</returns>
    private static bool IsEmittableNonRefitMethod(
        IMethodSymbol method,
        INamedTypeSymbol disposableInterface) =>
        !method.IsStatic
        && method.MethodKind != MethodKind.PropertyGet
        && method.MethodKind != MethodKind.PropertySet
        && method.IsAbstract
        && !SymbolEqualityComparer.Default.Equals(method.ContainingType, disposableInterface);

    /// <summary>Determines whether a method is decorated with a Refit HTTP method attribute.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute symbol.</param>
    /// <returns><see langword="true"/> if the method is a Refit method; otherwise, <see langword="false"/>.</returns>
    private static bool IsRefitMethod(IMethodSymbol method, INamedTypeSymbol httpMethodAttribute) =>
        FindHttpMethodAttribute(method, httpMethodAttribute) is not null;

    /// <summary>Finds the HTTP method attribute on a Refit method.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute symbol.</param>
    /// <returns>The matching attribute, if any.</returns>
    private static AttributeData? FindHttpMethodAttribute(
        IMethodSymbol method,
        INamedTypeSymbol httpMethodAttribute)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (InheritsFromOrEquals(attribute.AttributeClass, httpMethodAttribute))
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>Determines whether any base interface declares a Refit method.</summary>
    /// <param name="interfaceSymbol">The interface symbol to inspect.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method attribute symbol.</param>
    /// <returns><see langword="true"/> if a base interface declares a Refit method; otherwise, <see langword="false"/>.</returns>
    private static bool HasInheritedRefitMethods(
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol httpMethodAttribute)
    {
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is IMethodSymbol method && IsRefitMethod(method, httpMethodAttribute))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Determines whether a symbol inherits from or equals another symbol.</summary>
    /// <param name="symbol">The candidate symbol.</param>
    /// <param name="expected">The expected base symbol.</param>
    /// <returns><see langword="true"/> when the candidate matches.</returns>
    private static bool InheritsFromOrEquals(
        INamedTypeSymbol? symbol,
        INamedTypeSymbol expected)
    {
        while (symbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol, expected))
            {
                return true;
            }

            symbol = symbol.BaseType;
        }

        return false;
    }

    /// <summary>Determines whether a type is <see cref="CancellationToken"/> or nullable <see cref="CancellationToken"/>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is a cancellation token.</returns>
    private static bool IsCancellationToken(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        == "global::System.Threading.CancellationToken" || (type is INamedTypeSymbol
                                                            {
                                                                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
                                                                TypeArguments.Length: 1
                                                            } namedType
                                                            && namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                                            == "global::System.Threading.CancellationToken");

    /// <summary>Determines whether a parameter has <c>HeaderCollectionAttribute</c>.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> when the parameter has the attribute.</returns>
    private static bool HasHeaderCollectionAttribute(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "Refit.HeaderCollectionAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a header collection parameter matches runtime semantics.</summary>
    /// <param name="type">The parameter type.</param>
    /// <returns><see langword="true"/> when the type is supported.</returns>
    private static bool IsSupportedHeaderCollectionType(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        == "global::System.Collections.Generic.IDictionary<string, string>";

    /// <summary>Gets the first source location for a symbol.</summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The first available location, or <see langword="null"/>.</returns>
    private static Location? FirstLocation(ISymbol symbol) =>
        symbol.Locations.Length > 0 ? symbol.Locations[0] : null;
}
