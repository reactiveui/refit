// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
internal partial class RequestBuilderImplementation
{
    /// <summary>Cached reflection handle to the generic body-serialization method.</summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break when trimming",
        Justification = "The cached method handle points to a local generic serialization helper used through runtime generic method closure.")]
    private static readonly MethodInfo SerializeBodyMethod =
        FindDeclaredMethod(nameof(SerializeBodyGeneric));

    /// <summary>Cached reflection handle to the generic synchronous body-serialization method.</summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break when trimming",
        Justification = "The cached method handle points to a local generic serialization helper used through runtime generic method closure.")]
    private static readonly MethodInfo SerializeBodySynchronouslyMethod =
        FindDeclaredMethod(nameof(SerializeBodySynchronouslyGeneric));

    /// <summary>Cached reflection handle to the generic streaming body-serialization method.</summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break when trimming",
        Justification = "The cached method handle points to a local generic serialization helper used through runtime generic method closure.")]
    private static readonly MethodInfo SerializeBodyStreamingMethod =
        FindDeclaredMethod(nameof(SerializeBodyStreamingGeneric));

    /// <summary>Maps a single header, header-collection or authorization parameter into the pending headers.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="i">The index of the parameter.</param>
    /// <param name="param">The argument value.</param>
    /// <param name="headersToAdd">The pending header collection, created as needed.</param>
    /// <returns><see langword="true"/> when the parameter contributed a header.</returns>
    private static bool MapHeaderParameters(
        RestMethodInfoInternal restMethod,
        int i,
        object? param,
        ref Dictionary<string, string?>? headersToAdd)
    {
        var isMapped = false;

        if (restMethod.HeaderParameterMap.TryGetValue(i, out var headerParameterValue))
        {
            headersToAdd ??= [];
            headersToAdd[headerParameterValue] = param?.ToString();
            isMapped = true;
        }

        if (restMethod.HeaderCollectionAt(i))
        {
            AddHeaderCollection(param, ref headersToAdd);
            isMapped = true;
        }

        if (restMethod.AuthorizeParameterInfo?.Item2 == i)
        {
            headersToAdd ??= [];
            headersToAdd["Authorization"] =
                $"{restMethod.AuthorizeParameterInfo.Item1} {param}";
            isMapped = true;
        }

        return isMapped;
    }

    /// <summary>Adds each entry of a header-collection argument to the pending header dictionary.</summary>
    /// <param name="param">The header-collection argument value.</param>
    /// <param name="headersToAdd">The pending header collection, created as needed.</param>
    private static void AddHeaderCollection(object? param, ref Dictionary<string, string?>? headersToAdd)
    {
        if (param is not IDictionary<string, string> headerCollection)
        {
            return;
        }

        headersToAdd ??= [];
        foreach (var header in headerCollection)
        {
            headersToAdd[header.Key] = header.Value;
        }
    }

    /// <summary>Determines whether the parameter is a property-bound parameter without a [Query] attribute.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="i">The index of the parameter.</param>
    /// <returns><see langword="true"/> when the parameter is property-only and should not also feed the query string.</returns>
    private static bool IsPropertyOnlyParameter(RestMethodInfoInternal restMethod, int i) =>
        restMethod.PropertyParameterMap.ContainsKey(i)
        && restMethod.ParameterInfoArray[i].GetCustomAttribute<QueryAttribute>() is null;

    /// <summary>Reduces a round-tripping parameter value to its string form, using <c>ToString</c> for non-strings.</summary>
    /// <param name="param">The parameter value.</param>
    /// <returns>The value's string form, or null.</returns>
    private static string? RoundTripStringValue(object? param) => (param as string) ?? param?.ToString();

    /// <summary>Serializes a request body using the declared body type.</summary>
    /// <param name="serializer">The content serializer to use.</param>
    /// <param name="body">The body value to serialize.</param>
    /// <param name="declaredBodyType">The declared (static) type of the body.</param>
    /// <returns>The serialized HTTP content.</returns>
    [UnconditionalSuppressMessage(
        "AOT",
        "IL2060:MakeGenericMethod",
        Justification = "The reflection request builder intentionally closes the serializer method over the runtime body type.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private static HttpContent SerializeBody(
        IHttpContentSerializer serializer,
        object? body,
        Type declaredBodyType)
    {
        var serializeMethod = SerializeBodyMethod.MakeGenericMethod(declaredBodyType);
        return (HttpContent)serializeMethod.Invoke(null, [serializer, body])!;
    }

    /// <summary>Serializes a request body as the given type.</summary>
    /// <typeparam name="T">The type to serialize the body as.</typeparam>
    /// <param name="serializer">The content serializer to use.</param>
    /// <param name="body">The body value to serialize.</param>
    /// <returns>The serialized HTTP content.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private static HttpContent SerializeBodyGeneric<T>(IHttpContentSerializer serializer, object? body) =>
        serializer.ToHttpContent((T)body!);

    /// <summary>Serializes a request body synchronously as the given type.</summary>
    /// <param name="serializer">The synchronous content serializer to use.</param>
    /// <param name="body">The body value to serialize.</param>
    /// <param name="declaredBodyType">The declared body type to serialize as.</param>
    /// <returns>The serialized, buffered HTTP content.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2060:MakeGenericMethod",
        Justification = "The reflection request builder intentionally closes the serializer method over the runtime body type.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private static HttpContent SerializeBodySynchronously(
        ISynchronousContentSerializer serializer,
        object? body,
        Type declaredBodyType)
    {
        var serializeMethod = SerializeBodySynchronouslyMethod.MakeGenericMethod(declaredBodyType);
        return (HttpContent)serializeMethod.Invoke(null, [serializer, body])!;
    }

    /// <summary>Serializes a request body synchronously as the given type.</summary>
    /// <typeparam name="T">The type to serialize the body as.</typeparam>
    /// <param name="serializer">The synchronous content serializer to use.</param>
    /// <param name="body">The body value to serialize.</param>
    /// <returns>The serialized, buffered HTTP content.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private static HttpContent SerializeBodySynchronouslyGeneric<T>(ISynchronousContentSerializer serializer, object? body) =>
        serializer.ToHttpContentSynchronous((T)body!);

    /// <summary>Serializes a request body as a streaming content for the given type.</summary>
    /// <param name="serializer">The synchronous content serializer to use.</param>
    /// <param name="body">The body value to serialize.</param>
    /// <param name="declaredBodyType">The declared body type to serialize as.</param>
    /// <returns>The streaming HTTP content.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2060:MakeGenericMethod",
        Justification = "The reflection request builder intentionally closes the serializer method over the runtime body type.")]
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private static HttpContent SerializeBodyStreaming(
        ISynchronousContentSerializer serializer,
        object? body,
        Type declaredBodyType)
    {
        var serializeMethod = SerializeBodyStreamingMethod.MakeGenericMethod(declaredBodyType);
        return (HttpContent)serializeMethod.Invoke(null, [serializer, body])!;
    }

    /// <summary>Serializes a request body as a streaming content for the given type.</summary>
    /// <typeparam name="T">The type to serialize the body as.</typeparam>
    /// <param name="serializer">The synchronous content serializer to use.</param>
    /// <param name="body">The body value to serialize.</param>
    /// <returns>The streaming HTTP content.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    private static HttpContent SerializeBodyStreamingGeneric<T>(ISynchronousContentSerializer serializer, object? body) =>
        serializer.ToStreamingHttpContent((T)body!);

    /// <summary>Coerces a JSON Lines body argument into the sequence of values to serialize line by line.</summary>
    /// <param name="param">The body argument value.</param>
    /// <returns>The sequence of values; a non-enumerable value becomes a single line.</returns>
    private static IEnumerable AsJsonLinesSequence(object param) =>
        param is IEnumerable enumerable and not string
            ? enumerable
            : new[] { param };

    /// <summary>Returns a copy of an argument array with cancellation tokens removed.</summary>
    /// <param name="paramList">The original argument values.</param>
    /// <returns>The argument values used for request mapping.</returns>
    private static object[] RemoveCancellationTokens(object[] paramList)
    {
        var count = 0;
        for (var i = 0; i < paramList.Length; i++)
        {
            if (paramList[i] is not CancellationToken)
            {
                count++;
            }
        }

        if (count == paramList.Length)
        {
            return paramList;
        }

        var mappedParams = new object[count];
        var index = 0;
        for (var i = 0; i < paramList.Length; i++)
        {
            if (paramList[i] is not CancellationToken)
            {
                mappedParams[index] = paramList[i];
                index++;
            }
        }

        return mappedParams;
    }

    /// <summary>Builds the full request message for a method invocation, including body, headers and query.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="basePath">The base path from the client's base address.</param>
    /// <param name="paramsContainsCancellationToken">Whether the argument list contains a cancellation token.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <returns>The constructed request message.</returns>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private async Task<HttpRequestMessage> BuildRequestMessageForMethodAsync(
        RestMethodInfoInternal restMethod,
        string basePath,
        bool paramsContainsCancellationToken,
        object[] paramList)
    {
        var cancellationToken = CancellationToken.None;

        // Retain the full declared-order arguments (including any CancellationToken) so a captured MethodArguments
        // option aligns 1:1 with MethodInfo.GetParameters(); request mapping below uses the token-stripped copy.
        var declaredArguments = paramList;

        if (paramsContainsCancellationToken)
        {
            cancellationToken = GetCancellationToken(paramList);
            paramList = RemoveCancellationTokens(paramList);
        }

        var ret = new HttpRequestMessage { Method = restMethod.HttpMethod };
        try
        {
            MultipartFormDataContent? multiPartContent = null;
            if (restMethod.IsMultipart)
            {
                multiPartContent = new(restMethod.MultipartBoundary);
                ret.Content = multiPartContent;
            }

            List<QueryParameterEntry>? queryParamsToAdd = null;
            var headersToAdd = restMethod.Headers.Count > 0
                ? new Dictionary<string, string?>(restMethod.Headers)
                : null;

            MapParametersToRequest(restMethod, paramList, ret, multiPartContent, ref headersToAdd, ref queryParamsToAdd);

            AddHeadersToRequest(headersToAdd, ret);
            await AddAuthorizationHeadersFromGetterAsync(ret, cancellationToken)
                .ConfigureAwait(false);

            AddPropertiesToRequest(restMethod, ret, paramList, declaredArguments);
#if NET6_0_OR_GREATER
            AddVersionToRequest(ret);
#endif
            AssignRequestUri(restMethod, ret, basePath, paramList, queryParamsToAdd);
            return ret;
        }
        catch
        {
            ret.Dispose();
            throw;
        }
    }

    /// <summary>Maps each argument to the request body, headers or query, or to a multipart part.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <param name="ret">The request message being populated.</param>
    /// <param name="multiPartContent">The multipart content, when the method is multipart.</param>
    /// <param name="headersToAdd">The pending header collection, created as needed.</param>
    /// <param name="queryParamsToAdd">The pending query parameter collection, created as needed.</param>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private void MapParametersToRequest(
        RestMethodInfoInternal restMethod,
        object[] paramList,
        HttpRequestMessage ret,
        MultipartFormDataContent? multiPartContent,
        ref Dictionary<string, string?>? headersToAdd,
        ref List<QueryParameterEntry>? queryParamsToAdd)
    {
        RestMethodParameterInfo? parameterInfo = null;

        for (var i = 0; i < paramList.Length; i++)
        {
            var param = paramList[i];
            if (restMethod.ParameterMap.TryGetValue(i, out var parameterMapValue))
            {
                parameterInfo = parameterMapValue;
            }

            var isParameterMappedToRequest = MapSingleParameterToRequest(
                restMethod,
                i,
                param,
                ret,
                ref headersToAdd);

            if (isParameterMappedToRequest || param is null)
            {
                continue;
            }

            var queryAttribute = restMethod
                .ParameterInfoArray[i]
                .GetCustomAttribute<QueryAttribute>();
            if (!restMethod.IsMultipart
                || (restMethod.ParameterMap.TryGetValue(i, out var mapValue) && mapValue.IsObjectPropertyParameter)
                || queryAttribute is not null)
            {
                queryParamsToAdd ??= [];
                AddQueryParameters(
                    restMethod,
                    queryAttribute,
                    param,
                    queryParamsToAdd,
                    i,
                    parameterInfo);
                continue;
            }

            AddMultiPart(restMethod, i, param, multiPartContent);
        }
    }

    /// <summary>Maps a single argument to the request body, a header, header collection or authorization header.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="i">The index of the parameter.</param>
    /// <param name="param">The argument value.</param>
    /// <param name="ret">The request message being populated.</param>
    /// <param name="headersToAdd">The pending header collection, created as needed.</param>
    /// <returns><see langword="true"/> when the parameter was fully mapped and needs no further handling.</returns>
    [RequiresDynamicCode("Serializing a body by runtime Type requires runtime generic method instantiation.")]
    private bool MapSingleParameterToRequest(
        RestMethodInfoInternal restMethod,
        int i,
        object? param,
        HttpRequestMessage ret,
        ref Dictionary<string, string?>? headersToAdd)
    {
        var isMapped = restMethod.ParameterMap.TryGetValue(i, out var parameterMapValue)
            && !parameterMapValue.IsObjectPropertyParameter;

        if (restMethod.BodyParameterInfo?.Item3 == i)
        {
            AddBodyToRequest(restMethod, param!, ret);
            isMapped = true;
        }

        if (MapHeaderParameters(restMethod, i, param, ref headersToAdd))
        {
            isMapped = true;
        }

        // A property parameter that also carries [Query] should still contribute to
        // the query string, so do not treat it as fully mapped in that case.
        if (IsPropertyOnlyParameter(restMethod, i))
        {
            isMapped = true;
        }

        return isMapped;
    }

    /// <summary>Builds and assigns the relative request URI, merging built and existing query parameters.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="ret">The request message being populated.</param>
    /// <param name="basePath">The base path from the client's base address.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <param name="queryParamsToAdd">The query parameters collected for the request, if any.</param>
    private void AssignRequestUri(
        RestMethodInfoInternal restMethod,
        HttpRequestMessage ret,
        string basePath,
        object[] paramList,
        List<QueryParameterEntry>? queryParamsToAdd)
    {
        var urlTarget = BuildRelativePath(basePath, restMethod, paramList);

        if (restMethod.RefitSettings.UrlResolution == UrlResolutionMode.Rfc3986)
        {
            AssignRequestUriRfc3986(ret, urlTarget, queryParamsToAdd);
            return;
        }

        var uri = new UriBuilder(new Uri(BaseUri, urlTarget));
        ParseExistingQueryString(uri, ref queryParamsToAdd);

        uri.Query = queryParamsToAdd is not null && queryParamsToAdd.Count != 0
            ? CreateQueryString(queryParamsToAdd)
            : null;

        ret.RequestUri = new(
            uri.Uri.GetComponents(UriComponents.PathAndQuery, restMethod.QueryUriFormat),
            UriKind.Relative);
    }

    /// <summary>Builds the relative request path by expanding the method's path fragments.</summary>
    /// <param name="basePath">The base path to prefix.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values used to resolve dynamic fragments.</param>
    /// <returns>The fully expanded relative path.</returns>
    private string BuildRelativePath(string basePath, RestMethodInfoInternal restMethod, object[] paramList)
    {
        // Under RFC 3986 resolution the HttpClient merges the base address with the relative path itself,
        // so emit only the declared relative path (preserving any leading slash) and prepend nothing.
        // Otherwise every path fragment is prefixed with '/', so trim a trailing slash from the base path
        // to avoid emitting a double slash when the base address ends with one.
        basePath = restMethod.RefitSettings.UrlResolution == UrlResolutionMode.Rfc3986 || basePath == "/"
            ? string.Empty
            : basePath.TrimEnd('/');
        var pathFragments = restMethod.FragmentPath;
        if (pathFragments.Count == 0)
        {
            return basePath;
        }

        if (string.IsNullOrEmpty(basePath) && pathFragments.Count == 1)
        {
            Debug.Assert(pathFragments[0].IsConstant, "A single-fragment path with no base path must be a constant fragment.");
            return pathFragments[0].Value!;
        }

        // ValueStringBuilder disposes itself in ToString(), so CA2000 does not apply.
        var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
        vsb.Append(basePath);

        for (var i = 0; i < pathFragments.Count; i++)
        {
            var fragment = pathFragments[i];
            AppendPathFragmentValue(ref vsb, restMethod, paramList, fragment);
        }

        return vsb.ToString();
    }

    /// <summary>Appends a single resolved path fragment to the path builder.</summary>
    /// <param name="vsb">The path builder to append to.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values used to resolve the fragment.</param>
    /// <param name="fragment">The path fragment to append.</param>
    private void AppendPathFragmentValue(
        ref ValueStringBuilder vsb,
        RestMethodInfoInternal restMethod,
        object[] paramList,
        ParameterFragment fragment)
    {
        if (fragment.IsConstant)
        {
            vsb.Append(fragment.Value!);
            return;
        }

        // Every non-constant fragment carries the index of a parameter that BuildParameterMap registered,
        // so the lookup always succeeds.
        var parameterMapValue = restMethod.ParameterMap[fragment.ArgumentIndex];

        if (fragment.IsObjectProperty)
        {
            AppendObjectPropertyFragment(ref vsb, paramList, fragment, parameterMapValue);
            return;
        }

        // A non-constant fragment always carries an argument index, so anything that is not an
        // object property is a dynamic route value.
        AppendDynamicRouteFragment(ref vsb, restMethod, paramList, fragment, parameterMapValue);
    }

    /// <summary>Appends an object-property-bound path fragment.</summary>
    /// <param name="vsb">The path builder to append to.</param>
    /// <param name="paramList">The argument values used to resolve the fragment.</param>
    /// <param name="fragment">The path fragment to append.</param>
    /// <param name="parameterMapValue">The parameter info for the fragment.</param>
    private void AppendObjectPropertyFragment(
        ref ValueStringBuilder vsb,
        object[] paramList,
        ParameterFragment fragment,
        RestMethodParameterInfo parameterMapValue)
    {
        var property = parameterMapValue.ParameterProperties[fragment.PropertyIndex];

        // Walk the property chain (one link for a single-level binding, more for a dotted {a.b.c} binding), stopping at
        // the first null so a missing intermediate formats as an empty segment rather than throwing.
        object? propertyObject = paramList[fragment.ArgumentIndex];
        foreach (var link in property.PropertyChain)
        {
            if (propertyObject is null)
            {
                break;
            }

            propertyObject = link.GetValue(propertyObject);
        }

        vsb.Append(StringHelpers.EscapeDataString(_settings.UrlParameterFormatter.Format(
            propertyObject,
            property.PropertyInfo,
            property.PropertyInfo.PropertyType) ?? string.Empty));
    }

    /// <summary>Appends a dynamic-route path fragment, round-tripping segments when required.</summary>
    /// <param name="vsb">The path builder to append to.</param>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="paramList">The argument values used to resolve the fragment.</param>
    /// <param name="fragment">The path fragment to append.</param>
    /// <param name="parameterMapValue">The parameter info for the fragment.</param>
    private void AppendDynamicRouteFragment(
        ref ValueStringBuilder vsb,
        RestMethodInfoInternal restMethod,
        object[] paramList,
        ParameterFragment fragment,
        RestMethodParameterInfo parameterMapValue)
    {
        var param = paramList[fragment.ArgumentIndex];
        var parameterInfo = restMethod.ParameterInfoArray[fragment.ArgumentIndex];

        if (parameterMapValue.Type == ParameterType.Normal)
        {
            vsb.Append(StringHelpers.EscapeDataString(
                _settings.UrlParameterFormatter.Format(
                    param,
                    parameterInfo,
                    parameterInfo.ParameterType) ?? string.Empty));
            return;
        }

        // If round tripping, split the value's string form on '/' (ToString for a non-string, so any type is
        // supported) and format+escape each segment independently, preserving the separators.
        Debug.Assert(parameterMapValue.Type == ParameterType.RoundTripping, "Dynamic route fragments must be Normal or RoundTripping.");
        var paramValue = RoundTripStringValue(param);

        if (paramValue is null)
        {
            vsb.Append(
                StringHelpers.EscapeDataString(
                    _settings.UrlParameterFormatter.Format(
                        paramValue,
                        parameterInfo,
                        parameterInfo.ParameterType) ?? string.Empty));
            return;
        }

        var sectionStart = 0;
        for (var i = 0; i <= paramValue.Length; i++)
        {
            if (i != paramValue.Length && paramValue[i] != '/')
            {
                continue;
            }

            if (sectionStart > 0)
            {
                vsb.Append('/');
            }

            var section = paramValue.Substring(sectionStart, i - sectionStart);
            vsb.Append(
                StringHelpers.EscapeDataString(
                    _settings.UrlParameterFormatter.Format(
                        section,
                        parameterInfo,
                        parameterInfo.ParameterType) ?? string.Empty));
            sectionStart = i + 1;
        }
    }
}
