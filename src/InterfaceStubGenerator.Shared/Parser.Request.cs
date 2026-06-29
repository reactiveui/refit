// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Request parsing helpers for the Refit source generator.</content>
internal static partial class Parser
{
#if NET7_0_OR_GREATER
    /// <summary>Gets the compiled regular expression that matches URL path parameters.</summary>
    /// <returns>The parameter matching regular expression.</returns>
    [GeneratedRegex("{(([^/?\\r\\n])*?)}")]
    private static partial Regex ParameterRegex();
#else
    /// <summary>The compiled regular expression that matches URL path parameters.</summary>
    private static readonly Regex _parameterRegexValue = new(
        "{(([^/?\\r\\n])*?)}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>Gets the compiled regular expression that matches URL path parameters.</summary>
    /// <returns>The parameter matching regular expression.</returns>
    private static Regex ParameterRegex() => _parameterRegexValue;
#endif
    
    /// <summary>Parses the request metadata needed by generated request construction.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="returnTypeInfo">The classified return type shape.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The parsed request metadata.</returns>
    private static RequestModel ParseRequest(
        IMethodSymbol methodSymbol,
        ReturnTypeInfo returnTypeInfo,
        in InterfaceGenerationContext context)
    {
        if (!context.GeneratedRequestBuilding)
        {
            return RequestModel.Empty;
        }
        
        var httpMethodAttribute = FindHttpMethodAttribute(
            methodSymbol,
            context.HttpMethodBaseAttributeSymbol)!;

        var httpMethod = GetHttpMethodName(httpMethodAttribute.AttributeClass);
        var path = GetHttpPath(httpMethodAttribute);
        var returnTypes = GetRequestReturnTypes(methodSymbol);
        var (routeParameterMap, fragmentPath) = ParseParameterRouteMap(path, methodSymbol, methodSymbol.Parameters);
        var parameters = ParseRequestParameters(methodSymbol.Parameters, routeParameterMap, out var parameterEligibility);
        var staticHeaders = ParseStaticHeaders(methodSymbol);

        var canGenerateInline =
            parameterEligibility
            && returnTypeInfo is ReturnTypeInfo.AsyncVoid or ReturnTypeInfo.AsyncResult or ReturnTypeInfo.AsyncEnumerable
            && methodSymbol.TypeParameters.Length == 0
            && httpMethod.Length > 0
            && IsConstantPathSupported(path)
            && IsSupportedInlineBody(parameters)
            && !HasUnsupportedInlineRequestMetadata(methodSymbol);

        return new(
            httpMethod,
            NormalizeConstantPathForInline(path),
            returnTypes.ResultType,
            returnTypes.DeserializedResultType,
            returnTypes.IsApiResponse,
            returnTypes.DisposeResponse,
            canGenerateInline,
            staticHeaders,
            parameters,
            fragmentPath);
    }

    /// <summary>Gets the HTTP method name represented by a Refit method attribute.</summary>
    /// <param name="attributeClass">The attribute type.</param>
    /// <returns>The HTTP method name, or an empty string for unsupported custom attributes.</returns>
    [ExcludeFromCodeCoverage]
    private static string GetHttpMethodName(INamedTypeSymbol? attributeClass) =>
        attributeClass?.MetadataName switch
        {
            "DeleteAttribute" => "DELETE",
            "GetAttribute" => "GET",
            "HeadAttribute" => "HEAD",
            "OptionsAttribute" => "OPTIONS",
            "PatchAttribute" => "PATCH",
            "PostAttribute" => "POST",
            "PutAttribute" => "PUT",
            _ => string.Empty
        };

    /// <summary>Gets the literal path from a Refit HTTP method attribute.</summary>
    /// <param name="attributeData">The attribute data.</param>
    /// <returns>The path literal.</returns>
    private static string GetHttpPath(AttributeData attributeData)
    {
        var arguments = attributeData.ConstructorArguments;
        return arguments.Length > 0 && arguments[0].Value is string path
            ? path
            : string.Empty;
    }

    /// <summary>Appends a query segment unless its key is empty or whitespace.</summary>
    /// <param name="path">The full path containing the query segment.</param>
    /// <param name="queryStart">The query start index, used to size the lazy buffer.</param>
    /// <param name="partStart">The query segment start index.</param>
    /// <param name="partLength">The query segment length.</param>
    /// <param name="queryBuffer">The query buffer, allocated only when a segment is retained.</param>
    /// <param name="queryLength">The number of characters currently written to the query buffer.</param>
    private static void AppendNonEmptyQueryPart(
        string path,
        int queryStart,
        int partStart,
        int partLength,
        ref char[]? queryBuffer,
        ref int queryLength)
    {
        if (partLength <= 0)
        {
            return;
        }

        var equalsIndex = path.IndexOf('=', partStart, partLength);
        var keyLength = equalsIndex >= 0
            ? equalsIndex - partStart
            : partLength;
        if (IsWhiteSpace(path, partStart, keyLength))
        {
            return;
        }

        queryBuffer ??= new char[path.Length - queryStart];
        if (queryLength > 0)
        {
            queryBuffer[queryLength++] = '&';
        }

        path.CopyTo(partStart, queryBuffer, queryLength, partLength);
        queryLength += partLength;
    }

    /// <summary>Determines whether a method carries request metadata the initial inline emitter does not handle.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <returns><see langword="true"/> when request construction must use the runtime builder.</returns>
    private static bool HasUnsupportedInlineRequestMetadata(IMethodSymbol methodSymbol) =>
        HasUnsupportedMethodAttribute(methodSymbol.GetAttributes());

    /// <summary>Determines whether method attributes contain request metadata unsupported by the initial inline emitter.</summary>
    /// <param name="attributes">The attributes to inspect.</param>
    /// <returns><see langword="true"/> when an attribute name matches.</returns>
    private static bool HasUnsupportedMethodAttribute(in ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            var displayName = attribute.AttributeClass?.ToDisplayString();
            if (displayName is
                "Refit.MultipartAttribute" or
                "Refit.QueryUriFormatAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Parses the static headers declared on inherited interfaces, the declaring interface, and the method.</summary>
    /// <param name="methodSymbol">The method whose header metadata should be parsed.</param>
    /// <returns>The final static header set.</returns>
    private static ImmutableEquatableArray<HeaderModel> ParseStaticHeaders(IMethodSymbol methodSymbol)
    {
        var headers = new List<HeaderModel>();

        var inheritedInterfaces = methodSymbol.ContainingType.AllInterfaces;
        for (var i = inheritedInterfaces.Length - 1; i >= 0; i--)
        {
            AddStaticHeaders(headers, inheritedInterfaces[i].GetAttributes());
        }

        AddStaticHeaders(headers, methodSymbol.ContainingType.GetAttributes());
        AddStaticHeaders(headers, methodSymbol.GetAttributes());

        return headers.ToImmutableEquatableArray();
    }

    /// <summary>Adds headers from a collection of attributes, replacing earlier values for the same header name.</summary>
    /// <param name="headers">The mutable header list.</param>
    /// <param name="attributes">The attributes to inspect.</param>
    private static void AddStaticHeaders(
        List<HeaderModel> headers,
        in ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Refit.HeadersAttribute")
            {
                continue;
            }

            AddHeadersAttributeValues(headers, attribute);
        }
    }

    /// <summary>Adds the string values from a <c>HeadersAttribute</c> to the static header list.</summary>
    /// <param name="headers">The mutable header list.</param>
    /// <param name="attribute">The attribute data.</param>
    private static void AddHeadersAttributeValues(List<HeaderModel> headers, AttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Kind == TypedConstantKind.Array)
            {
                foreach (var value in argument.Values)
                {
                    if (value.Value is string header)
                    {
                        AddStaticHeader(headers, header);
                    }
                }
            }

            // HeadersAttribute is declared as params string[], so Roslyn always exposes
            // values as an array typed constant for supported Refit metadata.
        }
    }

