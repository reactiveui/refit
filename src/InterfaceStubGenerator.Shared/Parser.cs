// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
internal static partial class Parser
{
    /// <summary>The suffix used for generator-private Refit helper types.</summary>
    private const string RefitInternalGeneratedSuffix = "RefitInternalGenerated";

    /// <summary>Builds the generator model for the candidate Refit interfaces.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="refitInternalNamespace">The refit internal namespace.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is enabled.</param>
    /// <param name="emitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
    /// <param name="candidateMethods">The candidate methods.</param>
    /// <param name="candidateInterfaces">The candidate interfaces.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The collected diagnostics and the model used to generate the stubs.</returns>
    public static (
        List<Diagnostic> diagnostics,
        ContextGenerationModel contextGenerationSpec) GenerateInterfaceStubs(
        CSharpCompilation compilation,
        string? refitInternalNamespace,
        bool generatedRequestBuilding,
        bool emitGeneratedCodeMarkers,
        in ImmutableArray<MethodDeclarationSyntax> candidateMethods,
        in ImmutableArray<InterfaceDeclarationSyntax> candidateInterfaces,
        CancellationToken cancellationToken)
    {
        ArgumentExceptionHelper.ThrowIfNull(compilation);

        refitInternalNamespace = BuildRefitInternalNamespace(refitInternalNamespace);

        var httpMethodBaseAttributeSymbol = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute");

        var diagnostics = new List<Diagnostic>();
        if (httpMethodBaseAttributeSymbol is null)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.RefitNotReferenced, null));
            return CreateEmptyResult(diagnostics, refitInternalNamespace, generatedRequestBuilding, emitGeneratedCodeMarkers);
        }

        var interfaceToNullableEnabledMap = new Dictionary<INamedTypeSymbol, bool>(
            SymbolEqualityComparer.Default);
        var interfaces = CollectRefitInterfaces(
            compilation,
            candidateMethods,
            candidateInterfaces,
            httpMethodBaseAttributeSymbol,
            interfaceToNullableEnabledMap,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Nothing needs to be generated in this pass.
        if (interfaces.Count == 0)
        {
            return CreateEmptyResult(diagnostics, refitInternalNamespace, generatedRequestBuilding, emitGeneratedCodeMarkers);
        }

        var context = CreateGenerationContext(
            compilation,
            diagnostics,
            refitInternalNamespace,
            httpMethodBaseAttributeSymbol,
            generatedRequestBuilding,
            emitGeneratedCodeMarkers,
            cancellationToken);

        var interfaceModels = BuildInterfaceModels(
            interfaces,
            interfaceToNullableEnabledMap,
            context,
            cancellationToken);

        var contextGenerationSpec = new ContextGenerationModel(
            refitInternalNamespace,
            context.PreserveAttributeDisplayName,
            generatedRequestBuilding,
            emitGeneratedCodeMarkers,
            interfaceModels);
        return (diagnostics, contextGenerationSpec);
    }

    /// <summary>Builds the empty generation result returned when there is nothing to generate.</summary>
    /// <param name="diagnostics">The diagnostics collected so far.</param>
    /// <param name="refitInternalNamespace">The resolved internal generated namespace.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is enabled.</param>
    /// <param name="emitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
    /// <returns>The diagnostics paired with an empty context model.</returns>
    private static (List<Diagnostic> diagnostics, ContextGenerationModel contextGenerationSpec) CreateEmptyResult(
        List<Diagnostic> diagnostics,
        string refitInternalNamespace,
        bool generatedRequestBuilding,
        bool emitGeneratedCodeMarkers) =>
        (
            diagnostics,
            new(
                refitInternalNamespace,
                string.Empty,
                generatedRequestBuilding,
                emitGeneratedCodeMarkers,
                ImmutableEquatableArrayFactory.Empty<InterfaceModel>())
        );

    /// <summary>Resolves the well-known symbols and language capabilities used throughout a single generation pass.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="diagnostics">The list that collects diagnostics produced during processing.</param>
    /// <param name="refitInternalNamespace">The resolved internal generated namespace.</param>
    /// <param name="httpMethodBaseAttributeSymbol">The resolved Refit HTTP method attribute symbol.</param>
    /// <param name="generatedRequestBuilding">Whether generated request construction is enabled.</param>
    /// <param name="emitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generation context for the pass.</returns>
    private static InterfaceGenerationContext CreateGenerationContext(
        CSharpCompilation compilation,
        List<Diagnostic> diagnostics,
        string refitInternalNamespace,
        INamedTypeSymbol httpMethodBaseAttributeSymbol,
        bool generatedRequestBuilding,
        bool emitGeneratedCodeMarkers,
        CancellationToken cancellationToken)
    {
        var options = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;

        // Resolve the handful of well-known symbols directly. The previous WellKnownTypes wrapper
        // allocated a dictionary-backed cache object every pass for just these lookups.
        var disposableInterfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable");
        var formattableSymbol = compilation.GetTypeByMetadataName("System.IFormattable");

        // Resolve the value-formatting fast-path capabilities once per pass (never per interface or per parameter) and
        // thread them through the context. ISpanFormattable is net6+, and the query builder percent-encodes a formatted
        // span in place on every net6+ target, so the span-escape tier applies wherever ISpanFormattable exists.
        var spanFormattableSymbol = compilation.GetTypeByMetadataName("System.ISpanFormattable");
        var supportsSpanEscape = spanFormattableSymbol is not null;

        var supportsNullable = options.LanguageVersion >= LanguageVersion.CSharp8;
        var supportsStaticLambdas = options.LanguageVersion >= LanguageVersion.CSharp9;

        // The PreserveAttribute is emitted into the consumer's compilation by the emitter
        // (see Emitter.EmitSharedCode). Its fully-qualified display name is fully determined
        // by refitInternalNamespace, so we compute it directly instead of round-tripping
        // through compilation.AddSyntaxTrees + GetTypeByMetadataName + ToDisplayString on
        // every generator pass, which mutated the compilation and forced an extra bind.
        var preserveAttributeDisplayName = $"global::{refitInternalNamespace}.PreserveAttribute";

        var returnTypeAdapterInterface = ResolveReturnTypeAdapterInterface(compilation);
        var returnTypeAdapters = DiscoverReturnTypeAdapters(compilation, returnTypeAdapterInterface, cancellationToken);

        return new InterfaceGenerationContext(
            diagnostics,
            preserveAttributeDisplayName,
            disposableInterfaceSymbol,
            httpMethodBaseAttributeSymbol,
            formattableSymbol,
            spanFormattableSymbol,
            supportsSpanEscape,
            generatedRequestBuilding,
            emitGeneratedCodeMarkers,
            supportsNullable,
            supportsStaticLambdas,
            options.LanguageVersion >= LanguageVersion.CSharp12,
            compilation,
            returnTypeAdapterInterface,
            returnTypeAdapters,
            [],
            new Dictionary<ISymbol, string?>(SymbolEqualityComparer.Default));
    }

    /// <summary>Builds the internal generated namespace from a consumer-provided namespace prefix.</summary>
    /// <param name="refitInternalNamespace">The optional user or MSBuild-supplied namespace prefix.</param>
    /// <returns>A valid C# namespace for generated Refit internals.</returns>
    private static string BuildRefitInternalNamespace(string? refitInternalNamespace)
    {
        var rawNamespace = string.IsNullOrWhiteSpace(refitInternalNamespace)
            ? RefitInternalGeneratedSuffix
            : refitInternalNamespace + RefitInternalGeneratedSuffix;
        var parts = rawNamespace.Split('.');
        var builder = new StringBuilder(rawNamespace.Length);

        foreach (var part in parts)
        {
            var normalized = NormalizeNamespacePart(part);
            if (normalized.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                _ = builder.Append('.');
            }

            _ = builder.Append(normalized);
        }

        return builder.Length == 0 ? RefitInternalGeneratedSuffix : builder.ToString();
    }

    /// <summary>Normalizes one namespace segment into a valid identifier.</summary>
    /// <param name="part">The namespace segment.</param>
    /// <returns>The normalized segment, or an empty string when the segment is blank.</returns>
    private static string NormalizeNamespacePart(string part)
    {
        if (string.IsNullOrWhiteSpace(part))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(part.Length + 1);
        foreach (var character in part)
        {
            if (builder.Length == 0)
            {
                if (SyntaxFacts.IsIdentifierStartCharacter(character))
                {
                    _ = builder.Append(character);
                }
                else if (SyntaxFacts.IsIdentifierPartCharacter(character))
                {
                    _ = builder.Append('_').Append(character);
                }
                else
                {
                    _ = builder.Append('_');
                }

                continue;
            }

            _ = builder.Append(SyntaxFacts.IsIdentifierPartCharacter(character) ? character : '_');
        }

        var normalized = builder.ToString();
        return SyntaxFacts.GetKeywordKind(normalized) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(normalized) != SyntaxKind.None
            ? "_" + normalized
            : normalized;
    }

    /// <summary>Collects the interfaces with Refit methods, declared or inherited, into a single map.</summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="candidateMethods">The candidate methods.</param>
    /// <param name="candidateInterfaces">The candidate interfaces.</param>
    /// <param name="httpMethodBaseAttributeSymbol">The Refit HTTP method attribute symbol.</param>
    /// <param name="interfaceToNullableEnabledMap">Receives the nullable-context flag per interface.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A map of interface symbol to its directly declared Refit methods.</returns>
    private static Dictionary<INamedTypeSymbol, List<IMethodSymbol>> CollectRefitInterfaces(
        CSharpCompilation compilation,
        in ImmutableArray<MethodDeclarationSyntax> candidateMethods,
        in ImmutableArray<InterfaceDeclarationSyntax> candidateInterfaces,
        INamedTypeSymbol httpMethodBaseAttributeSymbol,
        Dictionary<INamedTypeSymbol, bool> interfaceToNullableEnabledMap,
        CancellationToken cancellationToken)
    {
        // Check the candidates and keep the ones we're actually interested in. LINQ (GroupBy,
        // ToDictionary, SelectMany) is deliberately avoided here: this runs on every generator
        // pass and the iterator/closure allocations add up. Group refit methods by their declaring
        // interface as they are discovered, building the dictionary in a single pass.
        var interfaces = new Dictionary<INamedTypeSymbol, List<IMethodSymbol>>(
            SymbolEqualityComparer.Default);

        var compilationNullableEnabled =
            compilation.Options.NullableContextOptions == NullableContextOptions.Enable;

        foreach (var method in candidateMethods)
        {
            // GetSemanticModel is cached per syntax tree, so calling it per method is cheap and
            // lets us skip a GroupBy-by-tree allocation.
            var model = compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = model.GetDeclaredSymbol(method, cancellationToken);
            if (!IsRefitMethod(methodSymbol, httpMethodBaseAttributeSymbol))
            {
                continue;
            }

            var containingType = methodSymbol!.ContainingType;
            interfaceToNullableEnabledMap[containingType] =
                compilationNullableEnabled
                || model.GetNullableContext(method.SpanStart) == NullableContext.Enabled;

            if (!interfaces.TryGetValue(containingType, out var interfaceMethods))
            {
                interfaceMethods = [];
                interfaces[containingType] = interfaceMethods;
            }

            interfaceMethods.Add(methodSymbol);
        }

        // Add interfaces whose Refit methods are inherited from base interfaces.
        foreach (var iface in candidateInterfaces)
        {
            var model = compilation.GetSemanticModel(iface.SyntaxTree);
            var ifaceSymbol = model.GetDeclaredSymbol(iface, cancellationToken);

            // Skip duplicates already captured from candidate methods.
            if (ifaceSymbol is null || interfaces.ContainsKey(ifaceSymbol))
            {
                continue;
            }

            // The interface has no refit methods of its own, but its base interfaces might.
            if (!HasDerivedRefitMethods(ifaceSymbol, httpMethodBaseAttributeSymbol))
            {
                continue;
            }

            // Add the interface with an empty method set; downstream processing already
            // looks for inherited Refit methods.
            interfaces.Add(ifaceSymbol, []);
            interfaceToNullableEnabledMap[ifaceSymbol] =
                model.GetNullableContext(iface.SpanStart) == NullableContext.Enabled;
        }

        return interfaces;
    }

    /// <summary>Builds the interface models for every collected Refit interface.</summary>
    /// <param name="interfaces">The collected Refit interfaces and their declared methods.</param>
    /// <param name="interfaceToNullableEnabledMap">The nullable-context flag per interface.</param>
    /// <param name="context">The shared generation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The interface models, one per collected interface.</returns>
    private static ImmutableEquatableArray<InterfaceModel> BuildInterfaceModels(
        Dictionary<INamedTypeSymbol, List<IMethodSymbol>> interfaces,
        Dictionary<INamedTypeSymbol, bool> interfaceToNullableEnabledMap,
        InterfaceGenerationContext context,
        CancellationToken cancellationToken)
    {
        var keyCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var interfaceModels = new InterfaceModel[interfaces.Count];
        var index = 0;

        // Process each interface into the generation model.
        foreach (var group in interfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Each entry is keyed by the interface symbol and contains the methods already known
            // to be Refit methods. Remaining members still need RF001 validation.
            var keyName = group.Key.Name;
            while (keyCount.TryGetValue(keyName, out var value))
            {
                value++;
                keyName = $"{keyName}{value}";
            }

            // A failed TryGetValue leaves value at its default of 0, so the deduplicated name starts at zero.
            keyCount[keyName] = 0;
            var fileName = $"{keyName}.g.cs";

            // A fresh collector per interface: each generated file declares only the extern aliases its own types use.
            interfaceModels[index] = ProcessInterface(
                fileName,
                group.Key,
                group.Value,
                interfaceToNullableEnabledMap[group.Key],
                context with { ExternAliases = [] });
            index++;
        }

        return ImmutableEquatableArrayFactory.FromArray(interfaceModels);
    }

    /// <summary>Builds the model for a single interface from its Refit and non-Refit members.</summary>
    /// <param name="fileName">The generated file name for the interface.</param>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <param name="refitMethods">The Refit methods declared on the interface.</param>
    /// <param name="nullableEnabled">Whether nullable reference types are enabled for the interface.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The model describing the interface.</returns>
    private static InterfaceModel ProcessInterface(
        string fileName,
        INamedTypeSymbol interfaceSymbol,
        List<IMethodSymbol> refitMethods,
        bool nullableEnabled,
        InterfaceGenerationContext context)
    {
        var names = ComputeInterfaceNames(interfaceSymbol);
        var members = interfaceSymbol.GetMembers();

        var partition = PartitionMembers(
            interfaceSymbol,
            members,
            refitMethods,
            context);

        var memberNames = CollectMemberNames(members);

        var refitMethodsArray = ParseMethods(refitMethods, true, context);

        // Only include refit methods discovered on base interfaces here.
        // Do NOT duplicate the current interface's refit methods.
        var derivedRefitMethodsArray = ParseMethods(partition.DerivedRefitMethods, false, context);

        var nonRefitMethodModels = BuildNonRefitMethodModels(
            partition.NonRefitMethods,
            partition.DerivedNonRefitMethods,
            context);
        var properties = BuildInterfacePropertyModels(members, partition.InheritedProperties, context);

        var constraints = GenerateConstraints(interfaceSymbol.TypeParameters, false, context);
        var nullability = (context.SupportsNullable, nullableEnabled) switch
        {
            (false, _) => Nullability.None,
            (true, true) => Nullability.Enabled,
            (true, false) => Nullability.Disabled
        };
        return new(
            context.PreserveAttributeDisplayName,
            fileName,
            names.ClassName,
            names.Namespace,
            names.ClassDeclaration,
            names.InterfaceDisplayName,
            names.ClassSuffix,
            context.GeneratedRequestBuilding,
            context.EmitGeneratedCodeMarkers,
            context.SupportsNullable,
            context.SupportsStaticLambdas,
            context.SupportsCollectionExpressions,
            constraints,
            memberNames,
            properties,
            nonRefitMethodModels,
            refitMethodsArray,
            derivedRefitMethodsArray,
            nullability,
            partition.HasDispose,
            BuildSortedExternAliases(context.ExternAliases));
    }

    /// <summary>Builds the deterministically-ordered set of extern aliases an interface's types reference.</summary>
    /// <param name="aliases">The collected extern aliases.</param>
    /// <returns>The sorted extern aliases, or an empty array when none were used.</returns>
    private static ImmutableEquatableArray<string> BuildSortedExternAliases(HashSet<string> aliases)
    {
        if (aliases.Count == 0)
        {
            return ImmutableEquatableArray<string>.Empty;
        }

        var sorted = new List<string>(aliases);
        sorted.Sort(StringComparer.Ordinal);
        return sorted.ToImmutableEquatableArray();
    }

    /// <summary>Computes the generated identifiers and display names for an interface.</summary>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <returns>The computed names for the interface.</returns>
    private static InterfaceNames ComputeInterfaceNames(INamedTypeSymbol interfaceSymbol)
    {
        // Start with the interface display name including type parameters, then strip the namespace.
        var className = interfaceSymbol.ToDisplayString();
        var lastDot = className.LastIndexOf('.');
        if (lastDot > 0)
        {
            className = className[(lastDot + 1)..];
        }

        var classDeclaration = (interfaceSymbol.ContainingType?.Name) + className;

        // Use the simple interface name for the generated class suffix.
        var classSuffix = (interfaceSymbol.ContainingType?.Name) + interfaceSymbol.Name;
        var ns = interfaceSymbol.ContainingNamespace?.ToDisplayString();

        // Keep generated names stable for interfaces declared in the global namespace.
        if (interfaceSymbol.ContainingNamespace is { IsGlobalNamespace: true })
        {
            ns = string.Empty;
        }

        // Flatten dots out of the namespace for generated identifiers.
        ns = ns!.Replace(".", string.Empty);
        var interfaceDisplayName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new(className, classDeclaration, classSuffix, ns, interfaceDisplayName);
    }

    /// <summary>Splits the interface's direct and inherited members into the sets the generated stub emits.</summary>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <param name="members">The directly declared members of the interface.</param>
    /// <param name="refitMethods">The Refit methods declared on the interface.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The partitioned member sets.</returns>
    private static MethodPartition PartitionMembers(
        INamedTypeSymbol interfaceSymbol,
        in ImmutableArray<ISymbol> members,
        List<IMethodSymbol> refitMethods,
        InterfaceGenerationContext context)
    {
        var nonRefitMethods = CollectDirectNonRefitMethods(members, refitMethods);

        var derivedRefitMethods = new List<IMethodSymbol>();
        var derivedNonRefitMethods = new List<IMethodSymbol>();
        var inheritedProperties = new List<IPropertySymbol>();
        var hasDispose = CollectDerivedMembers(
            interfaceSymbol,
            context,
            derivedRefitMethods,
            derivedNonRefitMethods,
            inheritedProperties);

        derivedNonRefitMethods = ExcludeExplicitlyImplementedBaseMethods(
            members,
            derivedNonRefitMethods);

        return new(
            nonRefitMethods,
            derivedRefitMethods,
            derivedNonRefitMethods,
            inheritedProperties,
            hasDispose);
    }

    /// <summary>Collects the non-Refit methods declared directly on the interface.</summary>
    /// <param name="members">The directly declared members of the interface.</param>
    /// <param name="refitMethods">The Refit methods declared on the interface.</param>
    /// <returns>The non-Refit methods declared directly on the interface.</returns>
    private static List<IMethodSymbol> CollectDirectNonRefitMethods(
        in ImmutableArray<ISymbol> members,
        List<IMethodSymbol> refitMethods)
    {
        // Get any other (non-Refit) methods declared directly on the interface. LINQ is avoided
        // throughout because it runs for every interface on every generator pass; the HashSets
        // below reproduce the de-duplicating semantics of the previous Except/Distinct.
        var refitMethodSet = new HashSet<IMethodSymbol>(refitMethods, SymbolEqualityComparer.Default);
        var nonRefitMethods = new List<IMethodSymbol>(members.Length);

        // HashSet has no capacity ctor on netstandard2.0, so it is left unsized.
        var seenNonRefitMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var member in members)
        {
            if (
                member is IMethodSymbol method
                && !refitMethodSet.Contains(method)
                && seenNonRefitMethods.Add(method)
            )
            {
                nonRefitMethods.Add(method);
            }
        }

        return nonRefitMethods;
    }

    /// <summary>Walks the inherited interfaces once, collecting derived methods and inherited properties.</summary>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <param name="context">The shared generation context.</param>
    /// <param name="derivedRefitMethods">Receives the Refit methods inherited from base interfaces.</param>
    /// <param name="derivedNonRefitMethods">Receives the non-Refit methods inherited from base interfaces.</param>
    /// <param name="inheritedProperties">Receives the emittable properties inherited from base interfaces.</param>
    /// <returns><see langword="true"/> if the interface inherits <c>IDisposable.Dispose</c>; otherwise, <see langword="false"/>.</returns>
    private static bool CollectDerivedMembers(
        INamedTypeSymbol interfaceSymbol,
        InterfaceGenerationContext context,
        List<IMethodSymbol> derivedRefitMethods,
        List<IMethodSymbol> derivedNonRefitMethods,
        List<IPropertySymbol> inheritedProperties)
    {
        // Walk all inherited interfaces (and each base's members) exactly once: pull out the IDisposable.Dispose
        // method, split the remaining methods into Refit and non-Refit sets, and collect inherited emittable
        // properties in the same pass. Methods and properties were previously two separate AllInterfaces walks.
        var disposableInterfaceSymbol = context.DisposableInterfaceSymbol;
        var hasDispose = false;
        var seenDerivedNonRefitMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var seenInheritedProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                switch (member)
                {
                    case IMethodSymbol method when IsDisposeMethod(method, disposableInterfaceSymbol):
                    {
                        hasDispose = true;
                        break;
                    }

                    case IMethodSymbol method when IsRefitMethod(method, context.HttpMethodBaseAttributeSymbol):
                    {
                        derivedRefitMethods.Add(method);
                        break;
                    }

                    case IMethodSymbol method when seenDerivedNonRefitMethods.Add(method):
                    {
                        derivedNonRefitMethods.Add(method);
                        break;
                    }

                    case IPropertySymbol property
                        when IsEmittableProperty(property) && seenInheritedProperties.Add(property):
                    {
                        inheritedProperties.Add(property);
                        break;
                    }
                }
            }
        }

        return hasDispose;
    }

    /// <summary>Determines whether a method is the inherited <c>IDisposable.Dispose</c> method.</summary>
    /// <param name="method">The candidate method.</param>
    /// <param name="disposableInterfaceSymbol">The <c>IDisposable</c> symbol, if available.</param>
    /// <returns><see langword="true"/> if the method is declared on <c>IDisposable</c>; otherwise, <see langword="false"/>.</returns>
    private static bool IsDisposeMethod(IMethodSymbol method, ISymbol? disposableInterfaceSymbol) =>
        method.ContainingType?.Equals(
            disposableInterfaceSymbol,
            SymbolEqualityComparer.Default) == true;

    /// <summary>Removes base methods that the current interface re-declares as explicit Refit members.</summary>
    /// <param name="members">The directly declared members of the interface.</param>
    /// <param name="derivedNonRefitMethods">The non-Refit methods discovered on base interfaces.</param>
    /// <returns>The filtered list of derived non-Refit methods.</returns>
    private static List<IMethodSymbol> ExcludeExplicitlyImplementedBaseMethods(
        in ImmutableArray<ISymbol> members,
        List<IMethodSymbol> derivedNonRefitMethods)
    {
        if (derivedNonRefitMethods.Count == 0)
        {
            return derivedNonRefitMethods;
        }

        var explicitlyImplementedBaseMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var member in members)
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            foreach (var baseMethod in method.ExplicitInterfaceImplementations)
            {
                // Compare generic methods by definition so explicit implementations line up.
                _ = explicitlyImplementedBaseMethods.Add(baseMethod.OriginalDefinition);
            }
        }

        if (explicitlyImplementedBaseMethods.Count == 0)
        {
            return derivedNonRefitMethods;
        }

        var filteredDerivedNonRefitMethods = new List<IMethodSymbol>(derivedNonRefitMethods.Count);
        foreach (var method in derivedNonRefitMethods)
        {
            if (!explicitlyImplementedBaseMethods.Contains(method.OriginalDefinition))
            {
                filteredDerivedNonRefitMethods.Add(method);
            }
        }

        return filteredDerivedNonRefitMethods;
    }

    /// <summary>The generated identifiers and display names computed for an interface.</summary>
    /// <param name="ClassName">The simple generated class name.</param>
    /// <param name="ClassDeclaration">The generated class declaration name.</param>
    /// <param name="ClassSuffix">The generated class suffix.</param>
    /// <param name="Namespace">The flattened namespace for generated identifiers.</param>
    /// <param name="InterfaceDisplayName">The fully qualified interface display name.</param>
    private readonly record struct InterfaceNames(
        string ClassName,
        string ClassDeclaration,
        string ClassSuffix,
        string Namespace,
        string InterfaceDisplayName);

    /// <summary>The interface's members partitioned into the sets the generated stub emits.</summary>
    /// <param name="NonRefitMethods">The non-Refit methods declared directly on the interface.</param>
    /// <param name="DerivedRefitMethods">The Refit methods inherited from base interfaces.</param>
    /// <param name="DerivedNonRefitMethods">The non-Refit methods inherited from base interfaces.</param>
    /// <param name="InheritedProperties">The emittable properties inherited from base interfaces, in discovery order.</param>
    /// <param name="HasDispose">Whether the interface inherits <c>IDisposable.Dispose</c>.</param>
    private readonly record struct MethodPartition(
        List<IMethodSymbol> NonRefitMethods,
        List<IMethodSymbol> DerivedRefitMethods,
        List<IMethodSymbol> DerivedNonRefitMethods,
        List<IPropertySymbol> InheritedProperties,
        bool HasDispose);
}
