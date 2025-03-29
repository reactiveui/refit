using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

        var wellKnownTypes = new WellKnownTypes(compilation);

        refitInternalNamespace = $"{refitInternalNamespace ?? string.Empty}RefitInternalGenerated";

        // Remove - as they are valid in csproj, but invalid in a namespace
        refitInternalNamespace = refitInternalNamespace.Replace('-', '_').Replace('@', '_');

        // we're going to create a new compilation that contains the attribute.
        // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
        var options = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;

        var disposableInterfaceSymbol = wellKnownTypes.Get(typeof(IDisposable));
        var httpMethodBaseAttributeSymbol = wellKnownTypes.TryGet(
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

        // Check the candidates and keep the ones we're actually interested in

#pragma warning disable RS1024 // Compare symbols correctly
        var interfaceToNullableEnabledMap = new Dictionary<INamedTypeSymbol, bool>(
            SymbolEqualityComparer.Default
        );
#pragma warning restore RS1024 // Compare symbols correctly
        var methodSymbols = new List<IMethodSymbol>();
        foreach (var group in candidateMethods.GroupBy(m => m.SyntaxTree))
        {
            var model = compilation.GetSemanticModel(group.Key);
            foreach (var method in group)
            {
                // Get the symbol being declared by the method
                var methodSymbol = model.GetDeclaredSymbol(
                    method,
                    cancellationToken: cancellationToken
                );
                if (!IsRefitMethod(methodSymbol, httpMethodBaseAttributeSymbol))
                    continue;

                var isAnnotated =
                    compilation.Options.NullableContextOptions == NullableContextOptions.Enable
                    || model.GetNullableContext(method.SpanStart) == NullableContext.Enabled;
                interfaceToNullableEnabledMap[methodSymbol!.ContainingType] = isAnnotated;

                methodSymbols.Add(methodSymbol!);
            }
        }

        var interfaces = methodSymbols
            .GroupBy<IMethodSymbol, INamedTypeSymbol>(
                m => m.ContainingType,
                SymbolEqualityComparer.Default
            )
            .ToDictionary<
                IGrouping<INamedTypeSymbol, IMethodSymbol>,
                INamedTypeSymbol,
                List<IMethodSymbol>
            >(g => g.Key, v => [.. v], SymbolEqualityComparer.Default);

        // Look through the candidate interfaces
        var interfaceSymbols = new List<INamedTypeSymbol>();
        foreach (var group in candidateInterfaces.GroupBy(i => i.SyntaxTree))
        {
            var model = compilation.GetSemanticModel(group.Key);
            foreach (var iface in group)
            {
                // get the symbol belonging to the interface
                var ifaceSymbol = model.GetDeclaredSymbol(
                    iface,
                    cancellationToken: cancellationToken
                );

                // See if we already know about it, might be a dup
                if (ifaceSymbol is null || interfaces.ContainsKey(ifaceSymbol))
                    continue;

                // The interface has no refit methods, but its base interfaces might
                var hasDerivedRefit = ifaceSymbol
                    .AllInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
                    .Any(m => IsRefitMethod(m, httpMethodBaseAttributeSymbol));

                if (hasDerivedRefit)
                {
                    // Add the interface to the generation list with an empty set of methods
                    // The logic already looks for base refit methods
                    interfaces.Add(ifaceSymbol, []);
                    var isAnnotated =
                        model.GetNullableContext(iface.SpanStart) == NullableContext.Enabled;

                    interfaceToNullableEnabledMap[ifaceSymbol] = isAnnotated;
                }
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

        var attributeText =
            @$"
#pragma warning disable
namespace {refitInternalNamespace}
{{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [global::System.AttributeUsage (global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Delegate)]
    sealed class PreserveAttribute : global::System.Attribute
    {{
        //
        // Fields
        //
        public bool AllMembers;

        public bool Conditional;
    }}
}}
#pragma warning restore
";

        // TODO: Delete?
        // Is it necessary to add the attributes to the compilation now, does it affect the users ide experience?
        // Is it needed in order to get the preserve attribute display name.
        // Will the compilation ever change this.
        compilation = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(
                SourceText.From(attributeText, Encoding.UTF8),
                options,
                cancellationToken: cancellationToken
            )
        );

        // get the newly bound attribute
        var preserveAttributeSymbol = compilation.GetTypeByMetadataName(
            $"{refitInternalNamespace}.PreserveAttribute"
        )!;

        var preserveAttributeDisplayName = preserveAttributeSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );

        var interfaceModels = new List<InterfaceModel>();
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

    static InterfaceModel ProcessInterface(
        string fileName,
        List<Diagnostic> diagnostics,
        INamedTypeSymbol interfaceSymbol,
        List<IMethodSymbol> refitMethods,
        string preserveAttributeDisplayName,
        ISymbol disposableInterfaceSymbol,
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

        // Get any other methods on the refit interfaces. We'll need to generate something for them and warn
        var nonRefitMethods = interfaceSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Except(refitMethods, SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToArray();

        // get methods for all inherited
        var derivedMethods = interfaceSymbol
            .AllInterfaces.SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .ToList();

        // Look for disposable
        var disposeMethod = derivedMethods.Find(
            m =>
                m.ContainingType?.Equals(disposableInterfaceSymbol, SymbolEqualityComparer.Default)
                == true
        );
        if (disposeMethod != null)
        {
            //remove it from the derived methods list so we don't process it with the rest
            derivedMethods.Remove(disposeMethod);
        }

        // Pull out the refit methods from the derived types
        var derivedRefitMethods = derivedMethods
            .Where(m => IsRefitMethod(m, httpMethodBaseAttributeSymbol))
            .ToArray();
        var derivedNonRefitMethods = derivedMethods
            .Except(derivedRefitMethods, SymbolEqualityComparer.Default)
            .Cast<IMethodSymbol>()
            .ToArray();

        var memberNames = interfaceSymbol
            .GetMembers()
            .Select(x => x.Name)
            .Distinct()
            .ToImmutableEquatableArray();

        // Handle Refit Methods
        var refitMethodsArray = refitMethods
            .Select(m => ParseMethod(m, true))
            .ToImmutableEquatableArray();
        var derivedRefitMethodsArray = refitMethods
            .Concat(derivedRefitMethods)
            .Select(m => ParseMethod(m, false))
            .ToImmutableEquatableArray();

        // Handle non-refit Methods that aren't static or properties or have a method body
        var nonRefitMethodModelList = new List<MethodModel>();
        foreach (var method in nonRefitMethods.Concat(derivedNonRefitMethods))
        {
            if (
                method.IsStatic
                || method.MethodKind == MethodKind.PropertyGet
                || method.MethodKind == MethodKind.PropertySet
                || !method.IsAbstract
            ) // If an interface method has a body, it won't be abstract
                continue;

            nonRefitMethodModelList.Add(ParseNonRefitMethod(method, diagnostics));
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
        List<Diagnostic> diagnostics
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

        return ParseMethod(methodSymbol, false);
    }

    private static bool IsRefitMethod(
        IMethodSymbol? methodSymbol,
        INamedTypeSymbol httpMethodAttribute
    )
    {
        return methodSymbol
                ?.GetAttributes()
                .Any(ad => ad.AttributeClass?.InheritsFromOrEquals(httpMethodAttribute) == true)
            == true;
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

        var containingType = methodSymbol.ContainingType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        var declaredMethod = methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var returnTypeInfo = methodSymbol.ReturnType.MetadataName switch
        {
            "Task" => ReturnTypeInfo.AsyncVoid,
            "Task`1" or "ValueTask`1" => ReturnTypeInfo.AsyncResult,
            _ => ReturnTypeInfo.Return,
        };

        var parameters = methodSymbol.Parameters.Select(ParseParameter).ToImmutableEquatableArray();

        var constraints = GenerateConstraints(methodSymbol.TypeParameters, !isImplicitInterface);

        return new MethodModel(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredMethod,
            returnTypeInfo,
            parameters,
            constraints
        );
    }
}
