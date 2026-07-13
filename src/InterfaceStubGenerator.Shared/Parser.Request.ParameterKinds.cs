// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Classifies a single request parameter into its specific kind, model, and supporting type predicates.</content>
internal static partial class Parser
{
    /// <summary>Determines whether a type is, or is constructed over, a generic type parameter.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type references a type parameter.</returns>
    private static bool ReferencesTypeParameter(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return ReferencesTypeParameter(array.ElementType);
            case INamedTypeSymbol named:
            {
                foreach (var argument in named.TypeArguments)
                {
                    if (ReferencesTypeParameter(argument))
                    {
                        return true;
                    }
                }

                return false;
            }

            default:
                return false;
        }
    }

    /// <summary>Determines whether a parameter type renders to a URL scalar and is eligible for inline path formatting.</summary>
    /// <param name="type">The parameter type to classify.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <returns><see langword="true"/> when the type is a simple scalar type supported by inline path formatting.</returns>
    private static bool IsSimpleType(ITypeSymbol type, INamedTypeSymbol? formattableSymbol)
    {
        // A path value is emitted as UrlParameterFormatter.Format(value, provider, typeof(T)) - the same call the
        // reflection path uses - so any type the formatter can render round-trips identically. That is exactly the
        // set of IFormattable types (which is also what makes [Query(Format = ...)] and invariant culture work),
        // plus string and bool, which are scalars but not IFormattable. Collections, arrays and DTOs are excluded
        // and fall back to reflection. Matching on the resolved IFormattable symbol avoids per-parameter name-string
        // allocations and automatically covers future BCL scalars.
        var underlyingType = GetUnderlyingType(type);

        static ITypeSymbol GetUnderlyingType(ITypeSymbol type) =>
            type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
                ? nullable.TypeArguments[0]
                : type;

        // The built-in value-type scalars occupy a contiguous SpecialType block - System_Boolean (bool, char,
        // every integer width, decimal, float, double) through System_Double - so a range check covers them all
        // in one comparison. string and DateTime sit just outside that block.
        static bool IsScalarSpecialType(SpecialType specialType) =>
            (specialType >= SpecialType.System_Boolean && specialType <= SpecialType.System_Double)
            || specialType == SpecialType.System_String
            || specialType == SpecialType.System_DateTime;

        // A null interfaceSymbol (System.IFormattable unresolved) simply matches nothing and falls back.
        static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol? interfaceSymbol)
        {
            foreach (var implemented in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implemented, interfaceSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        // Built-in scalars resolve from SpecialType alone (a jump table, no interface walk); everything else that
        // renders to a URL scalar - enums, Guid, DateTimeOffset, DateOnly, TimeOnly, TimeSpan, BigInteger,
        // Int128/UInt128, Half - implements IFormattable.
        return IsScalarSpecialType(underlyingType.SpecialType)
               || ImplementsInterface(underlyingType, formattableSymbol)
               || IsUri(underlyingType)
               || IsCultureInfo(underlyingType);
    }

    /// <summary>Determines whether a type is <see cref="Uri"/>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is <see cref="Uri"/>.</returns>
    /// <remarks>
    /// The reflection request builder treats <see cref="Uri"/> as a query scalar rather than an object to flatten
    /// (its <c>ShouldReturn</c> check), even though it is not <see cref="IFormattable"/>. The default formatter renders
    /// it through <c>string.Format("{0}", value)</c>, which is <c>ToString()</c> for a non-formattable value, so the
    /// generated fast path matches exactly.
    /// </remarks>
    private static bool IsUri(ITypeSymbol type) =>
        type is
        {
            Name: "Uri",
            ContainingNamespace.Name: SystemNamespace,
            ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
        };

    /// <summary>Determines whether a type is <see cref="System.Globalization.CultureInfo"/> or derives from it.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is assignable to <see cref="System.Globalization.CultureInfo"/>.</returns>
    /// <remarks>
    /// Mirrors the reflection builder's <c>typeof(CultureInfo).IsAssignableFrom(type)</c>, so derived cultures are
    /// scalars too rather than objects whose public properties get flattened into the query string.
    /// </remarks>
    private static bool IsCultureInfo(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is
                {
                    Name: "CultureInfo",
                    ContainingNamespace.Name: "Globalization",
                    ContainingNamespace.ContainingNamespace.Name: SystemNamespace,
                    ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
                })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a type is <see cref="CancellationToken"/> or nullable <see cref="CancellationToken"/>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is a cancellation token.</returns>
    private static bool IsCancellationToken(ITypeSymbol type)
    {
        // Structural match instead of allocating a fully-qualified display string for every parameter.
        if (type is INamedTypeSymbol
            {
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
                TypeArguments: [var underlying]
            })
        {
            type = underlying;
        }

        return type is
        {
            Name: "CancellationToken",
            ContainingNamespace.Name: "Threading",
            ContainingNamespace.ContainingNamespace.Name: SystemNamespace,
            ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
        };
    }

    /// <summary>Tries to parse an explicitly attributed body parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="bodyParameter">Receives the body parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a body attribute.</returns>
    private static bool TryParseBodyParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context,
        out RequestParameterModel bodyParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (!IsRefitAttribute(attribute.AttributeClass, BodyAttributeDisplayName))
            {
                continue;
            }

            var bodyInfo = ParseBodyAttribute(attribute);
            var formFields = bodyInfo.SerializationMethod == "UrlEncoded"
                ? TryBuildFormFields(parameter.Type, context)
                : null;
            bodyParameter = new(
                    parameter.MetadataName,
                    parameterType,
                    null,
                    BuildParameterAttributes(parameter, context),
                    RequestParameterKind.Body,
                    CanBeNull(parameter.Type, parameter.NullableAnnotation),
                    string.Empty,
                    string.Empty,
                    bodyInfo.SerializationMethod,
                bodyInfo.BufferMode)
            {
                FormFields = formFields,
            };
            return true;
        }

        // The caller only reads the out value on the true branch, so skip building a discarded
        // Unsupported model (and its per-attribute BuildParameterAttributes allocations) here.
        bodyParameter = null!;
        return false;
    }

    /// <summary>Tries to parse a dynamic header parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="headerParameter">Receives the header parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a supported header attribute.</returns>
    private static bool TryParseHeaderParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context,
        out RequestParameterModel headerParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (!IsRefitAttribute(attribute.AttributeClass, HeaderAttributeDisplayName))
            {
                continue;
            }

            var arguments = attribute.ConstructorArguments;
            if (arguments.IsEmpty || arguments[0].Value is not string headerName ||
                string.IsNullOrWhiteSpace(headerName))
            {
                continue;
            }

            headerParameter = new(
                parameter.MetadataName,
                parameterType,
                null,
                BuildParameterAttributes(parameter, context),
                RequestParameterKind.Header,
                CanBeNull(parameter.Type, parameter.NullableAnnotation),
                headerName.Trim(),
                string.Empty,
                string.Empty,
                BodyBufferMode.None);
            return true;
        }

        headerParameter = null!;
        return false;
    }

    /// <summary>Tries to parse a dynamic header collection parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="headerCollectionParameter">Receives the header collection parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a supported header collection attribute.</returns>
    private static bool TryParseHeaderCollectionParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context,
        out RequestParameterModel headerCollectionParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (!IsRefitAttribute(attribute.AttributeClass, HeaderCollectionAttributeDisplayName))
            {
                continue;
            }

            if (IsSupportedHeaderCollectionType(parameter.Type))
            {
                headerCollectionParameter = new(
                    parameter.MetadataName,
                    parameterType,
                    null,
                    BuildParameterAttributes(parameter, context),
                    RequestParameterKind.HeaderCollection,
                    CanBeNull(parameter.Type, parameter.NullableAnnotation),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    BodyBufferMode.None);
                return true;
            }

            headerCollectionParameter = null!;
            return false;
        }

        headerCollectionParameter = null!;
        return false;
    }

    /// <summary>Tries to parse a request property parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="propertyParameter">Receives the property parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a property attribute.</returns>
    private static bool TryParsePropertyParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context,
        out RequestParameterModel propertyParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (!IsRefitAttribute(attribute.AttributeClass, PropertyAttributeDisplayName))
            {
                continue;
            }

            var arguments = attribute.ConstructorArguments;
            var propertyKey = !arguments.IsEmpty && arguments[0].Value is string { Length: > 0 } key
                ? key
                : parameter.MetadataName;
            propertyParameter = new(
                parameter.MetadataName,
                parameterType,
                null,
                BuildParameterAttributes(parameter, context),
                RequestParameterKind.Property,
                CanBeNull(parameter.Type, parameter.NullableAnnotation),
                string.Empty,
                propertyKey,
                string.Empty,
                BodyBufferMode.None);
            return true;
        }

        propertyParameter = null!;
        return false;
    }

    /// <summary>Builds the <c>Authorization</c> header parameter model for an <c>[Authorize]</c> parameter.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="scheme">The authorization scheme, prepended to the value as <c>"{scheme} "</c>.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The header parameter model that emits <c>Authorization: {scheme} {value}</c>.</returns>
    private static RequestParameterModel BuildAuthorizeHeaderParameter(
        IParameterSymbol parameter,
        string parameterType,
        string scheme,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Header,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            "Authorization",
            string.Empty,
            string.Empty,
            BodyBufferMode.None)
        {
            HeaderValuePrefix = scheme + " ",
        };

    /// <summary>Builds an unsupported request parameter model.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The unsupported parameter model.</returns>
    private static RequestParameterModel UnsupportedRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Unsupported,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None);

    /// <summary>Builds a path request parameter model.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <param name="locations">The parameter's location in the URL.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The path request model.</returns>
    private static RequestParameterModel PathRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range> locations,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            locations,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Path,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None);

    /// <summary>
    /// Flattens a parameter's attributes into value-typed models so the incremental generator cache holds no
    /// Roslyn symbols. Attribute type names and argument expressions are precomputed for the emitter.
    /// </summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The precomputed attribute models.</returns>
    private static ImmutableEquatableArray<ParameterAttributeModel> BuildParameterAttributes(IParameterSymbol parameter, InterfaceGenerationContext context)
    {
        var attributes = parameter.GetAttributes();
        if (attributes.IsEmpty)
        {
            return ImmutableEquatableArray<ParameterAttributeModel>.Empty;
        }

        var models = new List<ParameterAttributeModel>(attributes.Length);
        foreach (var attribute in attributes)
        {
            var constructorArguments = new List<string>(attribute.ConstructorArguments.Length);
            foreach (var argument in attribute.ConstructorArguments)
            {
                constructorArguments.Add(ConstantValueToString(argument, context));
            }

            var namedArguments = new List<NamedAttributeArgument>(attribute.NamedArguments.Length);
            foreach (var named in attribute.NamedArguments)
            {
                namedArguments.Add(new(named.Key, ConstantValueToString(named.Value, context)));
            }

            models.Add(new(
                QualifyType(attribute.AttributeClass!, context),
                constructorArguments.ToImmutableEquatableArray(),
                namedArguments.ToImmutableEquatableArray()));
        }

        return models.ToImmutableEquatableArray();
    }

    /// <summary>Renders a typed constant attribute argument as the C# source expression the emitter writes.</summary>
    /// <param name="argument">The typed constant.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The source expression, or <c>"null"</c> when the value is null.</returns>
    private static string ConstantValueToString(TypedConstant argument, InterfaceGenerationContext context)
    {
        var result = string.Empty;

        if (!argument.IsNull)
        {
            // A non-null attribute argument is always one of Enum, Type, Array or Primitive; the primitive rendering
            // doubles as the fallback so no unreachable throwing arm is needed.
            result = argument.Kind switch
            {
                TypedConstantKind.Enum => $"({QualifyType(argument.Type!, context)}){argument.Value!}",
                TypedConstantKind.Type => $"typeof({QualifyType((ITypeSymbol)argument.Value!, context)})",
                TypedConstantKind.Array => RenderConstantArray(argument, context),
                _ => SymbolDisplay.FormatPrimitive(argument.Value!, true, false)!
            };
        }

        return result.Length > 0 ? result : "null";
    }

    /// <summary>Renders an array-valued attribute argument as a C# array-creation expression.</summary>
    /// <param name="argument">The array typed constant.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The <c>new[] { ... }</c> source expression.</returns>
    private static string RenderConstantArray(TypedConstant argument, InterfaceGenerationContext context)
    {
        var parts = new List<string>(argument.Values.Length);
        foreach (var value in argument.Values)
        {
            parts.Add(ConstantValueToString(value, context));
        }

        return $"new[] {{ {string.Join(", ", parts)} }}";
    }

    /// <summary>Determines whether generated code needs a null-safe dereference for a parameter value.</summary>
    /// <param name="type">The parameter type.</param>
    /// <param name="nullableAnnotation">The parameter nullable annotation.</param>
    /// <returns><see langword="true"/> when generated code should guard the value before dereferencing it.</returns>
    private static bool CanBeNull(ITypeSymbol type, NullableAnnotation nullableAnnotation) =>
        type switch
        {
            INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } => true,
            ITypeParameterSymbol typeParameter => !typeParameter.HasValueTypeConstraint,
            _ => !type.IsValueType || nullableAnnotation == NullableAnnotation.Annotated
        };

    /// <summary>Determines whether a header collection parameter matches existing runtime semantics.</summary>
    /// <param name="type">The parameter type.</param>
    /// <returns><see langword="true"/> when the type is supported.</returns>
    private static bool IsSupportedHeaderCollectionType(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        == "global::System.Collections.Generic.IDictionary<string, string>";
}
