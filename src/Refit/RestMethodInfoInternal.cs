// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Refit;

/// <summary>Internal representation of a Refit REST method holding the parsed metadata used to construct requests.</summary>
[DebuggerDisplay("{MethodInfo}")]
#if NET7_0_OR_GREATER
internal partial class RestMethodInfoInternal
#else
internal class RestMethodInfoInternal
#endif
{
    /// <summary>The HTTP PATCH method instance.</summary>
    private static readonly HttpMethod _patchMethod = new("PATCH");

#if !NET7_0_OR_GREATER
    /// <summary>The compiled regular expression that matches URL path parameters.</summary>
    private static readonly Regex _parameterRegexValue = new(
        "{(([^/?\\r\\n])*?)}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));
#endif

    /// <summary>The index of the header collection parameter, or a negative value when none is present.</summary>
    private readonly int _headerCollectionParameterIndex;

    /// <summary>Initializes a new instance of the <see cref="RestMethodInfoInternal"/> class.</summary>
    /// <param name="targetInterface">The interface type that declares the method.</param>
    /// <param name="methodInfo">The reflected method information.</param>
    /// <param name="refitSettings">The optional Refit settings to use.</param>
    [RequiresUnreferencedCode("Building request metadata from reflected interface methods requires request object property metadata to be available at runtime.")]
    public RestMethodInfoInternal(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type targetInterface,
        MethodInfo methodInfo,
        RefitSettings? refitSettings = null)
    {
        RefitSettings = refitSettings ?? new RefitSettings();
        ArgumentExceptionHelper.ThrowIfNull(targetInterface);
        ArgumentExceptionHelper.ThrowIfNull(methodInfo);
        Type = targetInterface;
        MethodInfo = methodInfo;

        var hma = methodInfo.GetCustomAttribute<HttpMethodAttribute>(true)
                  ?? throw new InvalidOperationException("Sequence contains no elements");

        HttpMethod = hma.Method;
        RelativePath = hma.Path;

        IsMultipart = methodInfo.GetCustomAttribute<MultipartAttribute>(true) is not null;

        MultipartBoundary = GetMultipartBoundary(methodInfo, IsMultipart);

        VerifyUrlPathIsSane(RelativePath, RefitSettings.UrlResolution);

        var (returnType, returnResultType, deserializedResultType) = DetermineReturnTypeInfo(methodInfo);
        ReturnType = returnType;
        ReturnResultType = returnResultType;
        DeserializedResultType = deserializedResultType;
        ShouldDisposeResponse = DetermineIfResponseMustBeDisposed(DeserializedResultType);

        // Exclude cancellation token parameters from this list
        ParameterInfoArray = GetNonCancellationTokenParameters(methodInfo.GetParameters());
        (ParameterMap, FragmentPath) = BuildParameterMap(RelativePath, ParameterInfoArray, RefitSettings.AllowUnmatchedRouteParameters);
        BodyParameterInfo = FindBodyParameter(ParameterInfoArray, IsMultipart, hma.Method);
        AuthorizeParameterInfo = FindAuthorizationParameter(ParameterInfoArray);

        Headers = ParseHeaders(targetInterface, methodInfo);
        HeaderParameterMap = BuildHeaderParameterMap(ParameterInfoArray);
        _headerCollectionParameterIndex = GetHeaderCollectionParameterIndex(ParameterInfoArray);
        PropertyParameterMap = BuildRequestPropertyMap(ParameterInfoArray);

        AttachmentNameMap = BuildAttachmentNameMap();
        QueryParameterMap = BuildQueryParameterMap();

        CancellationToken = FindCancellationTokenParameter(methodInfo);

        RestMethodInfo = new(Name, Type, MethodInfo, RelativePath, ReturnType);

        QueryUriFormat = methodInfo.GetCustomAttribute<QueryUriFormatAttribute>()?.UriFormat
                         ?? UriFormat.UriEscaped;

        IsApiResponse = DetermineIsApiResponse(ReturnResultType);
    }

    /// <summary>Gets the interface type that declares the method.</summary>
    public Type Type { get; }

    /// <summary>Gets the reflected method information.</summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>Gets the HTTP method used by the request.</summary>
    public HttpMethod HttpMethod { get; }

    /// <summary>Gets the relative URL path template for the method.</summary>
    public string RelativePath { get; }

    /// <summary>Gets a value indicating whether the request is a multipart request.</summary>
    public bool IsMultipart { get; }

    /// <summary>Gets the multipart boundary string used for multipart requests.</summary>
    public string MultipartBoundary { get; }

    /// <summary>Gets the public metadata describing this REST method.</summary>
    public RestMethodInfo RestMethodInfo { get; }

    /// <summary>Gets the cancellation token parameter, or null when none is present.</summary>
    public ParameterInfo? CancellationToken { get; }

    /// <summary>Gets the URI escaping format used for query parameters.</summary>
    public UriFormat QueryUriFormat { get; }

    /// <summary>Gets the static headers associated with the method.</summary>
    public Dictionary<string, string?> Headers { get; }

    /// <summary>Gets the map of parameter indexes to header names.</summary>
    public Dictionary<int, string> HeaderParameterMap { get; }

    /// <summary>Gets the map of parameter indexes to request property keys.</summary>
    public Dictionary<int, string> PropertyParameterMap { get; }

    /// <summary>Gets the body parameter information, or null when there is no body parameter.</summary>
    public Tuple<BodySerializationMethod, bool, int>? BodyParameterInfo { get; }

    /// <summary>Gets the authorization parameter information, or null when there is no authorize parameter.</summary>
    public Tuple<string, int>? AuthorizeParameterInfo { get; }

    /// <summary>Gets the map of parameter indexes to query string names.</summary>
    public Dictionary<int, string> QueryParameterMap { get; }

    /// <summary>Gets the map of parameter indexes to multipart attachment names.</summary>
    public Dictionary<int, Tuple<string, string>> AttachmentNameMap { get; }

    /// <summary>Gets the array of parameters excluding cancellation tokens.</summary>
    public ParameterInfo[] ParameterInfoArray { get; }

    /// <summary>Gets the map of parameter indexes to route parameter information.</summary>
    public Dictionary<int, RestMethodParameterInfo> ParameterMap { get; }

    /// <summary>Gets or sets the ordered fragments that make up the URL path.</summary>
    public List<ParameterFragment> FragmentPath { get; set; }

    /// <summary>Gets or sets the declared return type of the method.</summary>
    public Type ReturnType { get; set; }

    /// <summary>Gets or sets the result type wrapped by the return type.</summary>
    public Type ReturnResultType { get; set; }

    /// <summary>Gets or sets the type that the response content is deserialized into.</summary>
    public Type DeserializedResultType { get; set; }

    /// <summary>Gets the Refit settings used when building the request.</summary>
    public RefitSettings RefitSettings { get; }

    /// <summary>Gets a value indicating whether the method returns an API response wrapper.</summary>
    public bool IsApiResponse { get; }

    /// <summary>Gets a value indicating whether the response must be disposed by the caller.</summary>
    public bool ShouldDisposeResponse { get; }

    /// <summary>Gets a value indicating whether the method has a header collection parameter.</summary>
    public bool HasHeaderCollection => _headerCollectionParameterIndex >= 0;

    /// <summary>Gets the name of the method.</summary>
    private string Name => MethodInfo.Name;

    /// <summary>Determines whether the parameter at the given index is the header collection parameter.</summary>
    /// <param name="index">The parameter index to test.</param>
    /// <returns><see langword="true"/> when the parameter is the header collection parameter; otherwise <see langword="false"/>.</returns>
    public bool HeaderCollectionAt(int index) =>
        _headerCollectionParameterIndex >= 0 && _headerCollectionParameterIndex == index;

    /// <summary>Removes cancellation-token parameters from a reflected parameter array.</summary>
    /// <param name="parameters">The reflected method parameters.</param>
    /// <returns>The parameters used for request mapping.</returns>
    private static ParameterInfo[] GetNonCancellationTokenParameters(ParameterInfo[] parameters)
    {
        var count = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsCancellationTokenParameter(parameters[i]))
            {
                count++;
            }
        }

        if (count == parameters.Length)
        {
            return parameters;
        }

        var mappedParameters = new ParameterInfo[count];
        var index = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsCancellationTokenParameter(parameters[i]))
            {
                mappedParameters[index++] = parameters[i];
            }
        }

        return mappedParameters;
    }

    /// <summary>Determines whether a parameter is a cancellation token.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> for cancellation-token parameters.</returns>
    private static bool IsCancellationTokenParameter(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(CancellationToken)
        || parameter.ParameterType == typeof(CancellationToken?);

    /// <summary>Resolves the multipart boundary text for the method, defaulting when unspecified.</summary>
    /// <param name="methodInfo">The reflected method information.</param>
    /// <param name="isMultipart">A value indicating whether the request is multipart.</param>
    /// <returns>The multipart boundary text, or an empty string for non-multipart requests.</returns>
    private static string GetMultipartBoundary(MethodInfo methodInfo, bool isMultipart) =>
        isMultipart
            ? methodInfo.GetCustomAttribute<MultipartAttribute>(true)?.BoundaryText
              ?? new MultipartAttribute().BoundaryText
            : string.Empty;

    /// <summary>Determines whether the result type is one of the supported API response wrappers.</summary>
    /// <param name="returnResultType">The result type wrapped by the method's return type.</param>
    /// <returns><see langword="true"/> when the result type is an API response wrapper; otherwise <see langword="false"/>.</returns>
    private static bool DetermineIsApiResponse(Type returnResultType)
    {
        if (returnResultType == typeof(IApiResponse))
        {
            return true;
        }

        if (!returnResultType.GetTypeInfo().IsGenericType)
        {
            return false;
        }

        var genericDefinition = returnResultType.GetGenericTypeDefinition();
        return genericDefinition == typeof(ApiResponse<>)
               || genericDefinition == typeof(IApiResponse<>);
    }

    /// <summary>Finds the index of the header collection parameter in the parameter array.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <returns>The index of the header collection parameter, or a negative value when none exists.</returns>
    private static int GetHeaderCollectionParameterIndex(ParameterInfo[] parameterArray)
    {
        var headerIndex = -1;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var param = parameterArray[i];
            var headerCollection = param.GetCustomAttribute<HeaderCollectionAttribute>(true);

            if (headerCollection is null)
            {
                continue;
            }

            // Opted for IDictionary<string, string> semantics here as opposed to the looser
            // IEnumerable<KeyValuePair<string, string>> because IDictionary enforces unique keys.
            if (!param.ParameterType.IsAssignableFrom(typeof(IDictionary<string, string>)))
            {
                throw new ArgumentException(
                    $"HeaderCollection parameter of type {param.ParameterType.Name} is not assignable from IDictionary<string, string>");
            }

            // Throw if there is already a HeaderCollection parameter.
            if (headerIndex >= 0)
            {
                throw new ArgumentException("Only one parameter can be a HeaderCollection parameter");
            }

            headerIndex = i;
        }

        return headerIndex;
    }

    /// <summary>Builds the map of parameter indexes to request property keys.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <returns>A map of parameter indexes to property keys.</returns>
    private static Dictionary<int, string> BuildRequestPropertyMap(ParameterInfo[] parameterArray)
    {
        Dictionary<int, string>? propertyMap = null;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var param = parameterArray[i];
            var propertyAttribute = param.GetCustomAttribute<PropertyAttribute>(true);

            if (propertyAttribute is not null)
            {
                var propertyKey = !string.IsNullOrEmpty(propertyAttribute.Key)
                    ? propertyAttribute.Key
                    : param.Name!;
                propertyMap ??= [];
                propertyMap[i] = propertyKey!;
            }
        }

        return propertyMap ?? EmptyDictionary<int, string>.Get();
    }

    /// <summary>Gets the readable public instance properties of a parameter type.</summary>
    /// <param name="parameter">The parameter whose properties are enumerated.</param>
    /// <returns>The readable public instance properties.</returns>
    [RequiresUnreferencedCode("Reading request object properties requires public property metadata to be available at runtime.")]
    private static PropertyInfo[] GetParameterProperties(ParameterInfo parameter) =>
        ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(parameter.ParameterType);

    /// <summary>Verifies that the relative URL path is well formed and free of injection characters.</summary>
    /// <param name="relativePath">The relative URL path to validate.</param>
    /// <param name="urlResolution">The URL resolution mode; the leading-slash requirement is relaxed under <see cref="UrlResolutionMode.Rfc3986"/>.</param>
    private static void VerifyUrlPathIsSane(string relativePath, UrlResolutionMode urlResolution)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return;
        }

        if (urlResolution == UrlResolutionMode.RefitLegacy
            && !relativePath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"URL path {relativePath} must start with '/' and be of the form '/foo/bar/baz'");
        }

        // CRLF injection protection
        if (!StringHelpers.ContainsCrOrLf(relativePath))
        {
            return;
        }

        throw new ArgumentException(
            $"URL path {relativePath} must not contain CR or LF characters");
    }

    /// <summary>Builds the route parameter map and the ordered URL fragments for the relative path.</summary>
    /// <param name="relativePath">The relative URL path template.</param>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="allowUnmatchedRouteParameters">When true, a placeholder with no matching argument is left in the path verbatim instead of throwing.</param>
    /// <returns>A tuple containing the parameter map and the ordered list of URL fragments.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S3776:Cognitive Complexity of methods should not be too high",
        Justification = "Request route parsing has several validation branches that are clearer kept together.")]
    [SuppressMessage(
        "Major Code Smell",
        "S1541:Methods and properties should not be too complex",
        Justification = "Request route parsing has several validation branches that are clearer kept together.")]
    [RequiresUnreferencedCode("Binding route parameters from request object properties requires public property metadata to be available at runtime.")]
    private static (Dictionary<int, RestMethodParameterInfo> Map, List<ParameterFragment> Fragments)
        BuildParameterMap(
            string relativePath,
            ParameterInfo[] parameterInfo,
            bool allowUnmatchedRouteParameters)
    {
        var ret = new Dictionary<int, RestMethodParameterInfo>();

        var parameterizedParts = ParameterRegex().Matches(relativePath);

        if (parameterizedParts.Count == 0)
        {
            return string.IsNullOrEmpty(relativePath)
                ? (ret, [])
                : (ret, [ParameterFragment.Constant(relativePath)]);
        }

        var paramValidationDict = new Dictionary<string, ParameterInfo>(parameterInfo.Length);
        for (var i = 0; i < parameterInfo.Length; i++)
        {
            paramValidationDict[GetUrlNameForParameter(parameterInfo[i]).ToLowerInvariant()] = parameterInfo[i];
        }

        // If the parameter is a class, build a dictionary for all of its potential bound properties.
        var objectParamValidationDict = new Dictionary<string, Tuple<ParameterInfo, PropertyInfo>>();
        for (var i = 0; i < parameterInfo.Length; i++)
        {
            var parameter = parameterInfo[i];
            if (!parameter.ParameterType.GetTypeInfo().IsClass)
            {
                continue;
            }

            var properties = GetParameterProperties(parameter);
            for (var j = 0; j < properties.Length; j++)
            {
                var key = $"{parameter.Name}.{GetUrlNameForProperty(properties[j])}".ToLowerInvariant();
                if (!objectParamValidationDict.ContainsKey(key))
                {
                    objectParamValidationDict.Add(key, Tuple.Create(parameter, properties[j]));
                }
            }
        }

        var fragmentList = new List<ParameterFragment>();
        var index = 0;

        for (var i = 0; i < parameterizedParts.Count; i++)
        {
            var match = parameterizedParts[i];

            // Add constant value from given http path
            if (match.Index != index)
            {
                fragmentList.Add(ParameterFragment.Constant(relativePath.Substring(index, match.Index - index)));
            }

            index = match.Index + match.Length;

            var rawName = match.Groups[1].Value.ToLowerInvariant();
            var isRoundTripping = rawName.StartsWith("**", StringComparison.Ordinal);
            var name = isRoundTripping ? rawName[2..] : rawName;

            if (paramValidationDict.TryGetValue(name, out var value))
            {
                AddStandardParameter(
                    relativePath,
                    parameterInfo,
                    ret,
                    fragmentList,
                    new(rawName, name, isRoundTripping),
                    value);
            }
            else if (objectParamValidationDict.TryGetValue(name, out var value1) && !isRoundTripping)
            {
                AddObjectPropertyParameter(parameterInfo, ret, fragmentList, name, value1);
            }
            else if (allowUnmatchedRouteParameters)
            {
                // Leave the unmatched placeholder in the URL verbatim (including its braces) so the
                // caller can resolve it later, e.g. inside a DelegatingHandler.
                fragmentList.Add(ParameterFragment.Constant(match.Value));
            }
            else
            {
                throw new ArgumentException(
                    $"URL {relativePath} has parameter {rawName}, but no method parameter matches");
            }
        }

        if (index >= relativePath.Length)
        {
            return (ret, fragmentList);
        }

        // Add trailing string.
        var trailingConstant = relativePath[index..];
        fragmentList.Add(ParameterFragment.Constant(trailingConstant));

        return (ret, fragmentList);
    }

    /// <summary>Adds a standard (directly matched) route parameter to the parameter map and fragment list.</summary>
    /// <param name="relativePath">The relative URL path template.</param>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="parsedName">The parsed parameter name details from the URL template.</param>
    /// <param name="value">The matched method parameter.</param>
    private static void AddStandardParameter(
        string relativePath,
        ParameterInfo[] parameterInfo,
        Dictionary<int, RestMethodParameterInfo> ret,
        List<ParameterFragment> fragmentList,
        ParsedParameterName parsedName,
        ParameterInfo value)
    {
        var paramType = value.ParameterType;
        if (parsedName.IsRoundTripping && paramType != typeof(string))
        {
            throw new ArgumentException(
                $"URL {relativePath} has round-tripping parameter {parsedName.RawName}, but the type of matched method parameter is {paramType.FullName}. It must be a string.");
        }

        var parameterType = parsedName.IsRoundTripping
            ? ParameterType.RoundTripping
            : ParameterType.Normal;
        var restMethodParameterInfo = new RestMethodParameterInfo(parsedName.Name, value) { Type = parameterType };

        var parameterIndex = Array.IndexOf(parameterInfo, restMethodParameterInfo.ParameterInfo);
        fragmentList.Add(ParameterFragment.Dynamic(parameterIndex));
#if NET6_0_OR_GREATER
        _ = ret.TryAdd(parameterIndex, restMethodParameterInfo);
#else
        if (ret.ContainsKey(parameterIndex))
        {
            return;
        }

        ret.Add(parameterIndex, restMethodParameterInfo);
#endif
    }

    /// <summary>Adds an object-property route parameter to the parameter map and fragment list.</summary>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="name">The normalized parameter name.</param>
    /// <param name="property">The matched parameter and property pair.</param>
    private static void AddObjectPropertyParameter(
        ParameterInfo[] parameterInfo,
        Dictionary<int, RestMethodParameterInfo> ret,
        List<ParameterFragment> fragmentList,
        string name,
        Tuple<ParameterInfo, PropertyInfo> property)
    {
        var parameterIndex = Array.IndexOf(parameterInfo, property.Item1);

        // If we already have this parameter, add an additional ParameterProperty.
        if (ret.TryGetValue(parameterIndex, out var value2))
        {
            if (!value2.IsObjectPropertyParameter)
            {
                throw new ArgumentException(
                    $"Parameter {property.Item1.Name} matches both a parameter and nested parameter on a parameter object");
            }

            value2.ParameterProperties.Add(new(name, property.Item2));
            fragmentList.Add(
                ParameterFragment.DynamicObject(parameterIndex, value2.ParameterProperties.Count - 1));
            return;
        }

        var restMethodParameterInfo = new RestMethodParameterInfo(true, property.Item1);
        restMethodParameterInfo.ParameterProperties.Add(new(name, property.Item2));

        var idx = Array.IndexOf(parameterInfo, restMethodParameterInfo.ParameterInfo);
        fragmentList.Add(ParameterFragment.DynamicObject(idx, 0));
#if NET6_0_OR_GREATER
        _ = ret.TryAdd(idx, restMethodParameterInfo);
#else
        if (ret.ContainsKey(idx))
        {
            return;
        }

        ret.Add(idx, restMethodParameterInfo);
#endif
    }

    /// <summary>Gets the URL name to use for a parameter, honoring any alias attribute.</summary>
    /// <param name="paramInfo">The parameter whose URL name is resolved.</param>
    /// <returns>The aliased or declared parameter name.</returns>
    private static string GetUrlNameForParameter(ParameterInfo paramInfo)
    {
        var aliasAttr = paramInfo.GetCustomAttribute<AliasAsAttribute>(true);
        return aliasAttr is not null ? aliasAttr.Name : paramInfo.Name!;
    }

    /// <summary>Gets the URL name to use for a property, honoring any alias attribute.</summary>
    /// <param name="propInfo">The property whose URL name is resolved.</param>
    /// <returns>The aliased or declared property name.</returns>
    private static string GetUrlNameForProperty(PropertyInfo propInfo)
    {
        var aliasAttr = propInfo.GetCustomAttribute<AliasAsAttribute>(true);
        return aliasAttr is not null ? aliasAttr.Name : propInfo.Name;
    }

    /// <summary>Gets the multipart attachment name to use for a parameter.</summary>
    /// <param name="paramInfo">The parameter whose attachment name is resolved.</param>
    /// <returns>The attachment name, or null when none is specified.</returns>
    private static string GetAttachmentNameForParameter(ParameterInfo paramInfo)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var nameAttr = paramInfo.GetCustomAttribute<AttachmentNameAttribute>(true);
