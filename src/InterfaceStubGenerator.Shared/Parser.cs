using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

internal static class Parser
{
    /// <summary>
    /// Generates the interface stubs.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="refitInternalNamespace">The refit internal namespace.</param>
    /// <param name="candidateMethods">The candidate methods.</param>
    /// <param name="candidateInterfaces">The candidate interfaces.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static (
        List<Diagnostic> diagnostics,
        ContextGenerationModel contextGenerationSpec
    ) GenerateInterfaceStubs(
        CSharpCompilation compilation,
        string? refitInternalNamespace,
        ImmutableArray<MethodDeclarationSyntax> candidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> candidateInterfaces,
        CancellationToken cancellationToken
    )
    {
        if (compilation == null)
            throw new ArgumentNullException(nameof(compilation));

        refitInternalNamespace = $"{refitInternalNamespace ?? string.Empty}RefitInternalGenerated";

        // Remove - as they are valid in csproj, but invalid in a namespace
        refitInternalNamespace = refitInternalNamespace.Replace('-', '_').Replace('@', '_');

        var options = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;

        // Resolve the handful of well-known symbols directly. The previous WellKnownTypes wrapper
        // allocated a dictionary-backed cache object every pass for just these two lookups.
        var disposableInterfaceSymbol = compilation.GetTypeByMetadataName("System.IDisposable");
        var httpMethodBaseAttributeSymbol = compilation.GetTypeByMetadataName(
            "Refit.HttpMethodAttribute"
        );

        var diagnostics = new List<Diagnostic>();
        if (httpMethodBaseAttributeSymbol == null)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.RefitNotReferenced, null));
            return (
                diagnostics,
                new ContextGenerationModel(
                    refitInternalNamespace,
                    string.Empty,
                    ImmutableEquatableArray.Empty<InterfaceModel>()
                )
            );
        }

        // Check the candidates and keep the ones we're actually interested in. LINQ (GroupBy,
        // ToDictionary, SelectMany) is deliberately avoided here: this runs on every generator
        // pass and the iterator/closure allocations add up. Group refit methods by their declaring
        // interface as they are discovered, building the dictionary in a single pass.
#pragma warning disable RS1024 // Compare symbols correctly
        var interfaceToNullableEnabledMap = new Dictionary<INamedTypeSymbol, bool>(
            SymbolEqualityComparer.Default
        );
        var interfaces = new Dictionary<INamedTypeSymbol, List<IMethodSymbol>>(
            SymbolEqualityComparer.Default
        );
