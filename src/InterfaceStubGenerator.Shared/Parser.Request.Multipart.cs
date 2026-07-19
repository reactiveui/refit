// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>
/// Multipart part classification for generated request construction. Mirrors the reflection request builder's
/// <c>AddMultiPart</c>/<c>AddMultipartItem</c> dispatch and its part name/file-name selection, resolved statically from
/// each parameter's declared type. A part whose declared type is not statically dispatchable (an <c>object</c>, an
/// interface, an open type parameter, or a type that would fall through to the content serializer) makes the whole
/// method fall back to the reflection request builder, as does an opt-in <c>[FormObject]</c> parameter whose properties
/// the reflection builder flattens into individual form-data parts.
/// </content>
internal static partial class Parser
{
    /// <summary>The metadata name of the obsolete <c>Refit.AttachmentNameAttribute</c>.</summary>
    private const string AttachmentNameAttributeDisplayName = "AttachmentNameAttribute";

    /// <summary>The metadata name of <c>Refit.FormObjectAttribute</c>.</summary>
    private const string FormObjectAttributeDisplayName = "FormObjectAttribute";

    /// <summary>The default multipart boundary, matching <c>new MultipartAttribute().BoundaryText</c>.</summary>
    private const string DefaultMultipartBoundary = "----MyGreatBoundary";

    /// <summary>Resolves the multipart boundary and whether a method is multipart in one pass.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <param name="isMultipart">Receives whether the method carries <c>[Multipart]</c>.</param>
    /// <returns>The boundary text for a multipart method, or an empty string otherwise.</returns>
    internal static string ResolveMultipartBoundary(IMethodSymbol methodSymbol, out bool isMultipart)
    {
        var multipartAttribute = FindMultipartAttribute(methodSymbol);
        isMultipart = multipartAttribute is not null;
        return isMultipart ? GetMultipartBoundaryText(multipartAttribute!) : string.Empty;
    }

    /// <summary>Finds the <c>[Multipart]</c> attribute on a method, if present.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <returns>The multipart attribute data, or <see langword="null"/> when the method is not multipart.</returns>
    internal static AttributeData? FindMultipartAttribute(IMethodSymbol methodSymbol)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (IsRefitAttribute(attribute.AttributeClass, MultipartAttributeDisplayName))
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>Resolves the multipart boundary text for a method, matching the reflection builder's selection.</summary>
    /// <param name="multipartAttribute">The method's multipart attribute.</param>
    /// <returns>The boundary text: the <c>[Multipart(boundary)]</c> argument, or the attribute default.</returns>
    internal static string GetMultipartBoundaryText(AttributeData multipartAttribute) =>
        !multipartAttribute.ConstructorArguments.IsEmpty
        && multipartAttribute.ConstructorArguments[0].Value is string boundary
            ? boundary
            : DefaultMultipartBoundary;

    /// <summary>Determines whether any parameter is an explicit request body binding.</summary>
    /// <param name="parameters">The parsed request parameter models.</param>
    /// <returns><see langword="true"/> when a parameter carries <c>[Body]</c>.</returns>
    internal static bool HasBodyParameter(ImmutableEquatableArray<RequestParameterModel> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Kind == RequestParameterKind.Body)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Classifies one parameter of a multipart method into a form part, a query binding, or a fallback.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ClassifyMultipartParameter(
        IParameterSymbol parameter,
        string parameterType,
        in LooseParameterContext context)
    {
        // A parameter carrying [Query] still feeds the query string in a multipart method, matching the reflection
        // builder, which routes any [Query] parameter to the query map rather than to the multipart content.
        if (HasParameterAttribute(parameter, QueryAttributeDisplayName))
        {
            return TryBuildQueryModel(parameter, context.UrlName, context.FormattableSymbol, context.Generation, out var query)
                ? new(QueryRequestParameter(parameter, parameterType, query!, context.Generation), true, 0, 0, 0)
                : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
        }

        return TryBuildMultipartPart(parameter, context) is { } part
            ? new(MultipartRequestParameter(parameter, parameterType, part, context.Generation), true, 0, 0, 0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
    }

    /// <summary>Builds the multipart part descriptor for a parameter whose declared type is statically dispatchable.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The part descriptor, or <see langword="null"/> when the type is not statically dispatchable.</returns>
    internal static MultipartPartModel? TryBuildMultipartPart(IParameterSymbol parameter, in LooseParameterContext context)
    {
        // An opt-in [FormObject] parameter is flattened per-property by the reflection request builder; returning null
        // routes the whole method to that one authoritative implementation instead of duplicating the flattening here.
        if (HasParameterAttribute(parameter, FormObjectAttributeDisplayName))
        {
            return null;
        }

        if (ClassifyPartType(parameter.Type) is not { } classified)
        {
            return null;
        }

        // The field name (reflection's parameterName) is the aliased-or-declared name. The file name (reflection's
        // fileName/itemName) is the obsolete [AttachmentName] override when present, otherwise the same field name.
        var attachmentName = FindParameterAttribute(parameter, AttachmentNameAttributeDisplayName) is { } attribute
            ? GetFirstStringArgument(attribute)
            : null;
        return new(classified.Kind, context.UrlName, attachmentName ?? context.UrlName, classified.IsEnumerable);
    }

    /// <summary>Builds a multipart part request parameter model.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="part">The multipart part descriptor.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The multipart part request parameter model.</returns>
    internal static RequestParameterModel MultipartRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        MultipartPartModel part,
        in InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.MultipartPart,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None)
        {
            MultipartPart = part,
        };