     private static (HashSet<IParameterSymbol> Map, ImmutableEquatableArray<RouteFragmentModel> Fragments) ParseParameterRouteMap(
         string relativePath,
         IMethodSymbol method,
         ImmutableArray<IParameterSymbol> parameters)
    {
        var ret = new HashSet<IParameterSymbol>(SymbolEqualityComparer.Default);
            
        // might have to be relative path, not sure what the difference is 
        var parameterizedParts = ParameterRegex().Matches(relativePath);

        if (parameterizedParts.Count == 0)
        {
            return string.IsNullOrEmpty(relativePath)
                ? (ret, ImmutableEquatableArray<RouteFragmentModel>.Empty)
                : (ret, ImmutableEquatableArrayFactory.FromArray<RouteFragmentModel>([new RouteFragmentModel.Constant(relativePath)]));
        }
        
        var fragmentList = new List<RouteFragmentModel>();

        var paramValidationDict = BuildParamValidationDict(method.Parameters);
        var objectParamValidationDict = BuildObjectParamValidationDict(method.Parameters);

        var index = 0;

        for (var i = 0; i < parameterizedParts.Count; i++)
        {
            var match = parameterizedParts[i];

            // Add constant value from given http path
            if (match.Index != index)
            {
                fragmentList.Add(new RouteFragmentModel.Constant(relativePath.Substring(index, match.Index - index)));
            }

            index = match.Index + match.Length;

            AddFragmentForMatch(
                relativePath,
                method.Parameters,
                ret,
                fragmentList,
                paramValidationDict,
                objectParamValidationDict,
                match);
        }

        if (index >= relativePath.Length)
        {
            return (ret, ImmutableEquatableArrayFactory.FromList(fragmentList));
        }

        // Add trailing string.
        var trailingConstant = relativePath[index..];
        fragmentList.Add(new RouteFragmentModel.Constant(trailingConstant));

        return (ret, ImmutableEquatableArrayFactory.FromList(fragmentList));
    }