#pragma warning restore CS0618 // Type or member is obsolete

        // also check for AliasAs
        return nameAttr?.Name ?? paramInfo.GetCustomAttribute<AliasAsAttribute>(true)?.Name!;
    }

    /// <summary>Finds the parameter that carries the authorization value.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <returns>The authorization parameter information, or null when there is no authorize parameter.</returns>
    private static Tuple<string, int>? FindAuthorizationParameter(ParameterInfo[] parameterArray)
    {
        AuthorizeAttribute? authorizeAttribute = null;
        var authorizeIndex = -1;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var attribute = parameterArray[i].GetCustomAttribute<AuthorizeAttribute>(true);
            if (attribute is null)
            {
                continue;
            }

            if (authorizeAttribute is not null)
            {
                throw new ArgumentException("Only one parameter can be an Authorize parameter");
            }

            authorizeAttribute = attribute;
            authorizeIndex = i;
        }

        return authorizeAttribute is null
            ? null
            : Tuple.Create(authorizeAttribute.Scheme, authorizeIndex);
    }

    /// <summary>Finds the single cancellation token parameter for the method.</summary>
    /// <param name="methodInfo">The reflected method information.</param>
    /// <returns>The cancellation token parameter, or null when none is present.</returns>
    private static ParameterInfo? FindCancellationTokenParameter(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        ParameterInfo? cancellationTokenParam = null;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsCancellationTokenParameter(parameters[i]))
            {
                continue;
            }

            if (cancellationTokenParam is not null)
            {
                throw new ArgumentException(
                    $"Argument list to method \"{methodInfo.Name}\" can only contain a single CancellationToken");
            }

            cancellationTokenParam = parameters[i];
        }

        return cancellationTokenParam;
    }

    /// <summary>Adds headers from a <see cref="HeadersAttribute"/> into the accumulated map.</summary>
    /// <param name="headersAttribute">The header attribute to process.</param>
    /// <param name="ret">The accumulated map, created as needed.</param>
    private static void AddHeaders(HeadersAttribute headersAttribute, ref Dictionary<string, string?>? ret)
    {
        var headers = headersAttribute.Headers;
        for (var i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            ret ??= [];
            var colonIndex = header.IndexOf(':');
            if (colonIndex < 0)
            {
                ret[header.Trim()] = null;
                continue;
            }

            ret[header[..colonIndex].Trim()] = header[(colonIndex + 1)..].Trim();
        }
    }

    /// <summary>Parses the static headers declared on the method and its declaring type.</summary>
    /// <param name="targetInterface">The interface type that declares the method.</param>
    /// <param name="methodInfo">The reflected method information.</param>
    /// <returns>A map of header names to header values.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S3776:Cognitive Complexity of methods should not be too high",
        Justification = "Header precedence is order-sensitive and easier to audit in one method.")]
    private static Dictionary<string, string?> ParseHeaders(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type targetInterface,
        MethodInfo methodInfo)
    {
        Dictionary<string, string?>? ret = null;

        if (targetInterface is not null)
        {
            var interfaces = targetInterface.GetInterfaces();
            for (var i = interfaces.Length - 1; i >= 0; i--)
            {
                var attributes = interfaces[i].GetTypeInfo().GetCustomAttributes(true);
                for (var j = attributes.Length - 1; j >= 0; j--)
                {
                    if (attributes[j] is HeadersAttribute headersAttribute)
                    {
                        AddHeaders(headersAttribute, ref ret);
                    }
                }
            }

            var declaringAttributes = targetInterface.GetTypeInfo().GetCustomAttributes(true);
            for (var i = 0; i < declaringAttributes.Length; i++)
            {
                if (declaringAttributes[i] is HeadersAttribute headersAttribute)
                {
                    AddHeaders(headersAttribute, ref ret);
                }
            }
        }

        var methodAttributes = methodInfo.GetCustomAttributes(true);
        for (var i = 0; i < methodAttributes.Length; i++)
        {
            if (methodAttributes[i] is HeadersAttribute headersAttribute)
            {
                AddHeaders(headersAttribute, ref ret);
            }
        }

        return ret ?? EmptyDictionary<string, string?>.Get();
    }

    /// <summary>Builds the map of parameter indexes to header names.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <returns>A map of parameter indexes to header names.</returns>
    private static Dictionary<int, string> BuildHeaderParameterMap(ParameterInfo[] parameterArray)
    {
        Dictionary<int, string>? ret = null;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var header = parameterArray[i].GetCustomAttribute<HeaderAttribute>(true)?.Header;

            if (!string.IsNullOrWhiteSpace(header))
            {
                ret ??= [];
                ret[i] = header!.Trim();
            }
        }

        return ret ?? EmptyDictionary<int, string>.Get();
    }

    /// <summary>Determines the return type, result type, and deserialized result type for the method.</summary>
    /// <param name="methodInfo">The reflected method information.</param>
    /// <returns>A tuple of the return type, result type, and deserialized result type.</returns>
    private static (Type ReturnType, Type ReturnResultType, Type DeserializedResultType) DetermineReturnTypeInfo(
        MethodInfo methodInfo)
    {
        var returnType = methodInfo.ReturnType;
        if (
            returnType.IsGenericType
            && (
                returnType.GetGenericTypeDefinition() == typeof(Task<>)
                || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)
                || returnType.GetGenericTypeDefinition() == typeof(IObservable<>)
                || returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)
            )
        )
        {
            var resultType = returnType.GetGenericArguments()[0];
            return (returnType, resultType, DetermineDeserializedResultType(resultType));
        }

        if (returnType == typeof(Task))
        {
            return (returnType, typeof(void), typeof(void));
        }

        // Allow synchronous return types only for methods that are implemented by generated stubs
        // (for example explicit/default interface implementations). Public top-level Refit methods must
        // still use async-compatible return shapes.
