// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses a Refit method's type-parameter constraints, parameters, and resulting model.</summary>
internal static partial class Parser
{
    /// <summary>Builds the constraint models for a set of type parameters.</summary>
    /// <param name="typeParameters">The type parameters to generate constraints for.</param>
    /// <param name="isOverrideOrExplicitImplementation">Whether the member is an override or explicit implementation.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased constraint types.</param>
    /// <returns>The constraint models for the type parameters.</returns>
    internal static ImmutableEquatableArray<TypeConstraint> GenerateConstraints(
        in ImmutableArray<ITypeParameterSymbol> typeParameters,
        bool isOverrideOrExplicitImplementation,
        InterfaceGenerationContext context)
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
                isOverrideOrExplicitImplementation,
                context);
        }

        return ImmutableEquatableArrayFactory.FromArray(constraints);
    }

    /// <summary>Builds the constraint model for a single type parameter.</summary>
    /// <param name="typeParameter">The type parameter to parse.</param>
    /// <param name="isOverrideOrExplicitImplementation">Whether the member is an override or explicit implementation.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased constraint types.</param>
    /// <returns>The constraint model for the type parameter.</returns>
    internal static TypeConstraint ParseConstraintsForTypeParameter(
        ITypeParameterSymbol typeParameter,
        bool isOverrideOrExplicitImplementation,
        InterfaceGenerationContext context)
    {
        var known = ComputeKnownConstraints(typeParameter, isOverrideOrExplicitImplementation);

        var constraints = ImmutableEquatableArray<string>.Empty;
        if (!isOverrideOrExplicitImplementation)
        {
            constraints = ParseConstraintTypes(typeParameter.ConstraintTypes, context);
        }

        var declaredName = typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new(typeParameter.Name, declaredName, known, constraints);
    }

    /// <summary>Computes the set of well-known constraint flags emittable for a type parameter.</summary>
    /// <param name="typeParameter">The type parameter to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">Whether the member is an override or explicit implementation.</param>
    /// <returns>The combined well-known constraint flags.</returns>
    internal static KnownTypeConstraint ComputeKnownConstraints(
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
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The model describing the parameter.</returns>
    internal static ParameterModel ParseParameter(IParameterSymbol param, InterfaceGenerationContext context)
    {
        var annotation =
            !param.Type.IsValueType && param.NullableAnnotation == NullableAnnotation.Annotated;

        var paramType = QualifyType(param.Type, context);
        var isGeneric = ContainsTypeParameter(param.Type);

        return new(param.MetadataName, paramType, annotation, isGeneric);
    }

    /// <summary>Builds parameter models from a fixed Roslyn parameter array.</summary>
    /// <param name="parameters">The parameters to parse.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The parsed parameter models.</returns>
    internal static ImmutableEquatableArray<ParameterModel> ParseParameters(
        in ImmutableArray<IParameterSymbol> parameters,
        InterfaceGenerationContext context)
    {
        if (parameters.IsEmpty)
        {
            return ImmutableEquatableArrayFactory.Empty<ParameterModel>();
        }

        var parameterModels = new ParameterModel[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameterModels[i] = ParseParameter(parameters[i], context);
        }

        return ImmutableEquatableArrayFactory.FromArray(parameterModels);
    }

    /// <summary>Builds constraint display names from a fixed Roslyn type array.</summary>
    /// <param name="constraintTypes">The constraint types to parse.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased constraint types.</param>
    /// <returns>The parsed constraint type display names.</returns>
    internal static ImmutableEquatableArray<string> ParseConstraintTypes(
        in ImmutableArray<ITypeSymbol> constraintTypes,
        InterfaceGenerationContext context)
    {
        if (constraintTypes.IsEmpty)
        {
            return ImmutableEquatableArrayFactory.Empty<string>();
        }

        var constraints = new string[constraintTypes.Length];
        for (var i = 0; i < constraintTypes.Length; i++)
        {
            constraints[i] = QualifyType(constraintTypes[i], context);
        }

        return ImmutableEquatableArrayFactory.FromArray(constraints);
    }

    /// <summary>Determines whether a type or any of its type arguments is a type parameter.</summary>
    /// <param name="symbol">The type symbol to inspect.</param>
    /// <returns><see langword="true"/> if the type involves a type parameter; otherwise, <see langword="false"/>.</returns>
    internal static bool ContainsTypeParameter(ITypeSymbol symbol)
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
    internal static MethodModel ParseMethod(
        IMethodSymbol methodSymbol,
        bool isImplicitInterface,
        InterfaceGenerationContext context)
    {
        var returnType = QualifyType(methodSymbol.ReturnType, context);

        // For explicit interface implementations, emit the interface being implemented, not the
        // interface that originally declared the method.
        var explicitImpl = FirstExplicitInterfaceImplementation(methodSymbol);
        var containingTypeSymbol = explicitImpl?.ContainingType ?? methodSymbol.ContainingType;
        var containingType = QualifyType(containingTypeSymbol, context);

        var declaredBaseName = BuildDeclaredBaseName(methodSymbol);
        var returnTypeInfo = GetReturnTypeInfo(methodSymbol);
        var request = ParseRequest(methodSymbol, returnTypeInfo, context);
        var parameters = ParseParameters(methodSymbol.Parameters, context);

        var isExplicit = explicitImpl is not null;
        var constraints = GenerateConstraints(methodSymbol.TypeParameters, isExplicit || !isImplicitInterface, context);

        return new(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredBaseName,
            returnTypeInfo,
            request,
            parameters,
            constraints,
            isExplicit,
            HasTrimAnnotation(methodSymbol, "RequiresUnreferencedCodeAttribute"),
            HasTrimAnnotation(methodSymbol, "RequiresDynamicCodeAttribute"));
    }

    /// <summary>Determines whether a method declares a <c>System.Diagnostics.CodeAnalysis</c> trim annotation.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="attributeName">The attribute type name, for example <c>RequiresUnreferencedCodeAttribute</c>.</param>
    /// <returns><see langword="true"/> when the method carries the attribute.</returns>
    internal static bool HasTrimAnnotation(IMethodSymbol methodSymbol, string attributeName)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass!;
            if (attributeClass.Name == attributeName
                && attributeClass.ContainingNamespace.ToDisplayString() == "System.Diagnostics.CodeAnalysis")
            {
                return true;
            }
        }

        return false;
    }
}
