// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Request parsing helpers for the Refit source generator.</content>
internal static partial class Parser
{
    /// <summary>The <c>System</c> namespace name, matched structurally to identify well-known BCL types.</summary>
    private const string SystemNamespace = "System";

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

        var (httpMethod, path, normalizedPath, pathParameters) = ResolveRequestTarget(methodSymbol, context);

        // A registered IReturnTypeAdapter surfaces the declared return type; the HTTP call materializes the adapter's
        // wrapped result, so classify the return types against that inner type just like a Task<T>.
        var (adapterTypeExpression, resultTypeSource) = ResolveReturnTypeAdapter(methodSymbol.ReturnType, context);

        var returnTypes = GetRequestReturnTypes(resultTypeSource, context);
        var queryUriFormat = ResolveQueryUriFormat(methodSymbol);
        var timeoutMilliseconds = ResolveTimeout(methodSymbol);

        // A multipart method builds its parts inline; each part parameter is classified into a
        // MultipartFormDataContent entry instead of feeding the query string or an implicit body.
        var multipartBoundary = ResolveMultipartBoundary(methodSymbol, out var isMultipart);

        // Only POST/PUT/PATCH carry an implicit body, and a multipart method never does — every un-attributed
        // complex parameter is a form part rather than the request body.
        var allowImplicitBody = IsBodyCapableHttpMethod(httpMethod) && !isMultipart;

        var parameters = ParseRequestParameters(
            methodSymbol.Parameters,
            pathParameters,
            context.FormattableSymbol,
            allowImplicitBody,
            isMultipart,
            context,
            out var parameterEligibility);
        var staticHeaders = ParseStaticHeaders(methodSymbol);

