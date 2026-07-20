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

    /// <summary>The shared empty placeholder set returned for paths that declare no path parameters.</summary>
    /// <remarks>Most parsed paths carry no <c>{placeholder}</c>, so the empty value carries no backing array and no
    /// per-parse allocation - a cost otherwise paid on every keystroke because the parser re-runs for the whole
    /// compilation on each edit.</remarks>
    private static readonly PathParameterLocations EmptyPathParameters = PathParameterLocations.Empty;

    /// <summary>Parses the request metadata needed by generated request construction.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="returnTypeInfo">The classified return type shape.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The parsed request metadata.</returns>
    internal static RequestModel ParseRequest(
        IMethodSymbol methodSymbol,
        ReturnTypeInfo returnTypeInfo,
        in InterfaceGenerationContext context)
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
    internal static (string HttpMethod, string Path, string NormalizedPath, PathParameterLocations PathParameters) ResolveRequestTarget(
        IMethodSymbol methodSymbol,
        in InterfaceGenerationContext context)
    {
        var httpMethodAttribute = FindHttpMethodAttribute(
            methodSymbol,
            context.HttpMethodBaseAttributeSymbol)!;

        var httpMethod = GetHttpMethodName(httpMethodAttribute.AttributeClass!);
        var path = CombinePathPrefix(context.PathPrefix, GetHttpPath(httpMethodAttribute));
        var normalizedPath = NormalizeConstantPathForInline(path);
        var pathParameters = ExtractPathParameterPlaceholderNames(normalizedPath);
        return (httpMethod, path, normalizedPath, pathParameters);
    }

    /// <summary>Classifies the return type against a registered return-type adapter, if one matches.</summary>
    /// <param name="returnType">The method's declared return type.</param>
    /// <param name="context">The shared generation context.</param>
    /// <returns>The adapter type expression (or null) and the result type to classify the request against.</returns>
    internal static (string? AdapterTypeExpression, ITypeSymbol ResultTypeSource) ResolveReturnTypeAdapter(
        ITypeSymbol returnType,
        in InterfaceGenerationContext context) =>
        TryMatchReturnTypeAdapter(returnType, context, out var closedAdapter, out var adapterResultType)
            ? (QualifyType(closedAdapter, context), adapterResultType)
            : (null, returnType);

    /// <summary>Determines whether a return type shape (or its adapter) is eligible for inline request generation.</summary>
    /// <param name="returnTypeInfo">The classified return type shape.</param>
    /// <param name="adapterTypeExpression">The registered adapter expression, or null.</param>
    /// <returns><see langword="true"/> when the return shape can be generated inline.</returns>
    internal static bool IsInlineReturnShape(ReturnTypeInfo returnTypeInfo, string? adapterTypeExpression) =>
        returnTypeInfo is ReturnTypeInfo.AsyncVoid or ReturnTypeInfo.AsyncResult or ReturnTypeInfo.AsyncEnumerable
            or ReturnTypeInfo.Observable or ReturnTypeInfo.RequestMessage
        || adapterTypeExpression is not null;

    /// <summary>Reports an error when a method uses a source-generation-only attribute but cannot generate inline.</summary>
    /// <param name="methodSymbol">The Refit method symbol.</param>
    /// <param name="context">The shared generation context.</param>
    internal static void ReportSourceGenOnlyAttributeMisuse(
        IMethodSymbol methodSymbol,
        in InterfaceGenerationContext context)
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
    internal static bool IsBodyCapableHttpMethod(string httpMethod) =>
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
    internal static bool CanGenerateInlineRequest(
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
        && IsUrlBindingSupported(path, parameters)

        // A multipart method with an explicit [Body] is an invalid combination the reflection builder rejects; fall
        // back so its validation still throws instead of emitting a non-multipart body request.
        && (!isMultipart || !HasBodyParameter(parameters));

    /// <summary>Determines whether a method's <c>[Url]</c> binding, if any, can be emitted inline.</summary>
    /// <param name="path">The raw and normalized path forms from the HTTP method attribute.</param>
    /// <param name="parameters">The parsed request parameter models.</param>
    /// <returns><see langword="true"/> when the method has no <c>[Url]</c> parameter, or has exactly one alongside an
    /// empty path template and no path placeholders. Other shapes fall back to the reflection builder, whose
    /// validation throws for the invalid combination.</returns>
    internal static bool IsUrlBindingSupported(
        in RequestPathForms path,
        ImmutableEquatableArray<RequestParameterModel> parameters)
    {
        var urlCount = 0;
        var hasPathParameter = false;
        foreach (var parameter in parameters)
        {
            if (parameter.Kind == RequestParameterKind.Url)
            {
                urlCount++;
            }
            else if (parameter.Kind == RequestParameterKind.Path)
            {
                hasPathParameter = true;
            }
        }

        // A method with no [Url] parameter is unconstrained here. Otherwise the [Url] parameter provides the full
        // absolute URI, so the path template must be empty and carry no placeholders, and only one may supply the URL.
        return urlCount == 0
            || (urlCount == 1
                && !hasPathParameter
                && IsEmptyOrRootPath(path.Raw)
                && IsEmptyOrRootPath(path.Normalized));
    }

    /// <summary>Determines whether a path template is empty or the bare root, so a <c>[Url]</c> parameter may supply the URI.</summary>
    /// <param name="path">The path template.</param>
    /// <returns><see langword="true"/> when the template is empty or <c>"/"</c>.</returns>
    internal static bool IsEmptyOrRootPath(string path) =>
        string.IsNullOrEmpty(path) || path == "/";

    /// <summary>Extracts the path parameter placeholder names and their locations from a URL template.</summary>
    /// <param name="path">The normalized path template.</param>
    /// <returns>The placeholder occurrences (name and range) discovered in the template.</returns>
    /// <remarks>The occurrences are collected into a single exact-sized array rather than a dictionary of per-name
    /// range lists: placeholder counts are tiny, so a linear scan matches parameters without the dictionary, its
    /// bucket array, and the per-name lists that the previous grouping allocated on every parse.</remarks>
    internal static PathParameterLocations ExtractPathParameterPlaceholderNames(string path)
    {
        var pathSpan = path.AsSpan();
        var count = 0;
        var counter = new PathPlaceholderScanner(pathSpan);
        while (counter.MoveNext())
        {
            count++;
        }

        if (count == 0)
        {
            return EmptyPathParameters;
        }

        var occurrences = new PathPlaceholderOccurrence[count];
        var hasRoundTrip = false;
        var hasDotted = false;
        var index = 0;
        var scanner = new PathPlaceholderScanner(pathSpan);
        while (scanner.MoveNext())
        {
            // A trailing '?' marks the placeholder optional ({name?}); strip it from the bound name so the
            // parameter still matches, while the location range keeps covering the whole {name?} so the runtime
            // path builder can detect and drop the segment when the bound value is null.
            var nameSpan = pathSpan[(scanner.Start + 1)..scanner.End];
            if (!nameSpan.IsEmpty && nameSpan[nameSpan.Length - 1] == '?')
            {
                nameSpan = nameSpan[..^1];
            }

            hasRoundTrip |= nameSpan.Length >= 2 && nameSpan[0] == '*' && nameSpan[1] == '*';
            hasDotted |= nameSpan.IndexOf('.') >= 0;
            occurrences[index] = new(nameSpan.ToString(), new Range(scanner.Start, scanner.End + 1));
            index++;
        }

        return new(occurrences, hasRoundTrip, hasDotted);
    }

    /// <summary>Gets the literal path from a Refit HTTP method attribute.</summary>
    /// <param name="attributeData">The attribute data.</param>
    /// <returns>The path literal.</returns>
    internal static string GetHttpPath(AttributeData attributeData)
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
    internal static void AppendNonEmptyQueryPart(
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
    internal static int? ResolveQueryUriFormat(IMethodSymbol methodSymbol)
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
    internal static int? TryReadQueryUriFormat(AttributeData attribute) =>
        IsRefitAttribute(attribute.AttributeClass, QueryUriFormatAttributeDisplayName)
        && attribute.ConstructorArguments.Length == 1
        && attribute.ConstructorArguments[0].Value is int uriFormat
            ? uriFormat
            : null;

    /// <summary>Reads the per-call timeout in milliseconds from a method's <c>[Timeout]</c> attribute.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <returns>The timeout in milliseconds, or 0 when the method has no <c>[Timeout]</c>.</returns>
    internal static int ResolveTimeout(IMethodSymbol methodSymbol)
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
    internal static int? TryReadTimeout(AttributeData attribute) =>
        IsRefitAttribute(attribute.AttributeClass, TimeoutAttributeDisplayName)
        && attribute.ConstructorArguments.Length == 1
        && attribute.ConstructorArguments[0].Value is int milliseconds
            ? milliseconds
            : null;

    /// <summary>Parses the static headers declared on inherited interfaces, the declaring interface, and the method.</summary>
    /// <param name="methodSymbol">The method whose header metadata should be parsed.</param>
    /// <returns>The final static header set.</returns>
    internal static ImmutableEquatableArray<HeaderModel> ParseStaticHeaders(IMethodSymbol methodSymbol)
    {
        // Most methods declare no [Headers], so the list is created only once a header is actually found - a saved
        // per-method allocation on the hot per-keystroke parse path. A null list flattens to the shared empty set.
        List<HeaderModel>? headers = null;

        var inheritedInterfaces = methodSymbol.ContainingType.AllInterfaces;
        for (var i = inheritedInterfaces.Length - 1; i >= 0; i--)
        {
            AddStaticHeaders(ref headers, inheritedInterfaces[i].GetAttributes());
        }

        AddStaticHeaders(ref headers, methodSymbol.ContainingType.GetAttributes());
        AddStaticHeaders(ref headers, methodSymbol.GetAttributes());

        return headers.ToImmutableEquatableArray();
    }

    /// <summary>Adds headers from a collection of attributes, replacing earlier values for the same header name.</summary>
    /// <param name="headers">The header list, created on first use.</param>
    /// <param name="attributes">The attributes to inspect.</param>
    internal static void AddStaticHeaders(
        ref List<HeaderModel>? headers,
        in ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (!IsRefitAttribute(attribute.AttributeClass, HeadersAttributeDisplayName))
            {
                continue;
            }

            AddHeadersAttributeValues(ref headers, attribute);
        }
    }

    /// <summary>Adds the string values from a <c>HeadersAttribute</c> to the static header list.</summary>
    /// <param name="headers">The header list, created on first use.</param>
    /// <param name="attribute">The attribute data.</param>
    internal static void AddHeadersAttributeValues(ref List<HeaderModel>? headers, AttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Kind == TypedConstantKind.Array)
            {
                foreach (var value in argument.Values)
                {
                    if (value.Value is string header)
                    {
                        AddStaticHeader(headers ??= [], header);
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
    internal static RequestReturnTypes GetRequestReturnTypes(ITypeSymbol returnType, in InterfaceGenerationContext context)
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
    internal static ITypeSymbol GetReturnResultType(ITypeSymbol returnType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
        {
            // The single-arity guard above means the cached Name identifies each wrapper as precisely as its
            // "Name`1" MetadataName would, without the concatenation MetadataName performs on every generic type.
            // The name is checked first (a cheap compare against a cached property) so the namespace walk only runs
            // for the handful of well-known wrapper shapes, never for an arbitrary user return type.
            if (namedType.Name is "Task" or "ValueTask"
                && IsInNamespace(namedType, "System.Threading.Tasks"))
            {
                return namedType.TypeArguments[0];
            }

            if (namedType.Name == "IAsyncEnumerable"
                && IsInNamespace(namedType, "System.Collections.Generic"))
            {
                return namedType.TypeArguments[0];
            }

            if (namedType.Name == "IObservable" && IsInNamespace(namedType, "System"))
            {
                return namedType.TypeArguments[0];
            }
        }

        return returnType;
    }

    /// <summary>Determines whether a type is one of Refit's API response wrappers.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> for API response wrappers.</returns>
    internal static bool IsApiResponseType(ITypeSymbol type) =>
        type is INamedTypeSymbol namedType
        && namedType.MetadataName is "IApiResponse" or "ApiResponse`1" or "IApiResponse`1"
        && IsInNamespace(namedType, "Refit");

    /// <summary>Gets the response-content deserialization type for a method result type.</summary>
    /// <param name="resultType">The method result type.</param>
    /// <param name="isApiResponse">Whether the result is an API response wrapper.</param>
    /// <param name="context">The generation context, used to qualify extern-aliased types.</param>
    /// <returns>The deserialization target type.</returns>
    internal static string GetDeserializedResultTypeName(ITypeSymbol resultType, bool isApiResponse, in InterfaceGenerationContext context)
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
    internal readonly record struct RequestReturnTypes(
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
    internal readonly record struct LooseParameterContext(
        string UrlName,
        ImmutableEquatableArray<Range>? Locations,
        ImmutableEquatableArray<Range>? RoundTripLocations,
        PathParameterLocations ParameterLocations,
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
    internal readonly record struct ParsedRequestParameter(
        RequestParameterModel Parameter,
        bool CanGenerateInline,
        int BodyCount,
        int CancellationTokenCount,
        int HeaderCollectionCount);

    /// <summary>The raw and normalized forms of a method's path template.</summary>
    /// <param name="Raw">The raw path literal from the HTTP method attribute.</param>
    /// <param name="Normalized">The path after constant-path normalization (fragment/empty-query cleanup).</param>
    internal readonly record struct RequestPathForms(string Raw, string Normalized);

    /// <summary>A single <c>{placeholder}</c> occurrence in a path template.</summary>
    /// <param name="Name">The placeholder name (a trailing optional marker <c>?</c> already stripped).</param>
    /// <param name="Location">The range covering the whole <c>{placeholder}</c> including its braces.</param>
    internal readonly record struct PathPlaceholderOccurrence(string Name, Range Location);

    /// <summary>The path parameter placeholders discovered in one method's URL template.</summary>
    /// <remarks>Backed by a single occurrence array (or none for the shared empty value); placeholder counts are tiny,
    /// so parameters are matched by a linear scan instead of a dictionary keyed by name.</remarks>
    internal readonly struct PathParameterLocations : IEquatable<PathParameterLocations>
    {
        /// <summary>The length of the round-trip placeholder prefix (<c>**</c>).</summary>
        private const int RoundTripPrefixLength = 2;

        /// <summary>The placeholder occurrences in template order, or null for the shared empty value.</summary>
        private readonly PathPlaceholderOccurrence[]? _occurrences;

        /// <summary>Initializes a new instance of the <see cref="PathParameterLocations"/> struct.</summary>
        /// <param name="occurrences">The placeholder occurrences in template order.</param>
        /// <param name="hasRoundTrip">Whether any placeholder is a round-trip (<c>{**name}</c>) placeholder.</param>
        /// <param name="hasDotted">Whether any placeholder name binds a nested property (<c>{param.Prop}</c>).</param>
        public PathParameterLocations(PathPlaceholderOccurrence[] occurrences, bool hasRoundTrip, bool hasDotted)
        {
            _occurrences = occurrences;
            HasRoundTrip = hasRoundTrip;
            HasDotted = hasDotted;
        }

        /// <summary>Gets the shared empty value for paths that declare no placeholders.</summary>
        public static PathParameterLocations Empty => default;

        /// <summary>Gets a value indicating whether any placeholder is a round-trip (<c>{**name}</c>) placeholder.</summary>
        public bool HasRoundTrip { get; }

        /// <summary>Gets a value indicating whether any placeholder binds a nested property (<c>{param.Prop}</c>).</summary>
        public bool HasDotted { get; }

        /// <summary>Gets the placeholder occurrences in template order.</summary>
        public ReadOnlySpan<PathPlaceholderOccurrence> Occurrences => _occurrences;

        /// <summary>Determines whether two placeholder sets share the same backing occurrences.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true"/> when both wrap the same occurrence array.</returns>
        public static bool operator ==(PathParameterLocations left, PathParameterLocations right) => left.Equals(right);

        /// <summary>Determines whether two placeholder sets wrap different backing occurrences.</summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true"/> when the values differ.</returns>
        public static bool operator !=(PathParameterLocations left, PathParameterLocations right) => !left.Equals(right);

        /// <summary>Collects the ranges of the placeholder whose name matches a parameter's URL name.</summary>
        /// <param name="name">The parameter's URL name.</param>
        /// <param name="locations">Receives the matching placeholder ranges in template order.</param>
        /// <returns><see langword="true"/> when at least one placeholder matches.</returns>
        public bool TryGetDirectLocations(string name, out ImmutableEquatableArray<Range> locations)
        {
            var occurrences = _occurrences;
            if (occurrences is not null)
            {
                var matches = 0;
                foreach (var occurrence in occurrences)
                {
                    if (string.Equals(occurrence.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches++;
                    }
                }

                if (matches > 0)
                {
                    locations = BuildLocations(occurrences, name, matches, roundTrip: false);
                    return true;
                }
            }

            locations = ImmutableEquatableArray<Range>.Empty;
            return false;
        }

        /// <summary>Collects the ranges of the round-trip placeholder (<c>{**name}</c>) bound to a parameter's URL name.</summary>
        /// <param name="name">The parameter's URL name.</param>
        /// <param name="locations">Receives the matching placeholder ranges in template order.</param>
        /// <returns><see langword="true"/> when at least one round-trip placeholder matches.</returns>
        public bool TryGetRoundTripLocations(string name, out ImmutableEquatableArray<Range> locations)
        {
            var occurrences = _occurrences;
            if (occurrences is not null)
            {
                var matches = 0;
                foreach (var occurrence in occurrences)
                {
                    if (IsRoundTripMatch(occurrence.Name, name))
                    {
                        matches++;
                    }
                }

                if (matches > 0)
                {
                    locations = BuildLocations(occurrences, name, matches, roundTrip: true);
                    return true;
                }
            }

            locations = ImmutableEquatableArray<Range>.Empty;
            return false;
        }

        /// <inheritdoc/>
        public bool Equals(PathParameterLocations other) => ReferenceEquals(_occurrences, other._occurrences);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PathParameterLocations other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _occurrences?.GetHashCode() ?? 0;

        /// <summary>Determines whether a placeholder name is the round-trip form (<c>**name</c>) of a parameter name.</summary>
        /// <param name="placeholderName">The placeholder name.</param>
        /// <param name="name">The parameter's URL name.</param>
        /// <returns><see langword="true"/> when the placeholder is <c>**</c> followed by the parameter name.</returns>
        private static bool IsRoundTripMatch(string placeholderName, string name) =>
            placeholderName.Length == name.Length + RoundTripPrefixLength
            && placeholderName[0] == '*'
            && placeholderName[1] == '*'
            && string.Compare(placeholderName, RoundTripPrefixLength, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0;

        /// <summary>Builds the exact-sized range array for the placeholders matching a parameter's URL name.</summary>
        /// <param name="occurrences">The placeholder occurrences in template order.</param>
        /// <param name="name">The parameter's URL name.</param>
        /// <param name="matches">The number of matching occurrences.</param>
        /// <param name="roundTrip">Whether to match the round-trip placeholder form.</param>
        /// <returns>The matching placeholder ranges in template order.</returns>
        private static ImmutableEquatableArray<Range> BuildLocations(
            PathPlaceholderOccurrence[] occurrences,
            string name,
            int matches,
            bool roundTrip)
        {
            var ranges = new Range[matches];
            var index = 0;
            foreach (var occurrence in occurrences)
            {
                var matched = roundTrip
                    ? IsRoundTripMatch(occurrence.Name, name)
                    : string.Equals(occurrence.Name, name, StringComparison.OrdinalIgnoreCase);
                if (matched)
                {
                    ranges[index] = occurrence.Location;
                    index++;
                }
            }

            return ImmutableEquatableArrayFactory.FromArray(ranges);
        }
    }

    /// <summary>Walks a path template, yielding each <c>}</c>-terminated <c>{placeholder}</c> span.</summary>
    internal ref struct PathPlaceholderScanner
    {
        /// <summary>The path template being scanned.</summary>
        private readonly ReadOnlySpan<char> _path;

        /// <summary>The index of the current placeholder's opening brace.</summary>
        private int _i;

        /// <summary>The index of the current placeholder's terminator (a <c>}</c> or <c>/</c>).</summary>
        private int _j;

        /// <summary>Initializes a new instance of the <see cref="PathPlaceholderScanner"/> struct.</summary>
        /// <param name="path">The path template to scan.</param>
        public PathPlaceholderScanner(ReadOnlySpan<char> path)
        {
            _path = path;
            _i = path.IndexOf('{');

            // When there is no '{', keep _j <= _i so the first MoveNext ends immediately.
            _j = _i < 0 ? _i : _i + path[_i..].IndexOfAny('}', '/');
        }

        /// <summary>Gets the index of the current placeholder's opening brace.</summary>
        public int Start { get; private set; }

        /// <summary>Gets the index of the current placeholder's closing brace.</summary>
        public int End { get; private set; }

        /// <summary>Advances to the next <c>}</c>-terminated placeholder.</summary>
        /// <returns><see langword="true"/> when a placeholder was found.</returns>
        public bool MoveNext()
        {
            // _i always points at a '{' that IndexOf located, so it is always in range; only _j can fall behind _i
            // (when no '}' or '/' follows the brace), which ends the scan.
            while (_j > _i)
            {
                var currentStart = _i;
                var currentEnd = _j;
                var isClose = _path[currentEnd] == '}';

                var nextBrace = _path[currentEnd..].IndexOf('{');
                if (nextBrace < 0)
                {
                    _j = _i;
                }
                else
                {
                    _i = currentEnd + nextBrace;
                    _j = _i + _path[_i..].IndexOfAny('}', '/');
                }

                if (isClose)
                {
                    Start = currentStart;
                    End = currentEnd;
                    return true;
                }
            }

            return false;
        }
    }
}
