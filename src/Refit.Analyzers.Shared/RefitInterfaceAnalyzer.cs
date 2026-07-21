// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Linq;
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
        DiagnosticDescriptors.InvalidHeaderCollectionParameter,
        DiagnosticDescriptors.GeneratedRequestBuildingFallback,
        DiagnosticDescriptors.MultipleHeaderCollections,
        DiagnosticDescriptors.MultipleAuthorizeParameters,
        DiagnosticDescriptors.MultipleBodyParameters,
        DiagnosticDescriptors.MultipartBodyParameter
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
        return !arguments.IsEmpty && arguments[0].Value is string path
            ? path
            : string.Empty;
    }

#if !ROSLYN_5
    /// <summary>Creates the immutable descriptor set without using Roslyn 5-only collection expressions.</summary>
    /// <returns>The supported diagnostics.</returns>
    private static ImmutableArray<DiagnosticDescriptor> CreateSupportedDiagnostics()
    {
        const int supportedDiagnosticCount = 9;
        var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(supportedDiagnosticCount);
        builder.Add(DiagnosticDescriptors.InvalidRefitMember);
        builder.Add(DiagnosticDescriptors.InvalidRouteBackslash);
        builder.Add(DiagnosticDescriptors.MultipleCancellationTokens);
        builder.Add(DiagnosticDescriptors.InvalidHeaderCollectionParameter);
        builder.Add(DiagnosticDescriptors.GeneratedRequestBuildingFallback);
        builder.Add(DiagnosticDescriptors.MultipleHeaderCollections);
        builder.Add(DiagnosticDescriptors.MultipleAuthorizeParameters);
        builder.Add(DiagnosticDescriptors.MultipleBodyParameters);
        builder.Add(DiagnosticDescriptors.MultipartBodyParameter);
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

        // RF006 only makes sense when the source generator would actually build requests inline.
        // Mirror the generator's option handling: it is on by default, off when the consumer disables
        // generated request building or the generator entirely, in which case every method uses
        // reflection by design and the fallback diagnostic would be pure noise.
        var globalOptions = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        var reportGeneratedRequestBuildingFallback =
            GetBooleanOption(globalOptions, "RefitGeneratedRequestBuilding", defaultValue: true)
            && !GetBooleanOption(globalOptions, "DisableRefitSourceGenerator", defaultValue: false);

        var disposableInterface = context.Compilation.GetSpecialType(SpecialType.System_IDisposable);
        var formattableInterface = context.Compilation.GetTypeByMetadataName("System.IFormattable");

        // Discover return-type adapters the same way the generator does, so RF006 agrees an adapter-backed method is
        // built inline instead of falling back to reflection.
        var returnTypeAdapterInterface = Refit.Generator.Parser.ResolveReturnTypeAdapterInterface(context.Compilation);
        var returnTypeAdapters = Refit.Generator.Parser.DiscoverReturnTypeAdapters(
            context.Compilation,
            returnTypeAdapterInterface,
            context.CancellationToken);

        context.RegisterSymbolAction(
            symbolContext => AnalyzeInterface(
                (INamedTypeSymbol)symbolContext.Symbol,
                new(
                    httpMethodAttribute,
                    disposableInterface,
                    formattableInterface,
                    returnTypeAdapterInterface,
                    returnTypeAdapters,
                    reportGeneratedRequestBuildingFallback),
                symbolContext.ReportDiagnostic,
                symbolContext.CancellationToken),
            SymbolKind.NamedType);
    }

    /// <summary>Reads a boolean analyzer-config option by bare name or MSBuild build-property name.</summary>
    /// <param name="options">The analyzer-config options.</param>
    /// <param name="name">The option name without the build-property prefix.</param>
    /// <param name="defaultValue">The value to use when the option is absent or unparsable.</param>
    /// <returns>The parsed option value.</returns>
    private static bool GetBooleanOption(AnalyzerConfigOptions options, string name, bool defaultValue)
    {
        if (options.TryGetValue($"build_property.{name}", out var buildPropertyValue)
            && bool.TryParse(buildPropertyValue, out var buildPropertyParsed))
        {
            return buildPropertyParsed;
        }

        return options.TryGetValue(name, out var analyzerConfigValue) && bool.TryParse(analyzerConfigValue, out var analyzerConfigParsed)
            ? analyzerConfigParsed
            : defaultValue;
    }

    /// <summary>Analyzes a single interface and reports Refit contract diagnostics.</summary>
    /// <param name="interfaceSymbol">The interface symbol.</param>
    /// <param name="analysis">The resolved symbols and options shared by the compilation.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static void AnalyzeInterface(
        INamedTypeSymbol interfaceSymbol,
        in CompilationAnalysisState analysis,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        var httpMethodAttribute = analysis.HttpMethodAttribute;
        var disposableInterface = analysis.DisposableInterface;
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
            AnalyzeRefitMethod(method, analysis, reportDiagnostic);
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
    /// <param name="analysis">The resolved symbols and options shared by the compilation.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    private static void AnalyzeRefitMethod(
        IMethodSymbol method,
        in CompilationAnalysisState analysis,
        Action<Diagnostic> reportDiagnostic)
    {
        var httpMethodAttribute = analysis.HttpMethodAttribute;
        var httpMethod = FindHttpMethodAttribute(method, httpMethodAttribute);
        var path = GetHttpPath(httpMethod);
        if (path.IndexOf('\\') >= 0)
        {
            // A non-empty path can only originate from a source-declared HTTP method
            // attribute, so both the attribute and its syntax reference are always present here.
            reportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidRouteBackslash,
                httpMethod!.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                method.ContainingType.Name,
                method.Name));
        }

        ReportMultipartBodyDiagnostic(method, reportDiagnostic);
        ReportParameterShapeDiagnostics(method, reportDiagnostic);

        // The eligibility decision is the source generator's own classifier, compiled into this assembly,
        // so RF006 can never drift from what the generator actually emits.
        if (!analysis.ReportGeneratedRequestBuildingFallback
            || Refit.Generator.Parser.CanBuildRequestInline(
                method,
                httpMethodAttribute,
                analysis.FormattableInterface,
                analysis.ReturnTypeAdapterInterface,
                analysis.ReturnTypeAdapters))
        {
            return;
        }

        reportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.GeneratedRequestBuildingFallback,
            FirstLocation(method),
            method.ContainingType.Name,
            method.Name));
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
                                                                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
                                                            } namedType
                                                            && namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                                            == "global::System.Threading.CancellationToken");

    /// <summary>Reports the per-parameter duplicate and type diagnostics for a Refit method.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    private static void ReportParameterShapeDiagnostics(IMethodSymbol method, Action<Diagnostic> reportDiagnostic)
    {
        var cancellationTokenCount = 0;
        var headerCollectionCount = 0;
        var authorizeCount = 0;
        var bodyCount = 0;
        foreach (var parameter in method.Parameters)
        {
            ReportParameterShapeDiagnostics(
                method,
                parameter,
                reportDiagnostic,
                ref cancellationTokenCount,
                ref headerCollectionCount,
                ref authorizeCount,
                ref bodyCount);
        }
    }

    /// <summary>Reports the duplicate and type diagnostics for a single Refit method parameter.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    /// <param name="cancellationTokenCount">The running count of cancellation-token parameters seen so far.</param>
    /// <param name="headerCollectionCount">The running count of header-collection parameters seen so far.</param>
    /// <param name="authorizeCount">The running count of authorize parameters seen so far.</param>
    /// <param name="bodyCount">The running count of body parameters seen so far.</param>
    private static void ReportParameterShapeDiagnostics(
        IMethodSymbol method,
        IParameterSymbol parameter,
        Action<Diagnostic> reportDiagnostic,
        ref int cancellationTokenCount,
        ref int headerCollectionCount,
        ref int authorizeCount,
        ref int bodyCount)
    {
        if (IsCancellationToken(parameter.Type))
        {
            if (cancellationTokenCount > 0)
            {
                reportDiagnostic(MethodShapeDiagnostic(DiagnosticDescriptors.MultipleCancellationTokens, method, parameter));
            }

            cancellationTokenCount++;
        }

        if (HasHeaderCollectionAttribute(parameter))
        {
            if (!IsSupportedHeaderCollectionType(parameter.Type))
            {
                reportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidHeaderCollectionParameter,
                    FirstLocation(parameter),
                    parameter.Name,
                    method.ContainingType.Name,
                    method.Name));
            }

            if (headerCollectionCount > 0)
            {
                reportDiagnostic(MethodShapeDiagnostic(DiagnosticDescriptors.MultipleHeaderCollections, method, parameter));
            }

            headerCollectionCount++;
        }

        if (HasBodyAttribute(parameter))
        {
            if (bodyCount > 0)
            {
                reportDiagnostic(MethodShapeDiagnostic(DiagnosticDescriptors.MultipleBodyParameters, method, parameter));
            }

            bodyCount++;
        }

        if (!HasAuthorizeAttribute(parameter))
        {
            return;
        }

        if (authorizeCount > 0)
        {
            reportDiagnostic(MethodShapeDiagnostic(DiagnosticDescriptors.MultipleAuthorizeParameters, method, parameter));
        }

        authorizeCount++;
    }

    /// <summary>Creates a method-scoped diagnostic located at a parameter.</summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="method">The Refit method.</param>
    /// <param name="parameter">The parameter to locate the diagnostic at.</param>
    /// <returns>The diagnostic.</returns>
    private static Diagnostic MethodShapeDiagnostic(DiagnosticDescriptor descriptor, IMethodSymbol method, IParameterSymbol parameter) =>
        Diagnostic.Create(descriptor, FirstLocation(parameter), method.ContainingType.Name, method.Name);

    /// <summary>Determines whether a parameter has <c>HeaderCollectionAttribute</c>.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> when the parameter has the attribute.</returns>
    private static bool HasHeaderCollectionAttribute(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            // Every C# attribute surfaced by GetAttributes resolves to a symbol
            // (an error type when unresolved), so AttributeClass is never null here.
            if (attribute.AttributeClass!.ToDisplayString() == "Refit.HeaderCollectionAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a parameter carries the <c>[Authorize]</c> attribute.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> when the parameter has the attribute.</returns>
    private static bool HasAuthorizeAttribute(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass!.ToDisplayString() == "Refit.AuthorizeAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a parameter carries the <c>[Body]</c> attribute.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> when the parameter has the attribute.</returns>
    private static bool HasBodyAttribute(IParameterSymbol parameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass!.ToDisplayString() == "Refit.BodyAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports RF012 when a multipart method also declares a <c>[Body]</c> parameter.</summary>
    /// <param name="method">The Refit method.</param>
    /// <param name="reportDiagnostic">The diagnostic reporting callback.</param>
    private static void ReportMultipartBodyDiagnostic(IMethodSymbol method, Action<Diagnostic> reportDiagnostic)
    {
        if (!HasMultipartAttribute(method))
        {
            return;
        }

        foreach (var parameter in method.Parameters)
        {
            if (HasBodyAttribute(parameter))
            {
                reportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MultipartBodyParameter,
                    FirstLocation(parameter),
                    method.ContainingType.Name,
                    method.Name));
                return;
            }
        }
    }

    /// <summary>Determines whether a method carries the <c>[Multipart]</c> attribute.</summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns><see langword="true"/> when the method has the attribute.</returns>
    private static bool HasMultipartAttribute(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass!.ToDisplayString() == "Refit.MultipartAttribute")
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
        symbol.Locations.FirstOrDefault();

    /// <summary>Bundles the resolved symbols and options shared by one compilation's analysis.</summary>
    /// <param name="HttpMethodAttribute">The Refit HTTP method base attribute.</param>
    /// <param name="DisposableInterface">The <see cref="IDisposable"/> interface symbol.</param>
    /// <param name="FormattableInterface">The <see cref="IFormattable"/> interface symbol, if available.</param>
    /// <param name="ReturnTypeAdapterInterface">The <c>Refit.IReturnTypeAdapter`2</c> symbol, if available.</param>
    /// <param name="ReturnTypeAdapters">The discovered <c>IReturnTypeAdapter</c> implementations, kept in lockstep with the generator.</param>
    /// <param name="ReportGeneratedRequestBuildingFallback">Whether the RF006 fallback diagnostic is enabled.</param>
    private readonly record struct CompilationAnalysisState(
        INamedTypeSymbol HttpMethodAttribute,
        INamedTypeSymbol DisposableInterface,
        INamedTypeSymbol? FormattableInterface,
        INamedTypeSymbol? ReturnTypeAdapterInterface,
        INamedTypeSymbol[] ReturnTypeAdapters,
        bool ReportGeneratedRequestBuildingFallback);
}
