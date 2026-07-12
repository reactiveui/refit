// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Request parsing helpers for the Refit source generator.</content>
internal static partial class Parser
{
    /// <summary>Parses the request metadata needed by generated request construction.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="returnTypeInfo">The classified return type shape.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The parsed request metadata.</returns>
    private static RequestModel ParseRequest(
        IMethodSymbol methodSymbol,
        ReturnTypeInfo returnTypeInfo,
        InterfaceGenerationContext context)
    {
        if (!context.GeneratedRequestBuilding)
        {
            ReportSourceGenOnlyAttributeMisuse(methodSymbol, context);
            return RequestModel.Empty;
        }

        var httpMethodAttribute = FindHttpMethodAttribute(
            methodSymbol,
            context.HttpMethodBaseAttributeSymbol)!;

        var httpMethod = GetHttpMethodName(httpMethodAttribute.AttributeClass);
        var path = GetHttpPath(httpMethodAttribute);
        var normalizedPath = NormalizeConstantPathForInline(path);
        var pathParameters = ExtractPathParameterPlaceholderNames(normalizedPath);

        // A registered IReturnTypeAdapter surfaces the declared return type; the HTTP call materializes the adapter's
        // wrapped result, so classify the return types against that inner type just like a Task<T>.
        string? adapterTypeExpression = null;
        var resultTypeSource = methodSymbol.ReturnType;
        if (TryMatchReturnTypeAdapter(methodSymbol.ReturnType, context, out var closedAdapter, out var adapterResultType))
        {
            adapterTypeExpression = QualifyType(closedAdapter, context);
            resultTypeSource = adapterResultType;
        }

        var returnTypes = GetRequestReturnTypes(resultTypeSource, context);
        var unsupportedMetadata = HasUnsupportedInlineRequestMetadata(methodSymbol);

        // Only POST/PUT/PATCH carry an implicit body, and multipart methods never do; the multipart case is
        // covered because multipart methods carry unsupported metadata and fall back wholly.
        var allowImplicitBody = !unsupportedMetadata && IsBodyCapableHttpMethod(httpMethod);

        var parameters = ParseRequestParameters(
            methodSymbol.Parameters,
            pathParameters,
            context.FormattableSymbol,
            allowImplicitBody,
            context,
            out var parameterEligibility);
        var staticHeaders = ParseStaticHeaders(methodSymbol);

        // A registered adapter makes an otherwise-unsupported return shape inline-eligible.
        var returnShapeEligible =
            returnTypeInfo is ReturnTypeInfo.AsyncVoid or ReturnTypeInfo.AsyncResult or ReturnTypeInfo.AsyncEnumerable
            || adapterTypeExpression is not null;

        var canGenerateInline = CanGenerateInlineRequest(
            parameterEligibility,
            returnShapeEligible,
            httpMethod,
            new(path, normalizedPath),
            parameters,
            unsupportedMetadata);

        if (!canGenerateInline)
        {
            ReportSourceGenOnlyAttributeMisuse(methodSymbol, context);
        }

        return new(
            httpMethod,
            normalizedPath,
            returnTypes.ResultType,
            returnTypes.DeserializedResultType,
            returnTypes.IsApiResponse,
            returnTypes.DisposeResponse,
            canGenerateInline,
            canGenerateInline ? adapterTypeExpression : null,
            staticHeaders,
            parameters);
    }