        var canGenerateInline = CanGenerateInlineRequest(
            parameterEligibility,
            IsInlineReturnShape(returnTypeInfo, adapterTypeExpression),
            httpMethod,
            new(path, normalizedPath),
            parameters,
            isMultipart);

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
            parameters)
        {
            IsMultipart = isMultipart,
            MultipartBoundary = multipartBoundary,
            QueryUriFormat = queryUriFormat,
            TimeoutMilliseconds = timeoutMilliseconds,
        };
    }

    /// <summary>Resolves the HTTP verb, path, and path-parameter placeholders declared by a method's HTTP attribute.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The HTTP method, raw path, normalized path, and path parameter placeholders.</returns>
    private static (string HttpMethod, string Path, string NormalizedPath, Dictionary<string, List<Range>> PathParameters) ResolveRequestTarget(
        IMethodSymbol methodSymbol,
        InterfaceGenerationContext context)
    {
        var httpMethodAttribute = FindHttpMethodAttribute(
            methodSymbol,
            context.HttpMethodBaseAttributeSymbol)!;

        var httpMethod = GetHttpMethodName(httpMethodAttribute.AttributeClass!);
        var path = GetHttpPath(httpMethodAttribute);
        var normalizedPath = NormalizeConstantPathForInline(path);
        var pathParameters = ExtractPathParameterPlaceholderNames(normalizedPath);
        return (httpMethod, path, normalizedPath, pathParameters);
    }

    /// <summary>Classifies the return type against a registered return-type adapter, if one matches.</summary>
    /// <param name="returnType">The method's declared return type.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The adapter type expression (or null) and the result type to classify the request against.</returns>
    private static (string? AdapterTypeExpression, ITypeSymbol ResultTypeSource) ResolveReturnTypeAdapter(
        ITypeSymbol returnType,
        InterfaceGenerationContext context) =>
        TryMatchReturnTypeAdapter(returnType, context, out var closedAdapter, out var adapterResultType)
            ? (QualifyType(closedAdapter, context), adapterResultType)
            : (null, returnType);

    /// <summary>Determines whether a return type shape (or its adapter) is eligible for inline request generation.</summary>
    /// <param name="returnTypeInfo">The classified return type shape.</param>
    /// <param name="adapterTypeExpression">The registered adapter expression, or null.</param>
    /// <returns><see langword="true"/> when the return shape can be generated inline.</returns>
    private static bool IsInlineReturnShape(ReturnTypeInfo returnTypeInfo, string? adapterTypeExpression) =>
        returnTypeInfo is ReturnTypeInfo.AsyncVoid or ReturnTypeInfo.AsyncResult or ReturnTypeInfo.AsyncEnumerable
            or ReturnTypeInfo.Observable
        || adapterTypeExpression is not null;

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
                    methodSymbol.Locations[0],
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
    /// <param name="isMultipart">Whether the method is multipart, which cannot also carry an explicit <c>[Body]</c>.</param>
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
        bool isMultipart) =>
        parameterEligibility
        && returnShapeEligible
        && httpMethod.Length > 0
        && IsPathSupported(path.Raw)
        && IsPathSupported(path.Normalized)
        && IsSupportedInlineBody(parameters)

        // A multipart method with an explicit [Body] is an invalid combination the reflection builder rejects; fall
        // back so its validation still throws instead of emitting a non-multipart body request.
        && (!isMultipart || !HasBodyParameter(parameters));

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
            queryBuffer[queryLength] = '&';
            queryLength++;
        }

        path.CopyTo(partStart, queryBuffer, queryLength, partLength);
        queryLength += partLength;
    }

    /// <summary>Reads the <c>System.UriFormat</c> value from a method's <c>[QueryUriFormat]</c> attribute.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <returns>The <c>UriFormat</c> enum value, or null when the method has no <c>[QueryUriFormat]</c>.</returns>
    /// <remarks>The value re-encodes the whole built path and query, matching the reflection builder's final
    /// <c>Uri.GetComponents(PathAndQuery, QueryUriFormat)</c> pass, so the attribute no longer forces the runtime builder.</remarks>
    private static int? ResolveQueryUriFormat(IMethodSymbol methodSymbol)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (TryReadQueryUriFormat(attribute) is { } uriFormat)
            {
                return uriFormat;
            }
        }

        return null;
    }

    /// <summary>Reads the <c>UriFormat</c> value from an attribute if it is <c>[QueryUriFormat]</c>.</summary>
    /// <param name="attribute">The candidate attribute.</param>
    /// <returns>The <c>UriFormat</c> enum value, or null when the attribute is not <c>[QueryUriFormat]</c>.</returns>
    /// <remarks>The single-<c>int</c>-argument guards match the attribute's only constructor and cannot fail for a
    /// <c>[QueryUriFormat]</c> application that compiles.</remarks>
    [ExcludeFromCodeCoverage]
    private static int? TryReadQueryUriFormat(AttributeData attribute) =>
        IsRefitAttribute(attribute.AttributeClass, QueryUriFormatAttributeDisplayName)
        && attribute.ConstructorArguments.Length == 1
        && attribute.ConstructorArguments[0].Value is int uriFormat
            ? uriFormat
            : null;

    /// <summary>Reads the per-call timeout in milliseconds from a method's <c>[Timeout]</c> attribute.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <returns>The timeout in milliseconds, or 0 when the method has no <c>[Timeout]</c>.</returns>
    private static int ResolveTimeout(IMethodSymbol methodSymbol)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (TryReadTimeout(attribute) is { } timeoutMilliseconds)
            {
                return timeoutMilliseconds;
            }
        }

        return 0;
    }

    /// <summary>Reads the milliseconds value from an attribute if it is <c>[Timeout]</c>.</summary>
    /// <param name="attribute">The candidate attribute.</param>
    /// <returns>The timeout in milliseconds, or null when the attribute is not <c>[Timeout]</c>.</returns>
    /// <remarks>The single-<c>int</c>-argument guards match the attribute's only constructor and cannot fail for a
    /// <c>[Timeout]</c> application that compiles.</remarks>
    [ExcludeFromCodeCoverage]
    private static int? TryReadTimeout(AttributeData attribute) =>
        IsRefitAttribute(attribute.AttributeClass, TimeoutAttributeDisplayName)
        && attribute.ConstructorArguments.Length == 1
        && attribute.ConstructorArguments[0].Value is int milliseconds
            ? milliseconds
            : null;

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

            if (namedType.MetadataName == "IObservable`1" && ns == "System")
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
    /// <param name="IsMultipart">Whether the method is multipart, so an un-attributed parameter becomes a form part.</param>
    /// <param name="Generation">The interface generation context, carrying the extern-alias collector for type qualification.</param>
    private readonly record struct LooseParameterContext(
        string UrlName,
        ImmutableEquatableArray<Range>? Locations,
        ImmutableEquatableArray<Range>? RoundTripLocations,
        Dictionary<string, List<Range>> ParameterLocations,
        INamedTypeSymbol? FormattableSymbol,
        bool ImplicitBodyEligible,
        bool IsMultipart,
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