    /// <summary>Classifies a declared parameter type into a multipart dispatch arm and enumerable expansion flag.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <returns>The part kind and whether it expands element-by-element, or <see langword="null"/> when the type is not
    /// statically dispatchable and the method must fall back to the reflection request builder.</returns>
    internal static (MultipartPartKind Kind, bool IsEnumerable)? ClassifyPartType(ITypeSymbol type)
    {
        // string and byte[] are single parts even though they are enumerable: the reflection builder's
        // IEnumerable<object> check excludes them because char and byte are value types.
        if (type.SpecialType == SpecialType.System_String)
        {
            return (MultipartPartKind.String, false);
        }

        if (IsByteArray(type))
        {
            return (MultipartPartKind.ByteArray, false);
        }

        // A reference-typed enumerable is IEnumerable<object> at runtime, so the reflection builder adds one part per
        // element; the element type drives the part kind.
        if (GetReferenceEnumerableElement(type) is { } elementType)
        {
            return ClassifySingle(elementType) is { } elementKind ? (elementKind, true) : null;
        }

        return ClassifySingle(type) is { } kind ? (kind, false) : null;
    }

    /// <summary>Classifies a single (non-enumerable) value type into its multipart dispatch arm.</summary>
    /// <param name="type">The value's declared type.</param>
    /// <returns>The part kind, or <see langword="null"/> when the type is not statically dispatchable.</returns>
    internal static MultipartPartKind? ClassifySingle(ITypeSymbol type)
    {
        // A Guid?/DateTime? part classifies by its underlying formattable type; the null value itself is guarded
        // out at emission, matching the reflection builder's "skip null parameter" behavior.
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var underlying] })
        {
            type = underlying;
        }

        if (IsHttpContentType(type))
        {
            return MultipartPartKind.HttpContent;
        }

        if (IsMultipartItemType(type))
        {
            return MultipartPartKind.MultipartItem;
        }

        if (IsStreamType(type))
        {
            return MultipartPartKind.Stream;
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return MultipartPartKind.String;
        }

        if (IsFileInfoType(type))
        {
            return MultipartPartKind.FileInfo;
        }

        if (IsByteArray(type))
        {
            return MultipartPartKind.ByteArray;
        }

        if (IsMultipartFormattableType(type))
        {
            return MultipartPartKind.Formattable;
        }

        // A sealed or value type (bool, enum, sealed DTO) is written through the content serializer, exactly as the
        // reflection builder's serializer fallback does; its declared type is the runtime type, so the serialized form
        // matches. An open, interface, or object-typed part stays on the reflection path (runtime type decides).
        return IsConcreteComplexType(type) ? MultipartPartKind.Serialized : null;
    }

    /// <summary>Gets the reference-typed element of an enumerable parameter, or null when it is not one.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <returns>The element type when the parameter is a reference-typed enumerable (so its runtime value implements
    /// <c>IEnumerable&lt;object&gt;</c>), otherwise <see langword="null"/>.</returns>
    internal static ITypeSymbol? GetReferenceEnumerableElement(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol { Rank: 1 } array)
        {
            return array.ElementType.IsReferenceType ? array.ElementType : null;
        }

        if (type is INamedTypeSymbol
            {
                OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T,
                TypeArguments: [var self]
            })
        {
            return self.IsReferenceType ? self : null;
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (implemented is
                {
                    OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T,
                    TypeArguments: [var argument]
                }
                && argument.IsReferenceType)
            {
                return argument;
            }
        }

        return null;
    }

    /// <summary>Determines whether a type is a single-rank <see cref="byte"/> array.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is <c>byte[]</c>.</returns>
    internal static bool IsByteArray(ITypeSymbol type) =>
        type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte };

    /// <summary>Determines whether a type is <see cref="System.IO.FileInfo"/>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is <see cref="System.IO.FileInfo"/>.</returns>
    internal static bool IsFileInfoType(ITypeSymbol type) =>
        type is
        {
            Name: "FileInfo",
            ContainingNamespace.Name: "IO",
            ContainingNamespace.ContainingNamespace.Name: SystemNamespace,
            ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
        };

    /// <summary>Determines whether a type is <see cref="System.IO.Stream"/> or derives from it.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is assignable to <see cref="System.IO.Stream"/>.</returns>
    internal static bool IsStreamType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is
                {
                    Name: "Stream",
                    ContainingNamespace.Name: "IO",
                    ContainingNamespace.ContainingNamespace.Name: SystemNamespace,
                    ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
                })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a type is <see cref="System.Net.Http.HttpContent"/> or derives from it.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is assignable to <see cref="System.Net.Http.HttpContent"/>.</returns>
    internal static bool IsHttpContentType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is
                {
                    Name: "HttpContent",
                    ContainingNamespace.Name: "Http",
                    ContainingNamespace.ContainingNamespace.Name: "Net",
                    ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: SystemNamespace,
                    ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
                })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a type is <c>Refit.MultipartItem</c> or derives from it.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is assignable to <c>Refit.MultipartItem</c>.</returns>
    internal static bool IsMultipartItemType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is
                {
                    Name: "MultipartItem",
                    ContainingNamespace.Name: "Refit",
                    ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true
                })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a type is a date/time or <see cref="System.Guid"/> multipart value.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> for the BCL value types the reflection builder renders through the form
    /// URL-encoded formatter instead of the content serializer.</returns>
    internal static bool IsMultipartFormattableType(ITypeSymbol type) =>
        type is { ContainingNamespace.Name: SystemNamespace, ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true }
        && type.Name is "Guid" or "DateTime" or "DateTimeOffset" or "TimeSpan" or "DateOnly" or "TimeOnly";
}