    /// <summary>Reports an error when a method uses a source-generation-only attribute but cannot generate inline.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="context">The shared generation context.</param>
    private static void ReportSourceGenOnlyAttributeMisuse(
        IMethodSymbol methodSymbol,
        InterfaceGenerationContext context)
    {
        foreach (var parameter in methodSymbol.Parameters)
        {
            foreach (var attribute in parameter.GetAttributes())
            {
                if (!IsRefitAttribute(attribute.AttributeClass, QueryNameAttributeDisplayName)
                    && !IsRefitAttribute(attribute.AttributeClass, EncodedAttributeDisplayName)
                    && !IsRefitAttribute(attribute.AttributeClass, QueryConverterAttributeDisplayName))
                {
                    continue;
                }

                context.Diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.SourceGenOnlyAttributeRequiresInlineRequest,
                    methodSymbol.Locations.IsEmpty ? null : methodSymbol.Locations[0],
                    methodSymbol.Name,
                    attribute.AttributeClass!.Name));
                return;
            }
        }
    }

    /// <summary>Determines whether an HTTP method may carry an implicit request body.</summary>
    /// <param name="httpMethod">The HTTP method name.</param>
    /// <returns><see langword="true"/> for POST, PUT and PATCH.</returns>
    private static bool IsBodyCapableHttpMethod(string httpMethod) =>
        httpMethod is "POST" or "PUT" or "PATCH";

    /// <summary>Determines whether a method's request can be constructed by generated inline code.</summary>
    /// <param name="parameterEligibility">Whether every parameter binding is inline-supported.</param>
    /// <param name="returnShapeEligible">Whether the return shape is inline-eligible (a built-in async shape or an adapter-backed return type).</param>
    /// <param name="httpMethod">The HTTP method name.</param>
    /// <param name="path">The raw and normalized path forms from the HTTP method attribute.</param>
    /// <param name="parameters">The parsed request parameter models.</param>
    /// <param name="unsupportedMetadata">Whether the method carries metadata the inline emitter does not handle.</param>
    /// <returns><see langword="true"/> when the request is inline-eligible.</returns>
    /// <remarks>
    /// Generic methods are inline-eligible: a type parameter flows straight through to the generic runner
    /// (<c>SendAsync&lt;T, TBody&gt;</c>) with no reflection. Positions where an open type parameter cannot generate
    /// correctly or trim-safely — a complex query object (its properties are only known per value) or a form-url-encoded
    /// body (<c>[DynamicallyAccessedMembers]</c>) — are excluded upstream through <paramref name="parameterEligibility"/>.
    /// </remarks>
    private static bool CanGenerateInlineRequest(
        bool parameterEligibility,
        bool returnShapeEligible,
        string httpMethod,
        in RequestPathForms path,
        ImmutableEquatableArray<RequestParameterModel> parameters,
        bool unsupportedMetadata) =>
        parameterEligibility
        && returnShapeEligible
        && httpMethod.Length > 0
        && IsPathSupported(path.Raw)
        && IsPathSupported(path.Normalized)
        && IsSupportedInlineBody(parameters)
        && !unsupportedMetadata;

    /// <summary>Extracts the path parameter placeholder names and their locations from a URL template.</summary>
    /// <param name="path">The normalized path template.</param>
    /// <returns>A map of placeholder name to the ranges where each placeholder occurs in the template.</returns>
    private static Dictionary<string, List<Range>> ExtractPathParameterPlaceholderNames(string path)
    {
        var pathSpan = path.AsSpan();

        var i = pathSpan.IndexOf('{');
        if (i < 0)
        {
            return [];
        }

        var paramNames = new Dictionary<string, List<Range>>(StringComparer.OrdinalIgnoreCase);
        var j = i + pathSpan[i..].IndexOfAny('}', '/');

        // i always points at a '{' that IndexOf located, so it is always in range; only j can fall behind i
        // (when no '}' or '/' follows the brace), which ends the scan.
        while (j > i)
        {
            if (pathSpan[j] == '}')
            {
                var paramName = pathSpan[(i + 1)..j].ToString();
                var location = new Range(i, j + 1);
                if (paramNames.TryGetValue(paramName, out var values))
                {
                    values.Add(location);
                }
                else
                {
                    paramNames[paramName] = [location];
                }
            }

            var i2 = pathSpan[j..].IndexOf('{');
            if (i2 < 0)
            {
                break;
            }

            i = j;
            i += i2;
            j = i + pathSpan[i..].IndexOfAny('}', '/');
        }

        return paramNames;
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
        return !arguments.IsEmpty && arguments[0].Value is string path
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
            var attributeClass = attribute.AttributeClass;
            if (IsRefitAttribute(attributeClass, MultipartAttributeDisplayName)
                || IsRefitAttribute(attributeClass, QueryUriFormatAttributeDisplayName))
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
            if (!IsRefitAttribute(attribute.AttributeClass, HeadersAttributeDisplayName))
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

    /// <summary>Parses request parameter bindings for the generated inline path.</summary>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="parameterLocations">The placeholder names in the URL with their locations.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol used to classify inline-eligible path parameter types, or null when unavailable.</param>
    /// <param name="allowImplicitBody">Whether an un-attributed complex parameter becomes the implicit request body.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="canGenerateInline">Receives whether every parameter is supported.</param>
    /// <returns>The parsed request parameter models.</returns>
    private static ImmutableEquatableArray<RequestParameterModel> ParseRequestParameters(
        in ImmutableArray<IParameterSymbol> parameters,
        Dictionary<string, List<Range>> parameterLocations,
        INamedTypeSymbol? formattableSymbol,
        bool allowImplicitBody,
        InterfaceGenerationContext context,
        out bool canGenerateInline)
    {
        if (parameters.IsEmpty)
        {
            canGenerateInline = true;
            return ImmutableEquatableArray<RequestParameterModel>.Empty;
        }

        // An explicit [Body] anywhere suppresses implicit body detection, matching the reflection builder.
        var implicitBodyEligible = allowImplicitBody && !HasExplicitBodyParameter(parameters);

        var requestParameters = new RequestParameterModel[parameters.Length];
        var bodyCount = 0;
        var cancellationTokenCount = 0;
        var headerCollectionCount = 0;
        var implicitBodyAssigned = false;
        canGenerateInline = true;

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var name = ResolveUrlName(parameter);
            _ = parameterLocations.TryGetValue(name, out var location);
            List<Range>? roundTripLocation = null;
            if (location is null)
            {
                _ = parameterLocations.TryGetValue("**" + name, out roundTripLocation);
            }

            var classification = new LooseParameterContext(
                name,
                location?.ToImmutableEquatableArray(),
                roundTripLocation?.ToImmutableEquatableArray(),
                parameterLocations,
                formattableSymbol,
                implicitBodyEligible,
                context);
            var parsedParameter = ParseRequestParameter(parameter, classification, ref implicitBodyAssigned);
            requestParameters[i] = parsedParameter.Parameter;
            bodyCount += parsedParameter.BodyCount;
            cancellationTokenCount += parsedParameter.CancellationTokenCount;
            headerCollectionCount += parsedParameter.HeaderCollectionCount;
            canGenerateInline &= parsedParameter.CanGenerateInline;
        }

        // More than one body, cancellation token, header collection, or [Authorize] parameter is an invalid
        // definition the reflection builder rejects; fall back so its validation still throws.
        canGenerateInline &= HasInlineableParameterCounts(
            bodyCount,
            cancellationTokenCount,
            headerCollectionCount,
            requestParameters);

        return ImmutableEquatableArrayFactory.FromArray(requestParameters);
    }

    /// <summary>Determines whether the single-instance parameter counts allow inline generation.</summary>
    /// <param name="bodyCount">The number of body parameters.</param>
    /// <param name="cancellationTokenCount">The number of cancellation token parameters.</param>
    /// <param name="headerCollectionCount">The number of header collection parameters.</param>
    /// <param name="parameters">The parsed request parameters, scanned for <c>[Authorize]</c> parameters.</param>
    /// <returns><see langword="true"/> when no single-instance binding appears more than once.</returns>
    private static bool HasInlineableParameterCounts(
        int bodyCount,
        int cancellationTokenCount,
        int headerCollectionCount,
        RequestParameterModel[] parameters) =>
        bodyCount <= 1
        && cancellationTokenCount <= 1
        && headerCollectionCount <= 1
        && CountAuthorizeParameters(parameters) <= 1;

    /// <summary>Counts the <c>[Authorize]</c> parameters (Authorization headers carrying a scheme prefix).</summary>
    /// <param name="parameters">The parsed request parameters.</param>
    /// <returns>The number of <c>[Authorize]</c> parameters.</returns>
    private static int CountAuthorizeParameters(RequestParameterModel[] parameters)
    {
        var count = 0;
        foreach (var parameter in parameters)
        {
            if (parameter is { Kind: RequestParameterKind.Header, HeaderValuePrefix: not null })
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Resolves a parameter's URL name, honoring an <c>[AliasAs]</c> attribute.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <returns>The alias name or the declared parameter name.</returns>
    private static string ResolveUrlName(IParameterSymbol parameter)
    {
        var aliasAttr = FindParameterAttribute(parameter, AliasAsAttributeDisplayName);
        return aliasAttr is not null ? GetFirstStringArgument(aliasAttr) ?? parameter.Name : parameter.Name;
    }

    /// <summary>Determines whether any parameter carries an explicit <c>[Body]</c> attribute.</summary>
    /// <param name="parameters">The method parameters.</param>
    /// <returns><see langword="true"/> when an explicit body parameter exists.</returns>
    private static bool HasExplicitBodyParameter(in ImmutableArray<IParameterSymbol> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (HasParameterAttribute(parameter, BodyAttributeDisplayName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Parses one request parameter binding.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <param name="implicitBodyAssigned">Tracks whether an earlier parameter already claimed the implicit body.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ParseRequestParameter(
        IParameterSymbol parameter,
        in LooseParameterContext context,
        ref bool implicitBodyAssigned)
    {
        var parameterType = QualifyType(parameter.Type, context.Generation);
        if (IsCancellationToken(parameter.Type))
        {
            return CancellationTokenParameter(parameter, parameterType, context.Locations, context.Generation);
        }

        if (TryParseBodyParameter(parameter, parameterType, context.Generation, out var bodyParameter))
        {
            // A form-url-encoded body of a type that references a type parameter would emit
            // CreateUrlEncodedBodyContent<T>, whose [DynamicallyAccessedMembers(PublicProperties)] an open type
            // parameter cannot satisfy (IL2091), so it keeps using the reflection request builder.
            var bodyEligible = bodyParameter.BodySerializationMethod != "UrlEncoded"
                || !ReferencesTypeParameter(parameter.Type);
            return new(bodyParameter, bodyEligible, 1, 0, 0);
        }

        if (TryParseHeaderParameter(parameter, parameterType, context.Generation, out var headerParameter))
        {
            return new(headerParameter, true, 0, 0, 0);
        }

        if (TryParseHeaderCollectionParameter(parameter, parameterType, context.Generation, out var headerCollectionParameter))
        {
            return new(
                headerCollectionParameter,
                headerCollectionParameter.Kind == RequestParameterKind.HeaderCollection,
                0,
                0,
                1);
        }

        return TryParsePropertyParameter(parameter, parameterType, context.Generation, out var propertyParameter)
            ? ParsePropertyQueryBinding(parameter, parameterType, context.UrlName, context.FormattableSymbol, context.Generation, propertyParameter)
            : ParseBoundPathParameter(parameter, parameterType, context)
                ?? ClassifyLooseParameter(parameter, parameterType, context, ref implicitBodyAssigned);
    }

    /// <summary>Builds the cancellation token parameter binding.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="locations">The parameter's placeholder locations, if any.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter CancellationTokenParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range>? locations,
        InterfaceGenerationContext context) =>
        new(
            new(
                parameter.MetadataName,
                parameterType,
                locations,
                BuildParameterAttributes(parameter, context),
                RequestParameterKind.CancellationToken,
                CanBeNull(parameter.Type, parameter.NullableAnnotation),
                string.Empty,
                string.Empty,
                string.Empty,
                BodyBufferMode.None),
            true,
            0,
            1,
            0);

    /// <summary>Parses a parameter bound to a path placeholder, when one exists.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed path binding, or <see langword="null"/> when the parameter has no placeholder.</returns>
    private static ParsedRequestParameter? ParseBoundPathParameter(
        IParameterSymbol parameter,
        string parameterType,
        in LooseParameterContext context) =>
        context switch
        {
            { Locations: { } locations } => ParseDirectPathParameter(parameter, parameterType, locations, context),
            { RoundTripLocations: { } roundTripLocations } => ParseRoundTripPathParameter(parameter, parameterType, roundTripLocations, context),
            _ => null,
        };

    /// <summary>Parses a parameter bound to a plain <c>{name}</c> placeholder.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="locations">The placeholder locations.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ParseDirectPathParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range> locations,
        in LooseParameterContext context) =>
        IsSimpleType(parameter.Type, context.FormattableSymbol)
            ? new(
                PathRequestParameter(parameter, parameterType, locations, context.Generation) with
                {
                    ValueFormat = BuildValueFormat(parameter.Type, NormalizeFormat(ParseParameterQueryData(parameter).Format), context.FormattableSymbol, context.Generation),
                    PreEncoded = HasParameterAttribute(parameter, EncodedAttributeDisplayName),
                },
                true,
                0,
                0,
                0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);

    /// <summary>Parses a parameter bound to a round-tripping <c>{**name}</c> placeholder.</summary>
    /// <remarks>Round-tripping normally needs the reflection builder's per-segment escaping, but an
    /// <c>[Encoded]</c> string value passes through verbatim, so it becomes a plain inline substitution.</remarks>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="roundTripLocations">The placeholder locations.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ParseRoundTripPathParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range> roundTripLocations,
        in LooseParameterContext context)
    {
        var encoded = HasParameterAttribute(parameter, EncodedAttributeDisplayName);

        // [Encoded] keeps the caller-encoded string verbatim (string only). A non-[Encoded] catch-all splits the
        // value's string form on '/', formatting and escaping each segment while preserving the separators, exactly
        // as the reflection builder does — so any type is supported inline.
        return encoded && parameter.Type.SpecialType != SpecialType.System_String
            ? new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0)
            : new(
                PathRequestParameter(parameter, parameterType, roundTripLocations, context.Generation) with
                {
                    ValueFormat = BuildValueFormat(parameter.Type, null, context.FormattableSymbol, context.Generation),
                    PreEncoded = true,
                    IsRoundTrip = !encoded,
                },
                true,
                0,
                0,
                0);
    }

    /// <summary>Classifies a parameter with no path binding as a flag, implicit body, query value, or fallback.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <param name="implicitBodyAssigned">Tracks whether an earlier parameter already claimed the implicit body.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ClassifyLooseParameter(
        IParameterSymbol parameter,
        string parameterType,
        in LooseParameterContext context,
        ref bool implicitBodyAssigned)
    {
        // Dotted {param.Prop} placeholders bind object properties through the reflection builder.
        if (HasDottedPlaceholderFor(context.ParameterLocations, context.UrlName))
        {
            return new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
        }

        // [Authorize] emits an Authorization header of "{scheme} {value}"; the scheme is a compile-time constant.
        if (FindParameterAttribute(parameter, AuthorizeAttributeDisplayName) is { } authorizeAttribute)
        {
            var scheme = GetFirstStringArgument(authorizeAttribute) ?? DefaultAuthorizeScheme;
            return new(
                BuildAuthorizeHeaderParameter(parameter, parameterType, scheme, context.Generation),
                true,
                0,
                0,
                0);
        }

        if (HasParameterAttribute(parameter, QueryNameAttributeDisplayName))
        {
            var flagModel = TryBuildFlagModel(parameter, context.FormattableSymbol, context.Generation);
            return flagModel is not null
                ? new(QueryRequestParameter(parameter, parameterType, flagModel, context.Generation), true, 0, 0, 0)
                : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
        }

        // Body resolution precedes query mapping in the reflection builder: on POST/PUT/PATCH the first
        // un-attributed complex parameter is the implicit body; a second one throws there, so fall back.
        if (context.ImplicitBodyEligible && IsImplicitBodyCandidate(parameter))
        {
            return ClaimImplicitBody(parameter, parameterType, context.Generation, ref implicitBodyAssigned);
        }

        return TryBuildQueryModel(parameter, context.UrlName, context.FormattableSymbol, context.Generation, out var query)
            ? new(QueryRequestParameter(parameter, parameterType, query!, context.Generation), true, 0, 0, 0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
    }

    /// <summary>Determines whether a parameter matches the reflection builder's implicit body candidacy rules.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> for un-attributed non-string reference-type parameters.</returns>
    private static bool IsImplicitBodyCandidate(IParameterSymbol parameter) =>
        !parameter.Type.IsValueType
        && parameter.Type.SpecialType != SpecialType.System_String
        && !HasParameterAttribute(parameter, QueryAttributeDisplayName)
        && !HasParameterAttribute(parameter, QueryConverterAttributeDisplayName);

    /// <summary>Claims the implicit body slot, falling back when an earlier parameter already claimed it.</summary>
    /// <param name="parameter">The parameter to bind.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="implicitBodyAssigned">Tracks whether an earlier parameter already claimed the implicit body.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ClaimImplicitBody(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context,
        ref bool implicitBodyAssigned)
    {
        if (implicitBodyAssigned)
        {
            return new(UnsupportedRequestParameter(parameter, parameterType, context), false, 0, 0, 0);
        }

        implicitBodyAssigned = true;
        return new(ImplicitBodyRequestParameter(parameter, parameterType, context), true, 1, 0, 0);
    }

    /// <summary>Attaches query-binding metadata to a property parameter that also carries <c>[Query]</c>.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="urlName">The resolved URL name.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="propertyParameter">The parsed property parameter model.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    private static ParsedRequestParameter ParsePropertyQueryBinding(
        IParameterSymbol parameter,
        string parameterType,
        string urlName,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        RequestParameterModel propertyParameter)
    {
        if (!HasParameterAttribute(parameter, QueryAttributeDisplayName))
        {
            return new(propertyParameter, true, 0, 0, 0);
        }

        // A [Property] parameter that also carries [Query] feeds both the request options and the query string.
        return TryBuildQueryModel(parameter, urlName, formattableSymbol, context, out var propertyQuery)
            ? new(propertyParameter with { Query = propertyQuery }, true, 0, 0, 0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context), false, 0, 0, 0);
    }

    /// <summary>Builds a query request parameter model.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The query request parameter model.</returns>
    private static RequestParameterModel QueryRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        QueryParameterModel query,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Query,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None)
        {
            Query = query,
        };

    /// <summary>Builds the implicit body parameter model for POST/PUT/PATCH methods.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The implicit body parameter model.</returns>
    private static RequestParameterModel ImplicitBodyRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Body,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            "Serialized",
            BodyBufferMode.Settings);

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
            ContainingNamespace.Name: "System",
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
                    ContainingNamespace.ContainingNamespace.Name: "System",
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
            ContainingNamespace.ContainingNamespace.Name: "System",
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

    /// <summary>Gets return-type details required by the shared generated request runner.</summary>
    /// <param name="returnType">The declared return type, or an adapter's wrapped result type.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The parsed return type details.</returns>
    private static RequestReturnTypes GetRequestReturnTypes(ITypeSymbol returnType, InterfaceGenerationContext context)
    {
        var resultType = GetReturnResultType(returnType);
        var isApiResponse = IsApiResponseType(resultType);
        var deserializedResultType = GetDeserializedResultTypeName(resultType, isApiResponse, context);
        var disposeResponse = ShouldDisposeResponse(deserializedResultType);

        return new(
            QualifyType(resultType, context),
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
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The deserialization target type.</returns>
    private static string GetDeserializedResultTypeName(ITypeSymbol resultType, bool isApiResponse, InterfaceGenerationContext context)
    {
        if (!isApiResponse)
        {
            return QualifyType(resultType, context);
        }

        var namedType = (INamedTypeSymbol)resultType;
        return namedType.MetadataName == "IApiResponse"
            ? "global::System.Net.Http.HttpContent"
            : QualifyType(namedType.TypeArguments[0], context);
    }

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

    /// <summary>Bundles the lookup state used to classify one loosely-bound parameter.</summary>
    /// <param name="UrlName">The resolved URL name: the <c>[AliasAs]</c> name or the declared parameter name.</param>
    /// <param name="Locations">The parameter's placeholder locations, or null when it has no placeholder.</param>
    /// <param name="RoundTripLocations">The parameter's round-tripping (<c>{**name}</c>) placeholder locations, if any.</param>
    /// <param name="ParameterLocations">All placeholder names in the URL, used to detect dotted property bindings.</param>
    /// <param name="FormattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="ImplicitBodyEligible">Whether an un-attributed complex parameter becomes the implicit request body.</param>
    /// <param name="Generation">The interface generation context, carrying the extern-alias collector for type qualification.</param>
    private readonly record struct LooseParameterContext(
        string UrlName,
        ImmutableEquatableArray<Range>? Locations,
        ImmutableEquatableArray<Range>? RoundTripLocations,
        Dictionary<string, List<Range>> ParameterLocations,
        INamedTypeSymbol? FormattableSymbol,
        bool ImplicitBodyEligible,
        InterfaceGenerationContext Generation);

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

    /// <summary>The raw and normalized forms of a method's path template.</summary>
    /// <param name="Raw">The raw path literal from the HTTP method attribute.</param>
    /// <param name="Normalized">The path after constant-path normalization (fragment/empty-query cleanup).</param>
    private readonly record struct RequestPathForms(string Raw, string Normalized);
}