    /// <summary>Builds a lookup of lower-cased URL parameter names to their declaring method parameter.</summary>
    /// <param name="parameters">The array of method parameters.</param>
    /// <returns>A map of URL parameter names to method parameters.</returns>
    private static Dictionary<string, IParameterSymbol> BuildParamValidationDict(ImmutableArray<IParameterSymbol> parameters)
    {
        var paramValidationDict = new Dictionary<string, IParameterSymbol>(parameters.Length);
        for (var i = 0; i < parameters.Length; i++)
        {
            paramValidationDict[GetUrlNameForSymbol(parameters[i]).ToLowerInvariant()] = parameters[i];
        }

        return paramValidationDict;
    }
    
    
    /// <summary>Builds the constraint models for a set of type parameters.</summary>
    /// <param name="parameters">The parameters to parse.</param>
    /// <returns>The constraint models for the type parameters.</returns>
    private static Dictionary<string, (IParameterSymbol, IPropertySymbol)> BuildObjectParamValidationDict(
        in ImmutableArray<IParameterSymbol> parameters
        )
    {
        if (parameters.Length == 0)
        {
            return new();
        }

        var objectParamValidationDict = new Dictionary<string, (IParameterSymbol, IPropertySymbol)>();
        foreach (var parameter in parameters)
        {
            foreach (var property in GetPublicProperties(parameter.Type))
            {
                var key = $"{parameter.Name}.{GetUrlNameForSymbol(property)}".ToLowerInvariant();

                if (!objectParamValidationDict.ContainsKey(key))
                {
                    // some of these fields are redundant key can be constructed when dictionary is created (might not account for @ symbols)
                    // subProperties.Add(new(key, $"{parameter.Name}.{property.Name}", parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), property.Name));
                    objectParamValidationDict.Add(key, (parameter, property));
                }
            }
        }

        return objectParamValidationDict;
    }

    /// <summary>Gets public properties from.</summary>
    /// <param name="typeSymbol">The parameter to parse.</param>
    /// <returns>The parsed parameter models.</returns>
    private static IEnumerable<IPropertySymbol> GetPublicProperties(
        ITypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeKind != TypeKind.Class)
        {
            yield break;
        }