#pragma warning restore RS1024 // Compare symbols correctly

        var compilationNullableEnabled =
            compilation.Options.NullableContextOptions == NullableContextOptions.Enable;

        foreach (var method in candidateMethods)
        {
            // GetSemanticModel is cached per syntax tree, so calling it per method is cheap and
            // lets us skip a GroupBy-by-tree allocation.
            var model = compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = model.GetDeclaredSymbol(method, cancellationToken: cancellationToken);
            if (!IsRefitMethod(methodSymbol, httpMethodBaseAttributeSymbol))
                continue;

            var containingType = methodSymbol!.ContainingType;
            interfaceToNullableEnabledMap[containingType] =
                compilationNullableEnabled
                || model.GetNullableContext(method.SpanStart) == NullableContext.Enabled;

            if (!interfaces.TryGetValue(containingType, out var interfaceMethods))
            {
                interfaceMethods = [];
                interfaces[containingType] = interfaceMethods;
            }

            interfaceMethods.Add(methodSymbol!);
        }

        // Look through the candidate interfaces for ones whose Refit methods are only inherited.
        foreach (var iface in candidateInterfaces)
        {
            var model = compilation.GetSemanticModel(iface.SyntaxTree);
            var ifaceSymbol = model.GetDeclaredSymbol(iface, cancellationToken: cancellationToken);

            // See if we already know about it, might be a dup
            if (ifaceSymbol is null || interfaces.ContainsKey(ifaceSymbol))
                continue;

            // The interface has no refit methods of its own, but its base interfaces might.
            if (HasDerivedRefitMethods(ifaceSymbol, httpMethodBaseAttributeSymbol))
            {
                // Add the interface to the generation list with an empty set of methods;
                // the logic already looks for base refit methods.
                interfaces.Add(ifaceSymbol, []);
                interfaceToNullableEnabledMap[ifaceSymbol] =
                    model.GetNullableContext(iface.SpanStart) == NullableContext.Enabled;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Bail out if there aren't any interfaces to generate code for. This may be the case with transitives
        if (interfaces.Count == 0)
            return (
                diagnostics,
                new ContextGenerationModel(
                    refitInternalNamespace,
                    string.Empty,
                    ImmutableEquatableArray.Empty<InterfaceModel>()
                )
            );

        var supportsNullable = options.LanguageVersion >= LanguageVersion.CSharp8;

        var keyCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // The PreserveAttribute is emitted into the consumer's compilation by the emitter
        // (see Emitter.EmitSharedCode). Its fully-qualified display name is fully determined
        // by refitInternalNamespace, so we compute it directly instead of round-tripping
        // through compilation.AddSyntaxTrees + GetTypeByMetadataName + ToDisplayString on
        // every generator pass, which mutated the compilation and forced an extra bind.
        var preserveAttributeDisplayName = $"global::{refitInternalNamespace}.PreserveAttribute";

        var interfaceModels = new List<InterfaceModel>(interfaces.Count);
        // group the fields by interface and generate the source
        foreach (var group in interfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // each group is keyed by the Interface INamedTypeSymbol and contains the members
            // with a refit attribute on them. Types may contain other members, without the attribute, which we'll
            // need to check for and error out on
            var keyName = group.Key.Name;
            int value;
            while (keyCount.TryGetValue(keyName, out value))
            {
                keyName = $"{keyName}{++value}";
            }
            keyCount[keyName] = value;
            var fileName = $"{keyName}.g.cs";

            var interfaceModel = ProcessInterface(
                fileName,
                diagnostics,
                group.Key,
                group.Value,
                preserveAttributeDisplayName,
                disposableInterfaceSymbol,
                httpMethodBaseAttributeSymbol,
                supportsNullable,
                interfaceToNullableEnabledMap[group.Key]
            );

            interfaceModels.Add(interfaceModel);
        }

        var contextGenerationSpec = new ContextGenerationModel(
            refitInternalNamespace,
            preserveAttributeDisplayName,
            interfaceModels.ToImmutableEquatableArray()
        );
        return (diagnostics, contextGenerationSpec);
    }

    [SuppressMessage(
        "Reliability",
        "CA1508:Avoid dead conditional code",
        Justification = "False positive: the analyzer does not track the dispose method assigned in an earlier iteration of the inherited-member loop, so the null checks are not dead."
    )]
    static InterfaceModel ProcessInterface(
        string fileName,
        List<Diagnostic> diagnostics,
        INamedTypeSymbol interfaceSymbol,
        List<IMethodSymbol> refitMethods,
        string preserveAttributeDisplayName,
        ISymbol? disposableInterfaceSymbol,
        INamedTypeSymbol httpMethodBaseAttributeSymbol,
        bool supportsNullable,
        bool nullableEnabled
    )
    {
        // Get the class name with the type parameters, then remove the namespace
        var className = interfaceSymbol.ToDisplayString();
        var lastDot = className.LastIndexOf('.');
        if (lastDot > 0)
        {
            className = className.Substring(lastDot + 1);
        }
        var classDeclaration = $"{interfaceSymbol.ContainingType?.Name}{className}";

        // Get the class name itself
        var classSuffix = $"{interfaceSymbol.ContainingType?.Name}{interfaceSymbol.Name}";
        var ns = interfaceSymbol.ContainingNamespace?.ToDisplayString();

        // if it's the global namespace, our lookup rules say it should be the same as the class name
        if (interfaceSymbol.ContainingNamespace is { IsGlobalNamespace: true })
        {
            ns = string.Empty;
        }

        // Remove dots
        ns = ns!.Replace(".", "");
        var interfaceDisplayName = interfaceSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        // Get any other (non-Refit) methods declared directly on the interface. LINQ is avoided
        // throughout this method because it runs for every interface on every generator pass; the
        // HashSets below reproduce the de-duplicating semantics of the previous Except/Distinct.
        var members = interfaceSymbol.GetMembers();
        var refitMethodSet = new HashSet<IMethodSymbol>(
            refitMethods,
            SymbolEqualityComparer.Default
        );
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

        // Walk all inherited interfaces once, pulling out the IDisposable.Dispose method and
        // splitting the rest into Refit and non-Refit methods.
        IMethodSymbol? disposeMethod = null;
        var derivedRefitMethods = new List<IMethodSymbol>();
        var derivedNonRefitMethods = new List<IMethodSymbol>();
        var seenDerivedNonRefitMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is not IMethodSymbol method)
                    continue;

                if (
                    disposeMethod is null
                    && method.ContainingType?.Equals(
                        disposableInterfaceSymbol,
                        SymbolEqualityComparer.Default
                    ) == true
                )
                {
                    disposeMethod = method;
                    continue;
                }

                if (IsRefitMethod(method, httpMethodBaseAttributeSymbol))
                    derivedRefitMethods.Add(method);
                else if (seenDerivedNonRefitMethods.Add(method))
                    derivedNonRefitMethods.Add(method);
            }
        }

        // Exclude base interface methods that the current interface explicitly implements.
        // This avoids false positive RF001 diagnostics for cases like:
        // interface IFoo { int Bar(); } and interface IRemoteFoo : IFoo { [Get] abstract int IFoo.Bar(); }
        if (derivedNonRefitMethods.Count > 0)
        {
            var explicitlyImplementedBaseMethods = new HashSet<IMethodSymbol>(
                SymbolEqualityComparer.Default
            );

            foreach (var member in members)
            {
                if (member is not IMethodSymbol method)
                    continue;

                foreach (var baseMethod in method.ExplicitInterfaceImplementations)
                {
                    // Use OriginalDefinition for robustness when comparing generic methods
                    explicitlyImplementedBaseMethods.Add(
                        baseMethod.OriginalDefinition ?? baseMethod
                    );
                }
            }

            if (explicitlyImplementedBaseMethods.Count > 0)
            {
                var filteredDerivedNonRefitMethods = new List<IMethodSymbol>(
                    derivedNonRefitMethods.Count
                );
                foreach (var method in derivedNonRefitMethods)
                {
                    if (
                        !explicitlyImplementedBaseMethods.Contains(
                            method.OriginalDefinition ?? method
                        )
                    )
                    {
                        filteredDerivedNonRefitMethods.Add(method);
                    }
                }

                derivedNonRefitMethods = filteredDerivedNonRefitMethods;
            }
        }

        var seenMemberNames = new HashSet<string>();
        var memberNameList = new List<string>(members.Length);
        foreach (var member in members)
        {
            if (seenMemberNames.Add(member.Name))
                memberNameList.Add(member.Name);
        }

        var memberNames = memberNameList.ToImmutableEquatableArray();

        // Handle Refit Methods
        var refitMethodModels = new List<MethodModel>(refitMethods.Count);
        foreach (var method in refitMethods)
            refitMethodModels.Add(ParseMethod(method, true));
        var refitMethodsArray = refitMethodModels.ToImmutableEquatableArray();

        // Only include refit methods discovered on base interfaces here.
        // Do NOT duplicate the current interface's refit methods.
        var derivedRefitMethodModels = new List<MethodModel>(derivedRefitMethods.Count);
        foreach (var method in derivedRefitMethods)
            derivedRefitMethodModels.Add(ParseMethod(method, false));
        var derivedRefitMethodsArray = derivedRefitMethodModels.ToImmutableEquatableArray();

        // Handle non-refit Methods that aren't static or properties or have a method body
        var nonRefitMethodModelList = new List<MethodModel>(
            nonRefitMethods.Count + derivedNonRefitMethods.Count
        );
        foreach (var method in nonRefitMethods)
        {
            if (
                method.IsStatic
                || method.MethodKind == MethodKind.PropertyGet
                || method.MethodKind == MethodKind.PropertySet
                || !method.IsAbstract
            )
                continue;

            nonRefitMethodModelList.Add(ParseNonRefitMethod(method, diagnostics, isDerived: false));
        }
        foreach (var method in derivedNonRefitMethods)
        {
            if (
                method.IsStatic
                || method.MethodKind == MethodKind.PropertyGet
                || method.MethodKind == MethodKind.PropertySet
                || !method.IsAbstract
            )
                continue;

            // Derived non-refit methods should be emitted as explicit interface implementations
            nonRefitMethodModelList.Add(ParseNonRefitMethod(method, diagnostics, isDerived: true));
        }

        var nonRefitMethodModels = nonRefitMethodModelList.ToImmutableEquatableArray();

        var constraints = GenerateConstraints(interfaceSymbol.TypeParameters, false);
        var hasDispose = disposeMethod != null;
        var nullability = (supportsNullable, nullableEnabled) switch
        {
            (false, _) => Nullability.None,
            (true, true) => Nullability.Enabled,
            (true, false) => Nullability.Disabled,
        };
        return new InterfaceModel(
            preserveAttributeDisplayName,
            fileName,
            className,
            ns,
            classDeclaration,
            interfaceDisplayName,
            classSuffix,
            constraints,
            memberNames,
            nonRefitMethodModels,
            refitMethodsArray,
            derivedRefitMethodsArray,
            nullability,
            hasDispose
        );
    }

    private static MethodModel ParseNonRefitMethod(
        IMethodSymbol methodSymbol,
        List<Diagnostic> diagnostics,
        bool isDerived
    )
    {
        // report invalid error diagnostic
        foreach (var location in methodSymbol.Locations)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidRefitMember,
                location,
                methodSymbol.ContainingType.Name,
                methodSymbol.Name
            );
            diagnostics.Add(diagnostic);
        }

        // Parse like a regular method, but force explicit implementation for derived base-interface methods
        var explicitImpl = methodSymbol.ExplicitInterfaceImplementations.FirstOrDefault();
        var containingTypeSymbol = explicitImpl?.ContainingType ?? methodSymbol.ContainingType;
        var containingType = containingTypeSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        // Method name should be simple name only (never include interface qualifier)
        var declaredBaseName = methodSymbol.Name;
        var lastDot = declaredBaseName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            declaredBaseName = declaredBaseName.Substring(lastDot + 1);
        }

        if (methodSymbol.TypeParameters.Length > 0)
        {
            var typeParams = string.Join(
                ", ",
                methodSymbol.TypeParameters.Select(
                    tp => tp.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                )
            );
            declaredBaseName += $"<{typeParams}>";
        }

        var returnType = methodSymbol.ReturnType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        var returnTypeInfo = methodSymbol.ReturnType.MetadataName switch
        {
            "Task" => ReturnTypeInfo.AsyncVoid,
            "Task`1" or "ValueTask`1" => ReturnTypeInfo.AsyncResult,
            "Void" => ReturnTypeInfo.SyncVoid,
            _ => ReturnTypeInfo.Return,
        };

        var parameters = methodSymbol.Parameters.Select(ParseParameter).ToImmutableEquatableArray();

        var isExplicit = isDerived || explicitImpl is not null;
        var constraints = GenerateConstraints(methodSymbol.TypeParameters, isExplicit);

        return new MethodModel(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredBaseName,
            returnTypeInfo,
            parameters,
            constraints,
            isExplicit
        );
    }

    private static bool IsRefitMethod(
        IMethodSymbol? methodSymbol,
        INamedTypeSymbol httpMethodAttribute
    )
    {
        if (methodSymbol is null)
            return false;

        // Avoid LINQ here: this is called for every candidate method and every inherited member.
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.InheritsFromOrEquals(httpMethodAttribute) == true)
                return true;
        }

        return false;
    }

    private static bool HasDerivedRefitMethods(
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol httpMethodAttribute
    )
    {
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member is IMethodSymbol method && IsRefitMethod(method, httpMethodAttribute))
                    return true;
            }
        }

        return false;
    }

    private static ImmutableEquatableArray<TypeConstraint> GenerateConstraints(
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        bool isOverrideOrExplicitImplementation
    )
    {
        // Need to loop over the constraints and create them
        return typeParameters
            .Select(
                typeParameter =>
                    ParseConstraintsForTypeParameter(
                        typeParameter,
                        isOverrideOrExplicitImplementation
                    )
            )
            .ToImmutableEquatableArray();
    }

    private static TypeConstraint ParseConstraintsForTypeParameter(
        ITypeParameterSymbol typeParameter,
        bool isOverrideOrExplicitImplementation
    )
    {
        // Explicit interface implementations and overrides can only have class or struct constraints
        var known = KnownTypeConstraint.None;

        if (typeParameter.HasReferenceTypeConstraint)
        {
            known |= KnownTypeConstraint.Class;
        }
        if (typeParameter.HasUnmanagedTypeConstraint && !isOverrideOrExplicitImplementation)
        {
            known |= KnownTypeConstraint.Unmanaged;
        }

        // unmanaged constraints are both structs and unmanaged so the struct constraint is redundant
        if (typeParameter.HasValueTypeConstraint && !typeParameter.HasUnmanagedTypeConstraint)
        {
            known |= KnownTypeConstraint.Struct;
        }
        if (typeParameter.HasNotNullConstraint && !isOverrideOrExplicitImplementation)
        {
            known |= KnownTypeConstraint.NotNull;
        }

        var constraints = ImmutableEquatableArray<string>.Empty;
        if (!isOverrideOrExplicitImplementation)
        {
            constraints = typeParameter
                .ConstraintTypes.Select(
                    typeConstraint =>
                        typeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                )
                .ToImmutableEquatableArray();
        }

        // new constraint has to be last
        if (typeParameter.HasConstructorConstraint && !isOverrideOrExplicitImplementation)
        {
            known |= KnownTypeConstraint.New;
        }

        var declaredName = typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new TypeConstraint(typeParameter.Name, declaredName, known, constraints);
    }

    private static ParameterModel ParseParameter(IParameterSymbol param)
    {
        var annotation =
            !param.Type.IsValueType && param.NullableAnnotation == NullableAnnotation.Annotated;

        var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isGeneric = ContainsTypeParameter(param.Type);

        return new ParameterModel(param.MetadataName, paramType, annotation, isGeneric);
    }

    private static bool ContainsTypeParameter(ITypeSymbol symbol)
    {
        if (symbol is ITypeParameterSymbol)
            return true;

        if (symbol is not INamedTypeSymbol { TypeParameters.Length: > 0 } namedType)
            return false;

        foreach (var typeArg in namedType.TypeArguments)
        {
            if (ContainsTypeParameter(typeArg))
                return true;
        }

        return false;
    }

    private static MethodModel ParseMethod(IMethodSymbol methodSymbol, bool isImplicitInterface)
    {
        var returnType = methodSymbol.ReturnType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        // For explicit interface implementations, the containing type for the explicit method signature
        // must be the interface being implemented (e.g. IFoo), not the interface that declares it.
        var explicitImpl = methodSymbol.ExplicitInterfaceImplementations.FirstOrDefault();
        var containingTypeSymbol = explicitImpl?.ContainingType ?? methodSymbol.ContainingType;
        var containingType = containingTypeSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        // Simple method name (strip any explicit interface qualifier if present)
        var declaredBaseName = methodSymbol.Name;
        var lastDot = declaredBaseName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            declaredBaseName = declaredBaseName.Substring(lastDot + 1);
        }

        if (methodSymbol.TypeParameters.Length > 0)
        {
            var typeParams = string.Join(
                ", ",
                methodSymbol.TypeParameters.Select(
                    tp => tp.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                )
            );
            declaredBaseName += $"<{typeParams}>";
        }

        var returnTypeInfo = methodSymbol.ReturnType.MetadataName switch
        {
            "Task" => ReturnTypeInfo.AsyncVoid,
            "Task`1" or "ValueTask`1" => ReturnTypeInfo.AsyncResult,
            "Void" => ReturnTypeInfo.SyncVoid,
            _ => ReturnTypeInfo.Return,
        };

        var parameters = methodSymbol.Parameters.Select(ParseParameter).ToImmutableEquatableArray();

        var isExplicit = explicitImpl is not null;
        var constraints = GenerateConstraints(methodSymbol.TypeParameters, isExplicit || !isImplicitInterface);

        return new MethodModel(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredBaseName,
            returnTypeInfo,
            parameters,
            constraints,
            isExplicit
        );
    }
}
