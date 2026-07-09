// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>The underlying value for <c>BodySerializationMethod.UrlEncoded</c>.</summary>
    private const int BodySerializationUrlEncoded = 2;

    /// <summary>The underlying value for <c>BodySerializationMethod.Serialized</c>.</summary>
    private const int BodySerializationSerialized = 3;

    /// <summary>The underlying value for <c>BodySerializationMethod.JsonLines</c>.</summary>
    private const int BodySerializationJsonLines = 4;

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

        var options = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;

        // Resolve the handful of well-known symbols directly. The previous WellKnownTypes wrapper
        // allocated a dictionary-backed cache object every pass for just these two lookups.
        var disposableInterfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable");
        var httpMethodBaseAttributeSymbol = compilation.GetTypeByMetadataName(
            "Refit.HttpMethodAttribute");
        var formattableSymbol = compilation.GetTypeByMetadataName("System.IFormattable");

        var diagnostics = new List<Diagnostic>();
        if (httpMethodBaseAttributeSymbol is null)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.RefitNotReferenced, null));
            return (
                diagnostics,
                new(
                    refitInternalNamespace,
                    string.Empty,
                    generatedRequestBuilding,
                    emitGeneratedCodeMarkers,
                    ImmutableEquatableArrayFactory.Empty<InterfaceModel>())
            );
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
            return (
                diagnostics,
                new(
                    refitInternalNamespace,
                    string.Empty,
                    generatedRequestBuilding,
                    emitGeneratedCodeMarkers,
                    ImmutableEquatableArrayFactory.Empty<InterfaceModel>())
            );
        }

        var supportsNullable = options.LanguageVersion >= LanguageVersion.CSharp8;
        var supportsStaticLambdas = options.LanguageVersion >= LanguageVersion.CSharp9;

        // The PreserveAttribute is emitted into the consumer's compilation by the emitter
        // (see Emitter.EmitSharedCode). Its fully-qualified display name is fully determined
        // by refitInternalNamespace, so we compute it directly instead of round-tripping
        // through compilation.AddSyntaxTrees + GetTypeByMetadataName + ToDisplayString on
        // every generator pass, which mutated the compilation and forced an extra bind.
        var preserveAttributeDisplayName = $"global::{refitInternalNamespace}.PreserveAttribute";

        var context = new InterfaceGenerationContext(
            diagnostics,
            preserveAttributeDisplayName,
            disposableInterfaceSymbol,
            httpMethodBaseAttributeSymbol,
            formattableSymbol,
            generatedRequestBuilding,
            emitGeneratedCodeMarkers,
            supportsNullable,
            supportsStaticLambdas);

        var interfaceModels = BuildInterfaceModels(
            interfaces,
            interfaceToNullableEnabledMap,
            context,
            cancellationToken);

        var contextGenerationSpec = new ContextGenerationModel(
            refitInternalNamespace,
            preserveAttributeDisplayName,
            generatedRequestBuilding,
            emitGeneratedCodeMarkers,
            interfaceModels);
        return (diagnostics, contextGenerationSpec);
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
        in InterfaceGenerationContext context,
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
                keyName = $"{keyName}{++value}";
            }

            // A failed TryGetValue leaves value at its default of 0, so the deduplicated name starts at zero.
            keyCount[keyName] = 0;
            var fileName = $"{keyName}.g.cs";

            interfaceModels[index++] = ProcessInterface(
                fileName,
                group.Key,
                group.Value,
                interfaceToNullableEnabledMap[group.Key],
                context);
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
        in InterfaceGenerationContext context)
    {
        var names = ComputeInterfaceNames(interfaceSymbol);
        var members = interfaceSymbol.GetMembers();

        var partition = PartitionMethods(
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
            partition.DerivedNonRefitMethods);
        var properties = BuildInterfacePropertyModels(interfaceSymbol, members);

        var constraints = GenerateConstraints(interfaceSymbol.TypeParameters, false);
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
            constraints,
            memberNames,
            properties,
            nonRefitMethodModels,
            refitMethodsArray,
            derivedRefitMethodsArray,
            nullability,
            partition.HasDispose);
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

    /// <summary>Splits the interface's direct and inherited members into Refit and non-Refit sets.</summary>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <param name="members">The directly declared members of the interface.</param>
    /// <param name="refitMethods">The Refit methods declared on the interface.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The partitioned method sets.</returns>
    private static MethodPartition PartitionMethods(
        INamedTypeSymbol interfaceSymbol,
        in ImmutableArray<ISymbol> members,
        List<IMethodSymbol> refitMethods,
        in InterfaceGenerationContext context)
    {
        var nonRefitMethods = CollectDirectNonRefitMethods(members, refitMethods);

        var derivedRefitMethods = new List<IMethodSymbol>();
        var derivedNonRefitMethods = new List<IMethodSymbol>();
        var hasDispose = CollectDerivedMethods(
            interfaceSymbol,
            context,
            derivedRefitMethods,
            derivedNonRefitMethods);

        derivedNonRefitMethods = ExcludeExplicitlyImplementedBaseMethods(
            members,
            derivedNonRefitMethods);

        return new(
            nonRefitMethods,
            derivedRefitMethods,
            derivedNonRefitMethods,
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

    /// <summary>Walks the inherited interfaces, splitting their methods into Refit and non-Refit sets.</summary>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <param name="context">The shared generation context.</param>
    /// <param name="derivedRefitMethods">Receives the Refit methods inherited from base interfaces.</param>
    /// <param name="derivedNonRefitMethods">Receives the non-Refit methods inherited from base interfaces.</param>
    /// <returns><see langword="true"/> if the interface inherits <c>IDisposable.Dispose</c>; otherwise, <see langword="false"/>.</returns>
    private static bool CollectDerivedMethods(
        INamedTypeSymbol interfaceSymbol,
        in InterfaceGenerationContext context,
        List<IMethodSymbol> derivedRefitMethods,
        List<IMethodSymbol> derivedNonRefitMethods)
    {
        // Walk all inherited interfaces once, pulling out the IDisposable.Dispose method and
        // splitting the rest into Refit and non-Refit methods.
        var disposableInterfaceSymbol = context.DisposableInterfaceSymbol;
        var hasDispose = false;
        var seenDerivedNonRefitMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is not IMethodSymbol method)
                {
                    continue;
                }

                if (IsDisposeMethod(method, disposableInterfaceSymbol))
                {
                    hasDispose = true;
                    continue;
                }

                if (IsRefitMethod(method, context.HttpMethodBaseAttributeSymbol))
                {
                    derivedRefitMethods.Add(method);
                }
                else if (seenDerivedNonRefitMethods.Add(method))
                {
                    derivedNonRefitMethods.Add(method);
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

    /// <summary>Collects the distinct member names declared on the interface, preserving order.</summary>
    /// <param name="members">The directly declared members of the interface.</param>
    /// <returns>The distinct member names.</returns>
    private static ImmutableEquatableArray<string> CollectMemberNames(in ImmutableArray<ISymbol> members)
    {
        var seenMemberNames = new HashSet<string>();
        var memberNames = new string[members.Length];
        var count = 0;
        foreach (var member in members)
        {
            if (seenMemberNames.Add(member.Name))
            {
                memberNames[count++] = member.Name;
            }
        }

        return TrimAndWrap(memberNames, count);
    }

    /// <summary>Wraps a fully populated array, or copies the populated prefix before wrapping.</summary>
    /// <param name="values">The array containing populated values at the front.</param>
    /// <param name="count">The number of populated entries.</param>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The immutable equatable array.</returns>
    private static ImmutableEquatableArray<T> TrimAndWrap<T>(T[] values, int count)
        where T : IEquatable<T>
    {
        if (count == 0)
        {
            return ImmutableEquatableArrayFactory.Empty<T>();
        }

        if (count == values.Length)
        {
            return ImmutableEquatableArrayFactory.FromArray(values);
        }

        var trimmed = new T[count];
        Array.Copy(values, trimmed, count);
        return ImmutableEquatableArrayFactory.FromArray(trimmed);
    }

    /// <summary>Builds models for interface properties implemented by the generated stub.</summary>
    /// <param name="interfaceSymbol">The interface symbol being processed.</param>
    /// <param name="members">The directly declared interface members.</param>
    /// <returns>The property models.</returns>
    private static ImmutableEquatableArray<InterfacePropertyModel> BuildInterfacePropertyModels(
        INamedTypeSymbol interfaceSymbol,
        in ImmutableArray<ISymbol> members)
    {
        var properties = new List<InterfacePropertyModel>();
        foreach (var member in members)
        {
            if (member is IPropertySymbol property && IsEmittableProperty(property))
            {
                properties.Add(ParseInterfaceProperty(property, false));
            }
        }

        var seenInheritedProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is IPropertySymbol property
                    && IsEmittableProperty(property)
                    && seenInheritedProperties.Add(property))
                {
                    properties.Add(ParseInterfaceProperty(property, true));
                }
            }
        }

        return properties.ToImmutableEquatableArray();
    }

    /// <summary>Determines whether an interface property should be implemented by the generated stub.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property should be emitted.</returns>
    private static bool IsEmittableProperty(IPropertySymbol property) =>
        !property.IsStatic
        && property.IsAbstract
        && property.Parameters.IsEmpty;

    /// <summary>Builds an interface property model.</summary>
    /// <param name="property">The property to parse.</param>
    /// <param name="isDerived">Whether the property comes from a base interface.</param>
    /// <returns>The property model.</returns>
    private static InterfacePropertyModel ParseInterfaceProperty(IPropertySymbol property, bool isDerived)
    {
        var annotation =
            !property.Type.IsValueType && property.NullableAnnotation == NullableAnnotation.Annotated;
        var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var containingType = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var requestPropertyKey = GetInterfacePropertyRequestKey(property);
        var isSatisfiedByGeneratedMember = IsGeneratedClientProperty(property, propertyType);
        var isExplicitInterface =
            isDerived || (HasGeneratedMemberNameCollision(property) && !isSatisfiedByGeneratedMember);

        return new(
            property.MetadataName,
            propertyType,
            annotation,
            containingType,
            requestPropertyKey,
            property.GetMethod is not null,
            property.SetMethod is not null,
            isSatisfiedByGeneratedMember,
            isExplicitInterface);
    }

    /// <summary>Determines whether the generated stub's existing <c>Client</c> property satisfies an interface property.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <param name="propertyType">The fully-qualified property type display string.</param>
    /// <returns><see langword="true"/> when no extra property emission is required.</returns>
    private static bool IsGeneratedClientProperty(IPropertySymbol property, string propertyType) =>
        property.MetadataName == "Client"
        && propertyType == "global::System.Net.Http.HttpClient"
        && property.GetMethod is not null
        && property.SetMethod is null;

    /// <summary>Determines whether an interface property name collides with generated stub infrastructure members.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property should be emitted explicitly to avoid a member collision.</returns>
    private static bool HasGeneratedMemberNameCollision(IPropertySymbol property) =>
        property.MetadataName is "Client" or "requestBuilder";

    /// <summary>Gets the request-property key declared on an interface property.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns>The request-property key, or an empty string when the property is not request-bound.</returns>
    [ExcludeFromCodeCoverage]
    private static string GetInterfacePropertyRequestKey(IPropertySymbol property)
    {
        foreach (var attribute in property.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Refit.PropertyAttribute")
            {
                continue;
            }

            var arguments = attribute.ConstructorArguments;
            return !arguments.IsEmpty && arguments[0].Value is string { Length: > 0 } key
                ? key
                : property.MetadataName;
        }

        return string.Empty;
    }

    /// <summary>Parses a set of Refit methods into method models.</summary>
    /// <param name="methods">The Refit methods to parse.</param>
    /// <param name="isImplicitInterface">Whether the methods belong to the implicitly implemented interface.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The method models.</returns>
    private static ImmutableEquatableArray<MethodModel> ParseMethods(
        List<IMethodSymbol> methods,
        bool isImplicitInterface,
        in InterfaceGenerationContext context)
    {
        if (methods.Count == 0)
        {
            return ImmutableEquatableArrayFactory.Empty<MethodModel>();
        }

        var methodModels = new MethodModel[methods.Count];
        for (var i = 0; i < methods.Count; i++)
        {
            methodModels[i] = ParseMethod(methods[i], isImplicitInterface, context);
        }

        return ImmutableEquatableArrayFactory.FromArray(methodModels);
    }

    /// <summary>Builds the non-Refit method models from the interface's direct and derived methods.</summary>
    /// <param name="nonRefitMethods">The non-Refit methods declared directly on the interface.</param>
    /// <param name="derivedNonRefitMethods">The non-Refit methods inherited from base interfaces.</param>
    /// <returns>The non-Refit method models.</returns>
    private static ImmutableEquatableArray<MethodModel> BuildNonRefitMethodModels(
        List<IMethodSymbol> nonRefitMethods,
        List<IMethodSymbol> derivedNonRefitMethods)
    {
        // Only abstract instance methods become non-Refit method models.
        var methodModels = new MethodModel[nonRefitMethods.Count + derivedNonRefitMethods.Count];
        var count = 0;
        foreach (var method in nonRefitMethods)
        {
            if (IsEmittableNonRefitMethod(method))
            {
                methodModels[count++] = ParseNonRefitMethod(method, false);
            }
        }

        foreach (var method in derivedNonRefitMethods)
        {
            if (IsEmittableNonRefitMethod(method))
            {
                // Derived non-Refit methods are emitted as explicit interface implementations.
                methodModels[count++] = ParseNonRefitMethod(method, true);
            }
        }

        return TrimAndWrap(methodModels, count);
    }

    /// <summary>Determines whether a non-Refit method should be emitted as a method model.</summary>
    /// <param name="method">The candidate method.</param>
    /// <returns><see langword="true"/> if the method is an abstract instance method; otherwise, <see langword="false"/>.</returns>
    private static bool IsEmittableNonRefitMethod(IMethodSymbol method) =>
        !method.IsStatic
        && method.MethodKind != MethodKind.PropertyGet
        && method.MethodKind != MethodKind.PropertySet
        && method.IsAbstract;

    /// <summary>Builds a method model for a non-Refit interface method.</summary>
    /// <param name="methodSymbol">The non-Refit method symbol.</param>
    /// <param name="isDerived">Whether the method comes from a base interface.</param>
    /// <returns>The model describing the non-Refit method.</returns>
    private static MethodModel ParseNonRefitMethod(
        IMethodSymbol methodSymbol,
        bool isDerived)
    {
        // Derived base-interface methods are emitted as explicit implementations.
        var explicitImpl = FirstExplicitInterfaceImplementation(methodSymbol);
        var containingTypeSymbol = explicitImpl?.ContainingType ?? methodSymbol.ContainingType;
        var containingType = containingTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var declaredBaseName = BuildDeclaredBaseName(methodSymbol);
        var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var returnTypeInfo = GetReturnTypeInfo(methodSymbol);
        var parameters = ParseParameters(methodSymbol.Parameters);

        var isExplicit = isDerived || explicitImpl is not null;
        var constraints = GenerateConstraints(methodSymbol.TypeParameters, isExplicit);

        return new(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredBaseName,
            returnTypeInfo,
            RequestModel.Empty,
            parameters,
            constraints,
            isExplicit);
    }

    /// <summary>Gets the first explicit interface implementation for a method, if one exists.</summary>
    /// <param name="methodSymbol">The method symbol to inspect.</param>
    /// <returns>The first explicit interface implementation, or <see langword="null"/> when there is none.</returns>
    private static IMethodSymbol? FirstExplicitInterfaceImplementation(IMethodSymbol methodSymbol)
    {
        var implementations = methodSymbol.ExplicitInterfaceImplementations;
        return implementations.IsEmpty ? null : implementations[0];
    }

    /// <summary>Classifies a method's return type into its <see cref="ReturnTypeInfo"/> shape.</summary>
    /// <param name="methodSymbol">The method symbol.</param>
    /// <returns>The classified return type information.</returns>
    private static ReturnTypeInfo GetReturnTypeInfo(IMethodSymbol methodSymbol) =>
        methodSymbol.ReturnType.MetadataName switch
        {
            "Task" => ReturnTypeInfo.AsyncVoid,
            "Task`1" or "ValueTask`1" => ReturnTypeInfo.AsyncResult,
            "IAsyncEnumerable`1" => ReturnTypeInfo.AsyncEnumerable,
            "Void" => ReturnTypeInfo.SyncVoid,
            _ => ReturnTypeInfo.Return
        };

    /// <summary>Builds the constraint models for a set of type parameters.</summary>
    /// <param name="typeParameters">The type parameters to generate constraints for.</param>
    /// <param name="isOverrideOrExplicitImplementation">Whether the member is an override or explicit implementation.</param>
    /// <returns>The constraint models for the type parameters.</returns>
    private static ImmutableEquatableArray<TypeConstraint> GenerateConstraints(
        in ImmutableArray<ITypeParameterSymbol> typeParameters,
        bool isOverrideOrExplicitImplementation)
    {
        if (typeParameters.IsEmpty)
        {
            return ImmutableEquatableArrayFactory.Empty<TypeConstraint>();
        }

        var constraints = new TypeConstraint[typeParameters.Length];
        for (var i = 0; i < typeParameters.Length; i++)
        {
            constraints[i] = ParseConstraintsForTypeParameter(
                typeParameters[i],
                isOverrideOrExplicitImplementation);
        }

        return ImmutableEquatableArrayFactory.FromArray(constraints);
    }

    /// <summary>Builds the constraint model for a single type parameter.</summary>
    /// <param name="typeParameter">The type parameter to parse.</param>
    /// <param name="isOverrideOrExplicitImplementation">Whether the member is an override or explicit implementation.</param>
    /// <returns>The constraint model for the type parameter.</returns>
    private static TypeConstraint ParseConstraintsForTypeParameter(
        ITypeParameterSymbol typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        var known = ComputeKnownConstraints(typeParameter, isOverrideOrExplicitImplementation);

        var constraints = ImmutableEquatableArray<string>.Empty;
        if (!isOverrideOrExplicitImplementation)
        {
            constraints = ParseConstraintTypes(typeParameter.ConstraintTypes);
        }

        var declaredName = typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new(typeParameter.Name, declaredName, known, constraints);
    }

    /// <summary>Computes the set of well-known constraint flags emittable for a type parameter.</summary>
    /// <param name="typeParameter">The type parameter to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">Whether the member is an override or explicit implementation.</param>
    /// <returns>The combined well-known constraint flags.</returns>
    private static KnownTypeConstraint ComputeKnownConstraints(
        ITypeParameterSymbol typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        // Explicit implementations and overrides can only emit the subset of constraints that
        // the generated member declaration is allowed to repeat.
        var known = KnownTypeConstraint.None;

        if (typeParameter.HasReferenceTypeConstraint)
        {
            known |= KnownTypeConstraint.Class;
        }

        if (typeParameter.HasUnmanagedTypeConstraint && !isOverrideOrExplicitImplementation)
        {
            known |= KnownTypeConstraint.Unmanaged;
        }

        // `unmanaged` already implies `struct`, so avoid duplicating it.
        if (typeParameter.HasValueTypeConstraint && !typeParameter.HasUnmanagedTypeConstraint)
        {
            known |= KnownTypeConstraint.Struct;
        }

        if (typeParameter.HasNotNullConstraint && !isOverrideOrExplicitImplementation)
        {
            known |= KnownTypeConstraint.NotNull;
        }

        // The `new()` constraint must be emitted last.
        if (typeParameter.HasConstructorConstraint && !isOverrideOrExplicitImplementation)
        {
            known |= KnownTypeConstraint.New;
        }

        return known;
    }

    /// <summary>Builds a parameter model from a parameter symbol.</summary>
    /// <param name="param">The parameter symbol to parse.</param>
    /// <returns>The model describing the parameter.</returns>
    private static ParameterModel ParseParameter(IParameterSymbol param)
    {
        var annotation =
            !param.Type.IsValueType && param.NullableAnnotation == NullableAnnotation.Annotated;

        var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isGeneric = ContainsTypeParameter(param.Type);

        return new(param.MetadataName, paramType, annotation, isGeneric);
    }

    /// <summary>Builds parameter models from a fixed Roslyn parameter array.</summary>
    /// <param name="parameters">The parameters to parse.</param>
    /// <returns>The parsed parameter models.</returns>
    private static ImmutableEquatableArray<ParameterModel> ParseParameters(
        in ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsEmpty)
        {
            return ImmutableEquatableArrayFactory.Empty<ParameterModel>();
        }

        var parameterModels = new ParameterModel[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameterModels[i] = ParseParameter(parameters[i]);
        }

        return ImmutableEquatableArrayFactory.FromArray(parameterModels);
    }

    /// <summary>Builds constraint display names from a fixed Roslyn type array.</summary>
    /// <param name="constraintTypes">The constraint types to parse.</param>
    /// <returns>The parsed constraint type display names.</returns>
    private static ImmutableEquatableArray<string> ParseConstraintTypes(
        in ImmutableArray<ITypeSymbol> constraintTypes)
    {
        if (constraintTypes.IsEmpty)
        {
            return ImmutableEquatableArrayFactory.Empty<string>();
        }

        var constraints = new string[constraintTypes.Length];
        for (var i = 0; i < constraintTypes.Length; i++)
        {
            constraints[i] = constraintTypes[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return ImmutableEquatableArrayFactory.FromArray(constraints);
    }

    /// <summary>Determines whether a type or any of its type arguments is a type parameter.</summary>
    /// <param name="symbol">The type symbol to inspect.</param>
    /// <returns><see langword="true"/> if the type involves a type parameter; otherwise, <see langword="false"/>.</returns>
    private static bool ContainsTypeParameter(ITypeSymbol symbol)
    {
        if (symbol is ITypeParameterSymbol)
        {
            return true;
        }

        if (symbol is not INamedTypeSymbol { TypeParameters.Length: > 0 } namedType)
        {
            return false;
        }

        foreach (var typeArg in namedType.TypeArguments)
        {
            if (ContainsTypeParameter(typeArg))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds a method model for a Refit interface method.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="isImplicitInterface">Whether the method belongs to the implicitly implemented interface.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The model describing the Refit method.</returns>
    private static MethodModel ParseMethod(
        IMethodSymbol methodSymbol,
        bool isImplicitInterface,
        in InterfaceGenerationContext context)
    {
        var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // For explicit interface implementations, emit the interface being implemented, not the
        // interface that originally declared the method.
        var explicitImpl = FirstExplicitInterfaceImplementation(methodSymbol);
        var containingTypeSymbol = explicitImpl?.ContainingType ?? methodSymbol.ContainingType;
        var containingType = containingTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var declaredBaseName = BuildDeclaredBaseName(methodSymbol);
        var returnTypeInfo = GetReturnTypeInfo(methodSymbol);
        var request = ParseRequest(methodSymbol, returnTypeInfo, context);
        var parameters = ParseParameters(methodSymbol.Parameters);

        var isExplicit = explicitImpl is not null;
        var constraints = GenerateConstraints(methodSymbol.TypeParameters, isExplicit || !isImplicitInterface);

        return new(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredBaseName,
            returnTypeInfo,
            request,
            parameters,
            constraints,
            isExplicit);
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

    /// <summary>The interface's methods partitioned into Refit and non-Refit sets.</summary>
    /// <param name="NonRefitMethods">The non-Refit methods declared directly on the interface.</param>
    /// <param name="DerivedRefitMethods">The Refit methods inherited from base interfaces.</param>
    /// <param name="DerivedNonRefitMethods">The non-Refit methods inherited from base interfaces.</param>
    /// <param name="HasDispose">Whether the interface inherits <c>IDisposable.Dispose</c>.</param>
    private readonly record struct MethodPartition(
        List<IMethodSymbol> NonRefitMethods,
        List<IMethodSymbol> DerivedRefitMethods,
        List<IMethodSymbol> DerivedNonRefitMethods,
        bool HasDispose);
}
