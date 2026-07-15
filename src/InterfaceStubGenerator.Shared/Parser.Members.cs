// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses interface properties and non-Refit methods into the models used to generate Refit stubs.</summary>
internal static partial class Parser
{
    /// <summary>Builds models for interface properties implemented by the generated stub.</summary>
    /// <param name="members">The directly declared interface members.</param>
    /// <param name="inheritedProperties">The emittable inherited properties collected during the single member walk.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased property types.</param>
    /// <returns>The property models.</returns>
    private static ImmutableEquatableArray<InterfacePropertyModel> BuildInterfacePropertyModels(
        in ImmutableArray<ISymbol> members,
        List<IPropertySymbol> inheritedProperties,
        InterfaceGenerationContext context)
    {
        var properties = new List<InterfacePropertyModel>();
        foreach (var member in members)
        {
            if (member is IPropertySymbol property && IsEmittableProperty(property))
            {
                properties.Add(ParseInterfaceProperty(property, false, context));
            }
        }

        foreach (var property in inheritedProperties)
        {
            properties.Add(ParseInterfaceProperty(property, true, context));
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
    /// <param name="context">The generation context, used to qualify extern-aliased property types.</param>
    /// <returns>The property model.</returns>
    private static InterfacePropertyModel ParseInterfaceProperty(IPropertySymbol property, bool isDerived, InterfaceGenerationContext context)
    {
        var annotation =
            !property.Type.IsValueType && property.NullableAnnotation == NullableAnnotation.Annotated;
        var propertyType = QualifyType(property.Type, context);
        var containingType = QualifyType(property.ContainingType, context);
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
        InterfaceGenerationContext context)
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
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The non-Refit method models.</returns>
    private static ImmutableEquatableArray<MethodModel> BuildNonRefitMethodModels(
        List<IMethodSymbol> nonRefitMethods,
        List<IMethodSymbol> derivedNonRefitMethods,
        InterfaceGenerationContext context)
    {
        // Only abstract instance methods become non-Refit method models.
        var methodModels = new MethodModel[nonRefitMethods.Count + derivedNonRefitMethods.Count];
        var count = 0;
        foreach (var method in nonRefitMethods)
        {
            if (IsEmittableNonRefitMethod(method))
            {
                methodModels[count] = ParseNonRefitMethod(method, false, context);
                count++;
            }
        }

        foreach (var method in derivedNonRefitMethods)
        {
            if (IsEmittableNonRefitMethod(method))
            {
                // Derived non-Refit methods are emitted as explicit interface implementations.
                methodModels[count] = ParseNonRefitMethod(method, true, context);
                count++;
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
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The model describing the non-Refit method.</returns>
    private static MethodModel ParseNonRefitMethod(
        IMethodSymbol methodSymbol,
        bool isDerived,
        InterfaceGenerationContext context)
    {
        // Derived base-interface methods are emitted as explicit implementations.
        var explicitImpl = FirstExplicitInterfaceImplementation(methodSymbol);
        var containingTypeSymbol = explicitImpl?.ContainingType ?? methodSymbol.ContainingType;
        var containingType = QualifyType(containingTypeSymbol, context);

        var declaredBaseName = BuildDeclaredBaseName(methodSymbol);
        var returnType = QualifyType(methodSymbol.ReturnType, context);
        var returnTypeInfo = GetReturnTypeInfo(methodSymbol);
        var parameters = ParseParameters(methodSymbol.Parameters, context);

        var isExplicit = isDerived || explicitImpl is not null;
        var constraints = GenerateConstraints(methodSymbol.TypeParameters, isExplicit, context);

        return new(
            methodSymbol.Name,
            returnType,
            containingType,
            declaredBaseName,
            returnTypeInfo,
            RequestModel.Empty,
            parameters,
            constraints,
            isExplicit,
            RequiresUnreferencedCode: false,
            RequiresDynamicCode: false);
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
            "Task`1" when IsHttpRequestMessageType(((INamedTypeSymbol)methodSymbol.ReturnType).TypeArguments[0]) => ReturnTypeInfo.RequestMessage,
            "Task`1" or "ValueTask`1" => ReturnTypeInfo.AsyncResult,
            "IAsyncEnumerable`1" => ReturnTypeInfo.AsyncEnumerable,
            "IObservable`1" => ReturnTypeInfo.Observable,
            "Void" => ReturnTypeInfo.SyncVoid,
            _ => ReturnTypeInfo.Return
        };

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
                memberNames[count] = member.Name;
                count++;
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
}