#if NET8_0_OR_GREATER        
        var isExplicitInterfaceMember = methodInfo.Name.Contains('.');
#else
        var isExplicitInterfaceMember = methodInfo.Name.Contains(".");
#endif

        var isNonPublic = !methodInfo.IsPublic;

        if (!isExplicitInterfaceMember && !isNonPublic)
        {
            throw new ArgumentException(
                $"Method \"{methodInfo.Name}\" is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>");
        }

        return (returnType, returnType, DetermineDeserializedResultType(returnType));
    }

    /// <summary>Determines the type that response content is deserialized into for the given result type.</summary>
    /// <param name="returnResultType">The result type wrapped by the return type.</param>
    /// <returns>The type to deserialize response content into.</returns>
    private static Type DetermineDeserializedResultType(Type returnResultType)
    {
        if (
            returnResultType.IsGenericType
            && (
                returnResultType.GetGenericTypeDefinition() == typeof(ApiResponse<>)
                || returnResultType.GetGenericTypeDefinition() == typeof(IApiResponse<>)
            )
        )
        {
            return returnResultType.GetGenericArguments()[0];
        }

        return returnResultType == typeof(IApiResponse)
            ? typeof(HttpContent)
            : returnResultType;
    }

    /// <summary>Determines whether the response must be disposed based on the deserialized result type.</summary>
    /// <param name="deserializedResultType">The type the response content is deserialized into.</param>
    /// <returns><see langword="true"/> when the caller must dispose the response; otherwise <see langword="false"/>.</returns>
    private static bool DetermineIfResponseMustBeDisposed(Type deserializedResultType) =>

        // Rest method caller will have to dispose if it's one of those 3
        deserializedResultType != typeof(HttpResponseMessage)
        && deserializedResultType != typeof(HttpContent)
        && deserializedResultType != typeof(Stream);