        var currentType = typeSymbol;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            var publicReadableProps = currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => 
                    // Property itself must be public
                    p.DeclaredAccessibility == Accessibility.Public && 
                    // Property must have a getter
                    p.GetMethod != null && 
                    // The getter itself must be public (not private/protected)
                    p.GetMethod.DeclaredAccessibility == Accessibility.Public);

            foreach (var properties in publicReadableProps)
            {
                yield return properties;
            }

            currentType = currentType.BaseType;
        }
    }

    /// <summary>Resolves a single parameterized URL fragment against the parameter maps and appends the result.</summary>
    /// <param name="relativePath">The relative URL path template.</param>
    /// <param name="parameters">The generated settings local name.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="param">The lookups of directly matched parameter names.</param>
    /// <param name="objectProperty">The lookups of nested object-property names.</param>
    /// <param name="match">The parameterized URL match being resolved.</param>
    private static void AddFragmentForMatch(
        string relativePath,
        ImmutableArray<IParameterSymbol> parameters,
        HashSet<IParameterSymbol> ret,
        List<RouteFragmentModel> fragmentList,
        Dictionary<string, IParameterSymbol> param,
        Dictionary<string, (IParameterSymbol, IPropertySymbol)>objectProperty,
        Match match)
    {
        var rawName = match.Groups[1].Value.ToLowerInvariant();
        var isRoundTripping = rawName.StartsWith("**", StringComparison.Ordinal);
        var name = isRoundTripping ? rawName[2..] : rawName;

        if (param.TryGetValue(name, out var value))
        {
            AddStandardParameter(
                ret,
                fragmentList,
                isRoundTripping,
                value);
        }
        else if (objectProperty.TryGetValue(name, out var value1) && !isRoundTripping)
        {
            AddObjectPropertyParameter(
                value1.Item1,
                ret,
                fragmentList,
                value1.Item2);
        }
        else
        {
            fragmentList.Add(new RouteFragmentModel.UnmatchedRouteGuard(rawName));
            fragmentList.Add(new RouteFragmentModel.Constant(match.Value));
        }
    }

    /// <summary>Adds a standard (directly matched) route parameter to the parameter map and fragment list.</summary>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="isRoundTripping">The parsed parameter name details from the URL template.</param>
    /// <param name="value">The matched method parameter.</param>
    private static void AddStandardParameter(
        HashSet<IParameterSymbol> ret,
        List<RouteFragmentModel> fragmentList,
        bool isRoundTripping,
        IParameterSymbol value)
    {
        var paramType = value.Type;
        if (isRoundTripping && paramType.SpecialType != SpecialType.System_String)
        {
            fragmentList.Add(new RouteFragmentModel.RoundTripNotStringError(value.MetadataName, paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            return;
        }

        ret.Add(value);
        fragmentList.Add(new RouteFragmentModel.StandardParameter(value.MetadataName, isRoundTripping));
    }
    
    /// <summary>Adds an object-property route parameter to the parameter map and fragment list.</summary>
    /// <param name="parameter">The corresponding method parameter.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="property">The matched property symbol.</param>
    private static void AddObjectPropertyParameter(
        IParameterSymbol parameter,
        HashSet<IParameterSymbol> ret,
        List<RouteFragmentModel> fragmentList,
        IPropertySymbol property)
    {
        ret.Add(parameter);
        // perhaps this should be parameter metadata name
        fragmentList.Add(new RouteFragmentModel.ObjectAccess($"{parameter.Name}.{property.Name}", parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), property.Name));
    }

    /// <summary>Gets the URL name to use for a symbol, honoring any alias attribute.</summary>
    /// <param name="symbol">The symbol whose URL name is resolved.</param>
    /// <returns>The aliased or declared parameter name.</returns>
    private static string GetUrlNameForSymbol(ISymbol symbol) => GetMemberAlias(symbol) ?? symbol.MetadataName;

    /// <summary>Parses request parameter bindings for the conservative initial inline path.</summary>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="routeParameterMap">Set of parameters that are use in the route.</param>
    /// <param name="canGenerateInline">Receives whether every parameter is supported.</param>
    /// <returns>The parsed request parameter models.</returns>
    private static ImmutableEquatableArray<RequestParameterModel> ParseRequestParameters(
        in ImmutableArray<IParameterSymbol> parameters,
        HashSet<IParameterSymbol> routeParameterMap,
        out bool canGenerateInline)
    {
        if (parameters.Length == 0)
        {
            canGenerateInline = true;
            return ImmutableEquatableArray<RequestParameterModel>.Empty;
        }

        var requestParameters = new RequestParameterModel[parameters.Length];
        var bodyCount = 0;
        var cancellationTokenCount = 0;
        var headerCollectionCount = 0;
        canGenerateInline = true;

        for (var i = 0; i < parameters.Length; i++)
        {
            var parsedParameter = ParseRequestParameter(parameters[i], routeParameterMap);
            requestParameters[i] = parsedParameter.Parameter;
            bodyCount += parsedParameter.BodyCount;
            cancellationTokenCount += parsedParameter.CancellationTokenCount;
            headerCollectionCount += parsedParameter.HeaderCollectionCount;
            canGenerateInline &= parsedParameter.CanGenerateInline;
        }

        if (bodyCount > 1 || cancellationTokenCount > 1 || headerCollectionCount > 1)
        {
            canGenerateInline = false;
        }

        return ImmutableEquatableArrayFactory.FromArray(requestParameters);
    }

    /// <summary>Parses one request parameter binding.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="routeParameterMap">Set of parameters that are use in the route.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ParseRequestParameter(IParameterSymbol parameter,
        HashSet<IParameterSymbol> routeParameterMap)
    {
        var parameterType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var canBeNull = CanBeNull(parameter.Type, parameter.NullableAnnotation);
        if (IsCancellationToken(parameter.Type))
        {
            return new(
                new(
                    parameter.MetadataName,
                    parameterType,
                    RequestParameterKind.CancellationToken,
                    canBeNull,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    BodyBufferMode.None),
                true,
                0,
                1,
                0);
        }

        if (TryParseBodyParameter(parameter, parameterType, out var bodyParameter))
        {
            return new(bodyParameter, true, 1, 0, 0);
        }

        if (TryParseHeaderParameter(parameter, parameterType, out var headerParameter))
        {
            return new(headerParameter, true, 0, 0, 0);
        }

        if (TryParseHeaderCollectionParameter(parameter, parameterType, out var headerCollectionParameter))
        {
            return new(
                headerCollectionParameter,
                headerCollectionParameter.Kind == RequestParameterKind.HeaderCollection,
                0,
                0,
                1);
        }

        if (TryParsePropertyParameter(parameter, parameterType, out var propertyParameter))
        {
            return new(propertyParameter, true, 0, 0, 0);
        }

        if (routeParameterMap.Contains(parameter))
        {
            return new(propertyParameter, true, 0, 0, 0);
        }
        else
        {
            return new(UnsupportedRequestParameter(parameter, parameterType), false, 0, 0, 0);
        }
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

    /// <summary>Tries to parse an explicitly attributed body parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="bodyParameter">Receives the body parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a body attribute.</returns>
    private static bool TryParseBodyParameter(
        IParameterSymbol parameter,
        string parameterType,
        out RequestParameterModel bodyParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Refit.BodyAttribute")
            {
                continue;
            }

            var bodyInfo = ParseBodyAttribute(attribute);
            var formFields = bodyInfo.SerializationMethod == "UrlEncoded"
                ? TryBuildFormFields(parameter.Type)
                : null;
            bodyParameter = new(
                    parameter.MetadataName,
                    parameterType,
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

        bodyParameter = new(
            parameter.MetadataName,
            parameterType,
            RequestParameterKind.Unsupported,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None);
        return false;
    }

    /// <summary>Builds reflection-free form field descriptors for a URL-encoded body type.</summary>
    /// <param name="bodyType">The declared body type.</param>
    /// <returns>The field descriptors, or <see langword="null"/> when the type is not eligible for the descriptor path.</returns>
    private static ImmutableEquatableArray<FormFieldModel>? TryBuildFormFields(ITypeSymbol bodyType)
    {
        if (!IsFormFieldEligibleType(bodyType))
        {
            return null;
        }

        var fields = new List<FormFieldModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = bodyType;
            current is not null && current.SpecialType != SpecialType.System_Object;
            current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol property && IsReadableFormProperty(property) && seen.Add(property.Name))
                {
                    fields.Add(BuildFormFieldModel(property));
                }
            }
        }

        return ImmutableEquatableArrayFactory.FromList(fields);
    }

    /// <summary>Determines whether a property contributes a readable public instance form field.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property is read through a public instance getter.</returns>
    private static bool IsReadableFormProperty(IPropertySymbol property) =>
        !property.IsStatic
        && !property.IsIndexer
        && property.DeclaredAccessibility == Accessibility.Public
        && property.GetMethod is { DeclaredAccessibility: Accessibility.Public };

    /// <summary>Determines whether a body type can be flattened to form fields without reflection.</summary>
    /// <param name="type">The declared body type.</param>
    /// <returns><see langword="true"/> when descriptor-based flattening matches the reflection path.</returns>
    private static bool IsFormFieldEligibleType(ITypeSymbol type) =>
        type.TypeKind is not (TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Dynamic
            or TypeKind.Array or TypeKind.Pointer or TypeKind.Error)
        && type.SpecialType is not (SpecialType.System_String or SpecialType.System_Object)
        && type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }

        // Dictionaries and other enumerables flow through the reflection path, which special-cases them.
        && !ImplementsEnumerable(type);

    /// <summary>Determines whether a type implements the non-generic <see cref="System.Collections.IEnumerable"/>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is enumerable.</returns>
    private static bool ImplementsEnumerable(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Collections_IEnumerable)
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the form field descriptor for one body property.</summary>
    /// <param name="property">The property to describe.</param>
    /// <returns>The field descriptor.</returns>
    private static FormFieldModel BuildFormFieldModel(IPropertySymbol property)
    {
        string? aliasName = null;
        string? jsonName = null;
        var query = default(QueryFormData);

        foreach (var attribute in property.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (attributeName == "Refit.AliasAsAttribute")
            {
                aliasName = GetFirstStringArgument(attribute);
            }
            else if (attributeName == "System.Text.Json.Serialization.JsonPropertyNameAttribute")
            {
                jsonName = GetFirstStringArgument(attribute);
            }
            else if (attributeName == "Refit.QueryAttribute")
            {
                query = ParseFormQueryAttribute(attribute);
            }
        }

        var prefixSegment = string.IsNullOrWhiteSpace(query.Prefix) ? null : query.Prefix + query.Delimiter;
        return new(
            property.Name,
            aliasName ?? jsonName,
            prefixSegment,
            query.Format,
            query.CollectionFormatValue,
            query.SerializeNull);
    }

    /// <summary>Reads the first string constructor argument from an attribute.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <returns>The first string argument, or <see langword="null"/>.</returns>
    private static string? GetFirstStringArgument(AttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Value is string value)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Parses the form-relevant members of a <c>[Query]</c> attribute applied to a property.</summary>
    /// <param name="attribute">The query attribute data.</param>
    /// <returns>The parsed form query data.</returns>
    private static QueryFormData ParseFormQueryAttribute(AttributeData attribute) =>
        ApplyQueryNamedArguments(attribute, ParseQueryConstructorArguments(attribute));

    /// <summary>Parses the constructor arguments of a <c>[Query]</c> attribute.</summary>
    /// <param name="attribute">The query attribute data.</param>
    /// <returns>The form query data carried by the constructor arguments.</returns>
    private static QueryFormData ParseQueryConstructorArguments(AttributeData attribute)
    {
        var delimiter = ".";
        string? prefix = null;
        string? format = null;
        int? collectionFormatValue = null;
        var stringArguments = 0;

        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Type?.ToDisplayString() == "Refit.CollectionFormat" && argument.Value is int constructorCollectionFormat)
            {
                collectionFormatValue = constructorCollectionFormat;
            }
            else if (argument.Value is string stringValue)
            {
                if (stringArguments == 0)
                {
                    delimiter = stringValue;
                }
                else if (stringArguments == 1)
                {
                    prefix = stringValue;
                }
                else
                {
                    format = stringValue;
                }

                stringArguments++;
            }
        }

        return new(delimiter, prefix, format, collectionFormatValue, false);
    }

    /// <summary>Applies the named arguments of a <c>[Query]</c> attribute over constructor-supplied data.</summary>
    /// <param name="attribute">The query attribute data.</param>
    /// <param name="data">The data parsed from constructor arguments.</param>
    /// <returns>The form query data with named arguments applied.</returns>
    private static QueryFormData ApplyQueryNamedArguments(AttributeData attribute, QueryFormData data)
    {
        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key == "Format" && named.Value.Value is string formatValue)
            {
                data = data with { Format = formatValue };
            }
            else if (named.Key == "CollectionFormat" && named.Value.Value is int namedCollectionFormat)
            {
                data = data with { CollectionFormatValue = namedCollectionFormat };
            }
            else if (named.Key == "SerializeNull" && named.Value.Value is bool serializeNullValue)
            {
                data = data with { SerializeNull = serializeNullValue };
            }
        }

        return data;
    }

    /// <summary>Tries to parse a dynamic header parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="headerParameter">Receives the header parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a supported header attribute.</returns>
    private static bool TryParseHeaderParameter(
        IParameterSymbol parameter,
        string parameterType,
        out RequestParameterModel headerParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Refit.HeaderAttribute")
            {
                continue;
            }

            var arguments = attribute.ConstructorArguments;
            if (arguments.Length == 0 || arguments[0].Value is not string headerName ||
                string.IsNullOrWhiteSpace(headerName))
            {
                continue;
            }

            headerParameter = new(
                parameter.MetadataName,
                parameterType,
                RequestParameterKind.Header,
                CanBeNull(parameter.Type, parameter.NullableAnnotation),
                headerName.Trim(),
                string.Empty,
                string.Empty,
                BodyBufferMode.None);
            return true;
        }

        headerParameter = UnsupportedRequestParameter(parameter, parameterType);
        return false;
    }

    /// <summary>Tries to parse a dynamic header collection parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="headerCollectionParameter">Receives the header collection parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a supported header collection attribute.</returns>
    private static bool TryParseHeaderCollectionParameter(
        IParameterSymbol parameter,
        string parameterType,
        out RequestParameterModel headerCollectionParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Refit.HeaderCollectionAttribute")
            {
                continue;
            }

            if (IsSupportedHeaderCollectionType(parameter.Type))
            {
                headerCollectionParameter = new(
                    parameter.MetadataName,
                    parameterType,
                    RequestParameterKind.HeaderCollection,
                    CanBeNull(parameter.Type, parameter.NullableAnnotation),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    BodyBufferMode.None);
                return true;
            }

            headerCollectionParameter = UnsupportedRequestParameter(parameter, parameterType);
            return false;
        }

        headerCollectionParameter = UnsupportedRequestParameter(parameter, parameterType);
        return false;
    }

    /// <summary>Tries to parse a request property parameter.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="propertyParameter">Receives the property parameter model.</param>
    /// <returns><see langword="true"/> when the parameter has a property attribute.</returns>
    private static bool TryParsePropertyParameter(
        IParameterSymbol parameter,
        string parameterType,
        out RequestParameterModel propertyParameter)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Refit.PropertyAttribute")
            {
                continue;
            }

            var arguments = attribute.ConstructorArguments;
            var propertyKey = arguments.Length > 0 && arguments[0].Value is string { Length: > 0 } key
                ? key
                : parameter.MetadataName;
            propertyParameter = new(
                parameter.MetadataName,
                parameterType,
                RequestParameterKind.Property,
                CanBeNull(parameter.Type, parameter.NullableAnnotation),
                string.Empty,
                propertyKey,
                string.Empty,
                BodyBufferMode.None);
            return true;
        }

        propertyParameter = UnsupportedRequestParameter(parameter, parameterType);
        return false;
    }

    /// <summary>Builds an unsupported request parameter model.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <returns>The unsupported parameter model.</returns>
    private static RequestParameterModel UnsupportedRequestParameter(
        IParameterSymbol parameter,
        string parameterType) =>
        new(
            parameter.MetadataName,
            parameterType,
            RequestParameterKind.Unsupported,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None);

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

    /// <summary>Parses the constructor-supplied data from a body attribute.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The parsed body serialization and buffering data.</returns>
    private static BodyAttributeInfo ParseBodyAttribute(AttributeData attribute)
    {
        var serializationMethod = "Default";
        bool? buffered = null;

        foreach (var argument in attribute.ConstructorArguments)
        {
            if (TryGetBodySerializationMethodName(argument, out var methodName))
            {
                serializationMethod = methodName;
                continue;
            }

            if (TryGetBodyBufferedValue(argument, out var boolValue))
            {
                buffered = boolValue;
            }
        }

        var bufferMode = buffered switch
        {
            true => BodyBufferMode.Buffered,
            false => BodyBufferMode.Streaming,
            _ => BodyBufferMode.Settings
        };

        return new(serializationMethod, bufferMode);
    }

    /// <summary>Tries to parse a body serialization method constructor argument.</summary>
    /// <param name="argument">The constructor argument.</param>
    /// <param name="methodName">Receives the enum member name.</param>
    /// <returns><see langword="true"/> when the argument is a body serialization method.</returns>
    private static bool TryGetBodySerializationMethodName(in TypedConstant argument, out string methodName)
    {
        if (argument.Type?.ToDisplayString() == "Refit.BodySerializationMethod"
            && argument.Value is int enumValue)
        {
            methodName = GetBodySerializationMethodName(enumValue);
            return true;
        }

        methodName = string.Empty;
        return false;
    }

    /// <summary>Gets return-type details required by the shared generated request runner.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <returns>The parsed return type details.</returns>
    private static RequestReturnTypes GetRequestReturnTypes(IMethodSymbol methodSymbol)
    {
        var resultType = GetReturnResultType(methodSymbol.ReturnType);
        var isApiResponse = IsApiResponseType(resultType);
        var deserializedResultType = GetDeserializedResultTypeName(resultType, isApiResponse);
        var disposeResponse = ShouldDisposeResponse(deserializedResultType);

        return new(
            resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            deserializedResultType,
            isApiResponse,
            disposeResponse);
    }

    /// <summary>Gets the result type wrapped by Task or ValueTask.</summary>
    /// <param name="returnType">The declared return type.</param>
    /// <returns>The result type.</returns>
    private static ITypeSymbol GetReturnResultType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            var ns = namedType.ContainingNamespace.ToDisplayString();
            if (namedType.MetadataName is "Task`1" or "ValueTask`1" && ns == "System.Threading.Tasks")
            {
                return namedType.TypeArguments[0];
            }

            if (namedType.MetadataName == "IAsyncEnumerable`1" && ns == "System.Collections.Generic")
            {
                return namedType.TypeArguments[0];
            }
        }

        return returnType;
    }

    /// <summary>Determines whether a type is one of Refit's API response wrappers.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> for API response wrappers.</returns>
    private static bool IsApiResponseType(ITypeSymbol type) =>
        type is INamedTypeSymbol namedType
        && namedType.ContainingNamespace.ToDisplayString() == "Refit" && namedType.MetadataName is "IApiResponse" or "ApiResponse`1" or "IApiResponse`1";

    /// <summary>Gets the response-content deserialization type for a method result type.</summary>
    /// <param name="resultType">The method result type.</param>
    /// <param name="isApiResponse">Whether the result is an API response wrapper.</param>
    /// <returns>The deserialization target type.</returns>
    private static string GetDeserializedResultTypeName(ITypeSymbol resultType, bool isApiResponse)
    {
        if (!isApiResponse)
        {
            return resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        var namedType = (INamedTypeSymbol)resultType;
        return namedType.MetadataName == "IApiResponse"
            ? "global::System.Net.Http.HttpContent"
            : namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>Parsed body attribute data.</summary>
    /// <param name="SerializationMethod">The body serialization method name.</param>
    /// <param name="BufferMode">The body buffering mode.</param>
    private readonly record struct BodyAttributeInfo(
        string SerializationMethod,
        BodyBufferMode BufferMode);

    /// <summary>Form-relevant data parsed from a <c>[Query]</c> attribute on a body property.</summary>
    /// <param name="Delimiter">The delimiter combined with the prefix.</param>
    /// <param name="Prefix">The field name prefix, if any.</param>
    /// <param name="Format">The value format, if any.</param>
    /// <param name="CollectionFormatValue">The explicit collection format value, if any.</param>
    /// <param name="SerializeNull">Whether null values are serialized as empty fields.</param>
    private readonly record struct QueryFormData(
        string Delimiter,
        string? Prefix,
        string? Format,
        int? CollectionFormatValue,
        bool SerializeNull);

    /// <summary>Parsed return-type data for generated requests.</summary>
    /// <param name="ResultType">The method result type.</param>
    /// <param name="DeserializedResultType">The response-body deserialization type.</param>
    /// <param name="IsApiResponse">Whether the result type is an API response wrapper.</param>
    /// <param name="DisposeResponse">Whether the runner should dispose the response.</param>
    private readonly record struct RequestReturnTypes(
        string ResultType,
        string DeserializedResultType,
        bool IsApiResponse,
        bool DisposeResponse);

    /// <summary>Parsed request parameter data plus duplicate-detection counters.</summary>
    /// <param name="Parameter">The parsed parameter model.</param>
    /// <param name="CanGenerateInline">Whether this parameter can be emitted inline.</param>
    /// <param name="BodyCount">The number of body parameters represented by this parameter.</param>
    /// <param name="CancellationTokenCount">The number of cancellation tokens represented by this parameter.</param>
    /// <param name="HeaderCollectionCount">The number of header collections represented by this parameter.</param>
    private readonly record struct ParsedRequestParameter(
        RequestParameterModel Parameter,
        bool CanGenerateInline,
        int BodyCount,
        int CancellationTokenCount,
        int HeaderCollectionCount);
}