#if NET7_0_OR_GREATER
    /// <summary>Gets the compiled regular expression that matches URL path parameters.</summary>
    /// <returns>The parameter matching regular expression.</returns>
    [GeneratedRegex("{(([^/?\\r\\n])*?)}")]
    private static partial Regex ParameterRegex();
#else
    /// <summary>Gets the compiled regular expression that matches URL path parameters.</summary>
    /// <returns>The parameter matching regular expression.</returns>
    private static Regex ParameterRegex() => _parameterRegexValue;
#endif

    /// <summary>Builds the map of parameter indexes to multipart attachment names.</summary>
    /// <returns>A map of parameter indexes to attachment name pairs.</returns>
    private Dictionary<int, Tuple<string, string>> BuildAttachmentNameMap()
    {
        if (!IsMultipart)
        {
            return EmptyDictionary<int, Tuple<string, string>>.Get();
        }

        Dictionary<int, Tuple<string, string>>? attachmentDict = null;
        for (var i = 0; i < ParameterInfoArray.Length; i++)
        {
            if (ParameterMap.ContainsKey(i)
                || HeaderParameterMap.ContainsKey(i)
                || PropertyParameterMap.ContainsKey(i)
                || HeaderCollectionAt(i))
            {
                continue;
            }

            var attachmentName = GetAttachmentNameForParameter(ParameterInfoArray[i]);
            if (attachmentName is null)
            {
                continue;
            }

            attachmentDict ??= [];
            attachmentDict[i] = Tuple.Create(
                attachmentName,
                GetUrlNameForParameter(ParameterInfoArray[i]));
        }

        return attachmentDict ?? EmptyDictionary<int, Tuple<string, string>>.Get();
    }

    /// <summary>Builds the map of parameter indexes to query string names.</summary>
    /// <returns>A map of parameter indexes to query string names.</returns>
    private Dictionary<int, string> BuildQueryParameterMap()
    {
        Dictionary<int, string>? queryDict = null;
        for (var i = 0; i < ParameterInfoArray.Length; i++)
        {
            if (IsExcludedFromQueryMap(i))
            {
                continue;
            }

            queryDict ??= [];
            queryDict.Add(i, GetUrlNameForParameter(ParameterInfoArray[i]));
        }

        return queryDict ?? EmptyDictionary<int, string>.Get();
    }

    /// <summary>Determines whether the parameter at the given index should be excluded from the query map.</summary>
    /// <param name="index">The parameter index to test.</param>
    /// <returns><see langword="true"/> when the parameter is not a query parameter; otherwise <see langword="false"/>.</returns>
    private bool IsExcludedFromQueryMap(int index) =>
        ParameterMap.ContainsKey(index)
        || HeaderParameterMap.ContainsKey(index)

        // A parameter can carry both [Property] and [Query]; only exclude it from the
        // query map when it is a property AND does not also opt into the query string.
        || (PropertyParameterMap.ContainsKey(index)
            && ParameterInfoArray[index].GetCustomAttribute<QueryAttribute>() is null)
        || HeaderCollectionAt(index)
        || (BodyParameterInfo is not null && BodyParameterInfo.Item3 == index)
        || (AuthorizeParameterInfo is not null && AuthorizeParameterInfo.Item2 == index);

    /// <summary>Finds the parameter that carries the request body.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <param name="isMultipart">A value indicating whether the request is multipart.</param>
    /// <param name="method">The HTTP method of the request.</param>
    /// <returns>The body parameter information, or null when there is no body parameter.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S1541:Methods and properties should not be too complex",
        Justification = "Body parameter inference mirrors the documented precedence rules.")]
    private Tuple<BodySerializationMethod, bool, int>? FindBodyParameter(
        ParameterInfo[] parameterArray,
        bool isMultipart,
        HttpMethod method)
    {
        // The body parameter is found using the following logic / order of precedence:
        // 1) [Body] attribute
        // 2) POST/PUT/PATCH: Reference type other than string
        // 3) If there are two reference types other than string, without the body attribute, throw
        BodyAttribute? bodyAttribute = null;
        var bodyParameterIndex = -1;
        var hasMultipleBodyParameters = false;
        for (var i = 0; i < parameterArray.Length; i++)
        {
            var attribute = parameterArray[i].GetCustomAttribute<BodyAttribute>(true);
            if (attribute is null)
            {
                continue;
            }

            if (bodyAttribute is not null)
            {
                hasMultipleBodyParameters = true;
                break;
            }

            bodyAttribute = attribute;
            bodyParameterIndex = i;
        }

        // multipart requests may not contain a body, implicit or explicit
        if (isMultipart)
        {
            if (bodyAttribute is not null)
            {
                throw new ArgumentException(
                    "Multipart requests may not contain a Body parameter");
            }

            return null;
        }

        if (hasMultipleBodyParameters)
        {
            throw new ArgumentException("Only one parameter can be a Body parameter");
        }

        // #1, body attribute wins
        if (bodyAttribute is not null)
        {
            return Tuple.Create(
                bodyAttribute.SerializationMethod,
                bodyAttribute.Buffered ?? RefitSettings.Buffered,
                bodyParameterIndex);
        }

        // Only post/put/patch carry an implicit body.
        return method.Equals(HttpMethod.Post)
            || method.Equals(HttpMethod.Put)
            || method.Equals(_patchMethod)
            ? FindImplicitBodyParameter(parameterArray)
            : null;
    }

    /// <summary>Finds an implicit body parameter for POST/PUT/PATCH requests.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <returns>The body parameter information, or null when there is no implicit body parameter.</returns>
    private Tuple<BodySerializationMethod, bool, int>? FindImplicitBodyParameter(ParameterInfo[] parameterArray)
    {
        // see if we're a post/put/patch
        // explicitly skip [Query], [HeaderCollection], and [Property]-denoted params
        ParameterInfo? refParam = null;
        var refParamIndex = -1;
        for (var i = 0; i < parameterArray.Length; i++)
        {
            var parameter = parameterArray[i];
            if (parameter.ParameterType.GetTypeInfo().IsValueType
                || parameter.ParameterType == typeof(string)
                || parameter.GetCustomAttribute<QueryAttribute>() is not null
                || parameter.GetCustomAttribute<HeaderCollectionAttribute>() is not null
                || parameter.GetCustomAttribute<PropertyAttribute>() is not null)
            {
                continue;
            }

            if (refParam is not null)
            {
                throw new ArgumentException(
                    "Multiple complex types found. Specify one parameter as the body using BodyAttribute");
            }

            refParam = parameter;
            refParamIndex = i;
        }

        return refParam is null
            ? null
            : Tuple.Create(
                BodySerializationMethod.Serialized,
                RefitSettings.Buffered,
                refParamIndex);
    }

    /// <summary>Holds the parsed forms of a route parameter name extracted from a URL template.</summary>
    /// <param name="RawName">The raw parameter name from the URL template.</param>
    /// <param name="Name">The normalized parameter name with any round-tripping prefix removed.</param>
    /// <param name="IsRoundTripping">A value indicating whether the parameter is round-tripping.</param>
    private readonly record struct ParsedParameterName(string RawName, string Name, bool IsRoundTripping);
}
