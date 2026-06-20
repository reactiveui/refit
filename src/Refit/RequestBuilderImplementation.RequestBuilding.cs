// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace Refit
{
    /// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
    internal partial class RequestBuilderImplementation
    {
        /// <summary>Cached reflection handle to the generic body-serialization method.</summary>
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' may break when trimming",
            Justification = "The reflective serialization path is only reached from public APIs already annotated with RequiresUnreferencedCode; the source generator is the trim-safe alternative.")]
        private static readonly MethodInfo SerializeBodyMethod =
            FindDeclaredMethod(nameof(SerializeBodyGeneric));

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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private static bool IsPropertyOnlyParameter(RestMethodInfoInternal restMethod, int i) =>
            restMethod.PropertyParameterMap.ContainsKey(i)
            && restMethod.ParameterInfoArray[i].GetCustomAttribute<QueryAttribute>() is null;

        /// <summary>Serializes a request body using the declared body type.</summary>
        /// <param name="serializer">The content serializer to use.</param>
        /// <param name="body">The body value to serialize.</param>
        /// <param name="declaredBodyType">The declared (static) type of the body.</param>
        /// <returns>The serialized HTTP content.</returns>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "Type parameter intentionally specified explicitly by callers.")]
        private static HttpContent SerializeBodyGeneric<T>(IHttpContentSerializer serializer, object? body) =>
            serializer.ToHttpContent((T)body!);

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
                    mappedParams[index++] = paramList[i];
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private async Task<HttpRequestMessage?> BuildRequestMessageForMethodAsync(
            RestMethodInfoInternal restMethod,
            string basePath,
            bool paramsContainsCancellationToken,
            object[] paramList)
        {
            var cancellationToken = CancellationToken.None;

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

                List<KeyValuePair<string, string?>>? queryParamsToAdd = null;
                var headersToAdd = restMethod.Headers.Count > 0
                    ? new Dictionary<string, string?>(restMethod.Headers)
                    : null;

                MapParametersToRequest(restMethod, paramList, ret, multiPartContent, ref headersToAdd, ref queryParamsToAdd);

                AddHeadersToRequest(headersToAdd, ret);
                await AddAuthorizationHeadersFromGetterAsync(ret, cancellationToken)
                    .ConfigureAwait(false);

                AddPropertiesToRequest(restMethod, ret, paramList);
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void MapParametersToRequest(
            RestMethodInfoInternal restMethod,
            object[] paramList,
            HttpRequestMessage ret,
            MultipartFormDataContent? multiPartContent,
            ref Dictionary<string, string?>? headersToAdd,
            ref List<KeyValuePair<string, string?>>? queryParamsToAdd)
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AssignRequestUri(
            RestMethodInfoInternal restMethod,
            HttpRequestMessage ret,
            string basePath,
            object[] paramList,
            List<KeyValuePair<string, string?>>? queryParamsToAdd)
        {
            var urlTarget = BuildRelativePath(basePath, restMethod, paramList);

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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private string BuildRelativePath(string basePath, RestMethodInfoInternal restMethod, object[] paramList)
        {
            // Every path fragment is prefixed with '/', so trim a trailing slash from the
            // base path to avoid emitting a double slash when the base address ends with one.
            basePath = basePath == "/" ? string.Empty : basePath.TrimEnd('/');
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
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

            var contains = restMethod.ParameterMap.TryGetValue(fragment.ArgumentIndex, out var parameterMapValue);
            if (!contains || parameterMapValue is null)
            {
                throw new InvalidOperationException($"{restMethod.ParameterMap} should contain parameter.");
            }

            if (fragment.IsObjectProperty)
            {
                AppendObjectPropertyFragment(ref vsb, paramList, fragment, parameterMapValue);
                return;
            }

            if (fragment.IsDynamicRoute)
            {
                AppendDynamicRouteFragment(ref vsb, restMethod, paramList, fragment, parameterMapValue);
                return;
            }

            throw new ArgumentException($"{nameof(ParameterFragment)} is in an invalid form.");
        }

        /// <summary>Appends an object-property-bound path fragment.</summary>
        /// <param name="vsb">The path builder to append to.</param>
        /// <param name="paramList">The argument values used to resolve the fragment.</param>
        /// <param name="fragment">The path fragment to append.</param>
        /// <param name="parameterMapValue">The parameter info for the fragment.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AppendObjectPropertyFragment(
            ref ValueStringBuilder vsb,
            object[] paramList,
            ParameterFragment fragment,
            RestMethodParameterInfo parameterMapValue)
        {
            var param = paramList[fragment.ArgumentIndex];
            var property = parameterMapValue.ParameterProperties[fragment.PropertyIndex];
            var propertyObject = property.PropertyInfo.GetValue(param);

            vsb.Append(Uri.EscapeDataString(_settings.UrlParameterFormatter.Format(
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
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
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
                vsb.Append(Uri.EscapeDataString(
                    _settings.UrlParameterFormatter.Format(
                        param,
                        parameterInfo,
                        parameterInfo.ParameterType) ?? string.Empty));
                return;
            }

            // If round tripping, format each path segment independently.
            Debug.Assert(parameterMapValue.Type == ParameterType.RoundTripping, "Dynamic route fragments must be Normal or RoundTripping.");
            var paramValue = (string)param;
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
                    Uri.EscapeDataString(
                        _settings.UrlParameterFormatter.Format(
                            section,
                            parameterInfo,
                            parameterInfo.ParameterType) ?? string.Empty));
                sectionStart = i + 1;
            }
        }

        /// <summary>Sets the request content from the body parameter using the configured serialization method.</summary>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="param">The body argument value.</param>
        /// <param name="ret">The request message to populate.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AddBodyToRequest(RestMethodInfoInternal restMethod, object param, HttpRequestMessage ret)
        {
            if (param is HttpContent httpContentParam)
            {
                ret.Content = httpContentParam;
                return;
            }

            if (param is Stream streamParam)
            {
                ret.Content = new StreamContent(streamParam);
                return;
            }

            // Default sends raw strings
            if (restMethod.BodyParameterInfo!.Item1 == BodySerializationMethod.Default
                && param is string stringParam)
            {
                ret.Content = new StringContent(stringParam);
                return;
            }

            switch (restMethod.BodyParameterInfo.Item1)
            {
                case BodySerializationMethod.UrlEncoded:
                {
                    ret.Content = param is string str
                        ? new StringContent(
                            Uri.EscapeDataString(str),
                            Encoding.UTF8,
                            "application/x-www-form-urlencoded")
                        : new FormUrlEncodedContent(new FormValueMultimap(param, _settings));
                    break;
                }

                // BodySerializationMethod.Json is obsolete, but the reflection path must still
                // accept legacy [Body(BodySerializationMethod.Json)] usage from compiled callers.
                // Falling through to Default would incorrectly send string bodies as raw text.
#pragma warning disable CS0618 // Required for legacy BodySerializationMethod.Json compatibility.
                case BodySerializationMethod.Default or BodySerializationMethod.Json or BodySerializationMethod.Serialized:
#pragma warning restore CS0618 // Compatibility switch complete; re-enable obsolete warnings.
                {
                    AddSerializedBodyToRequest(restMethod, param, ret);
                    break;
                }
            }
        }

        /// <summary>Sets the request content from a serialized body, optionally streaming it.</summary>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="param">The body argument value.</param>
        /// <param name="ret">The request message to populate.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AddSerializedBodyToRequest(RestMethodInfoInternal restMethod, object param, HttpRequestMessage ret)
        {
            var declaredBodyType = restMethod.ParameterInfoArray[
                restMethod.BodyParameterInfo!.Item3].ParameterType;
            var content = SerializeBody(_serializer, param, declaredBodyType);

            if (restMethod.BodyParameterInfo.Item2)
            {
                ret.Content = content;
                return;
            }

            ret.Content = new PushStreamContent(
                async (stream, _, _) =>
                {
                    using (stream)
                    {
                        await content
                            .CopyToAsync(stream)
                            .ConfigureAwait(false);
                    }
                },
                content.Headers.ContentType);
        }

        /// <summary>Adds query-string parameters for a single argument to the pending query list.</summary>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="queryAttribute">The query attribute on the parameter, if any.</param>
        /// <param name="param">The argument value.</param>
        /// <param name="queryParamsToAdd">The list of query parameters being built.</param>
        /// <param name="i">The index of the parameter.</param>
        /// <param name="parameterInfo">Optional parameter info for property-bound parameters.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AddQueryParameters(
            RestMethodInfoInternal restMethod,
            QueryAttribute? queryAttribute,
            object param,
            List<KeyValuePair<string, string?>> queryParamsToAdd,
            int i,
            RestMethodParameterInfo? parameterInfo)
        {
            var attr = queryAttribute ?? DefaultQueryAttribute;
            if (attr.TreatAsString)
            {
                AppendQueryParameter(
                    queryParamsToAdd,
                    param.ToString(),
                    restMethod.ParameterInfoArray[i],
                    restMethod.QueryParameterMap[i],
                    attr);
                return;
            }

            if (DoNotConvertToQueryMap(param))
            {
                AppendQueryParameter(
                    queryParamsToAdd,
                    param,
                    restMethod.ParameterInfoArray[i],
                    restMethod.QueryParameterMap[i],
                    attr);
                return;
            }

            var parameterCollectionFormat = attr.IsCollectionFormatSpecified
                ? attr.CollectionFormat
                : (CollectionFormat?)null;
            var queryMap = BuildQueryMap(param, attr.Delimiter, parameterInfo, parameterCollectionFormat);
            for (var queryMapIndex = 0; queryMapIndex < queryMap.Count; queryMapIndex++)
            {
                var kvp = queryMap[queryMapIndex];
                var path = !string.IsNullOrWhiteSpace(attr.Prefix)
                    ? attr.Prefix + attr.Delimiter + kvp.Key
                    : kvp.Key;
                AppendQueryParameter(
                    queryParamsToAdd,
                    kvp.Value,
                    restMethod.ParameterInfoArray[i],
                    path,
                    attr);
            }
        }

        /// <summary>Adds one (or each enumerated) multipart part for a single argument.</summary>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="i">The index of the parameter.</param>
        /// <param name="param">The argument value, which may be a single item or an enumerable.</param>
        /// <param name="multiPartContent">The multipart content to add to.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AddMultiPart(
            RestMethodInfoInternal restMethod,
            int i,
            object param,
            MultipartFormDataContent? multiPartContent)
        {
            // we are in a multipart method, add the part to the content
            // the parameter name should be either the attachment name or the parameter name (as fallback)
            string itemName;
            string parameterName;

            if (!restMethod.AttachmentNameMap.TryGetValue(i, out var attachment))
            {
                itemName = restMethod.QueryParameterMap[i];
                parameterName = itemName;
            }
            else
            {
                itemName = attachment.Item1;
                parameterName = attachment.Item2;
            }

            // Check to see if it's an IEnumerable
            if (param is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable)
                {
                    AddMultipartItem(multiPartContent!, itemName, parameterName, item);
                }
            }
            else
            {
                AddMultipartItem(multiPartContent!, itemName, parameterName, param);
            }
        }

        /// <summary>Adds a single value to a multipart form as the appropriate content type.</summary>
        /// <param name="multiPartContent">The multipart content to add to.</param>
        /// <param name="fileName">The file name to use for file-like parts.</param>
        /// <param name="parameterName">The form field name for the part.</param>
        /// <param name="itemValue">The value to add.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AddMultipartItem(
            MultipartFormDataContent multiPartContent,
            string fileName,
            string parameterName,
            object itemValue)
        {
            if (itemValue is HttpContent content)
            {
                multiPartContent.Add(content);
                return;
            }

            if (itemValue is MultipartItem multipartItem)
            {
                var httpContent = multipartItem.ToContent();
                multiPartContent.Add(
                    httpContent,
                    multipartItem.Name ?? parameterName,
                    string.IsNullOrEmpty(multipartItem.FileName) ? fileName : multipartItem.FileName);
                return;
            }

            if (itemValue is Stream streamValue)
            {
                var streamContent = new StreamContent(streamValue);
                multiPartContent.Add(streamContent, parameterName, fileName);
                return;
            }

            if (itemValue is string stringValue)
            {
                multiPartContent.Add(new StringContent(stringValue), parameterName);
                return;
            }

            if (itemValue is FileInfo fileInfoValue)
            {
                var fileContent = new StreamContent(fileInfoValue.OpenRead());
                multiPartContent.Add(fileContent, parameterName, fileInfoValue.Name);
                return;
            }

            if (itemValue is byte[] byteArrayValue)
            {
                var fileContent = new ByteArrayContent(byteArrayValue);
                multiPartContent.Add(fileContent, parameterName, fileName);
                return;
            }

            AddSerializedMultipartItem(multiPartContent, fileName, parameterName, itemValue);
        }

        /// <summary>Adds a multipart part by serializing the value, throwing a descriptive error on failure.</summary>
        /// <param name="multiPartContent">The multipart content to add to.</param>
        /// <param name="fileName">The file name used in the error message.</param>
        /// <param name="parameterName">The form field name for the part.</param>
        /// <param name="itemValue">The value to serialize and add.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AddSerializedMultipartItem(
            MultipartFormDataContent multiPartContent,
            string fileName,
            string parameterName,
            object itemValue)
        {
            // Fallback to serializer
            Exception e;
            try
            {
                multiPartContent.Add(
                    _settings.ContentSerializer.ToHttpContent(itemValue),
                    parameterName);
                return;
            }
            catch (Exception ex)
            {
                // Eat this since we're about to throw as a fallback anyway
                e = ex;
            }

            throw new ArgumentException(
                $"Unexpected parameter type in a Multipart request. Parameter {fileName} is of type {itemValue.GetType().Name}, "
                    + "whereas allowed types are String, Stream, FileInfo, Byte array and anything that's JSON serializable",
                nameof(itemValue),
                e);
        }

        /// <summary>Appends query key/value pairs for a single parameter value.</summary>
        /// <param name="queryParamsToAdd">The list receiving query parameters.</param>
        /// <param name="param">The parameter value.</param>
        /// <param name="parameterInfo">Reflection info for the parameter.</param>
        /// <param name="queryPath">The query key path for the parameter.</param>
        /// <param name="queryAttribute">The query attribute governing formatting.</param>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private void AppendQueryParameter(
            List<KeyValuePair<string, string?>> queryParamsToAdd,
            object? param,
            ParameterInfo parameterInfo,
            string queryPath,
            QueryAttribute queryAttribute)
        {
            if (param is not string and IEnumerable paramValues)
            {
                foreach (var value in ParseEnumerableQueryParameterValue(
                             paramValues,
                             parameterInfo,
                             parameterInfo.ParameterType,
                             queryAttribute))
                {
                    queryParamsToAdd.Add(new(queryPath, value));
                }

                return;
            }

            queryParamsToAdd.Add(
                new(
                    queryPath,
                    _settings.UrlParameterFormatter.Format(
                        param,
                        parameterInfo,
                        parameterInfo.ParameterType)));
        }

        /// <summary>Formats an enumerable parameter value according to the effective collection format.</summary>
        /// <param name="paramValues">The enumerable values to format.</param>
        /// <param name="customAttributeProvider">The attribute provider for the parameter or property.</param>
        /// <param name="type">The element type used for formatting.</param>
        /// <param name="queryAttribute">The query attribute governing the collection format, if any.</param>
        /// <param name="fallbackCollectionFormat">The collection format to use when none is specified.</param>
        /// <returns>The formatted query values.</returns>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        private IEnumerable<string?> ParseEnumerableQueryParameterValue(
            IEnumerable paramValues,
            ICustomAttributeProvider customAttributeProvider,
            Type type,
            QueryAttribute? queryAttribute,
            CollectionFormat? fallbackCollectionFormat = null)
        {
            // Precedence: the property's own [Query] format wins; otherwise the format
            // supplied by the enclosing parameter's [Query] attribute (if any); finally
            // the global RefitSettings default.
            var collectionFormat =
                queryAttribute?.IsCollectionFormatSpecified == true
                    ? queryAttribute.CollectionFormat
                    : fallbackCollectionFormat ?? _settings.CollectionFormat;

            if (collectionFormat == CollectionFormat.Multi)
            {
                foreach (var paramValue in paramValues)
                {
                    yield return _settings.UrlParameterFormatter.Format(
                        paramValue,
                        customAttributeProvider,
                        type);
                }

                yield break;
            }

            var delimiter =
                collectionFormat switch
                {
                    CollectionFormat.Ssv => " ",
                    CollectionFormat.Tsv => "\t",
                    CollectionFormat.Pipes => "|",
                    _ => ","
                };

            // Missing a "default" clause was preventing the collection from serializing at all, as it was hitting "continue" thus causing an off-by-one error
            yield return JoinFormattedQueryValues(paramValues, customAttributeProvider, type, delimiter);
        }

        /// <summary>Formats and joins an enumerable query value without LINQ adapters.</summary>
        /// <param name="paramValues">The enumerable values to format.</param>
        /// <param name="customAttributeProvider">The attribute provider for the parameter or property.</param>
        /// <param name="type">The element type used for formatting.</param>
        /// <param name="delimiter">The delimiter between formatted values.</param>
        /// <returns>The joined formatted values.</returns>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
        [SuppressMessage(
            "Major Code Smell",
            "S2930:\"IDisposables\" should be disposed",
            Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
        private string JoinFormattedQueryValues(
            IEnumerable paramValues,
            ICustomAttributeProvider customAttributeProvider,
            Type type,
            string delimiter)
        {
            var enumerator = paramValues.GetEnumerator();
            try
            {
                if (!enumerator.MoveNext())
                {
                    return string.Empty;
                }

                var builder = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
                builder.Append(
                    _settings.UrlParameterFormatter.Format(
                        enumerator.Current,
                        customAttributeProvider,
                        type));

                while (enumerator.MoveNext())
                {
                    builder.Append(delimiter);
                    builder.Append(
                        _settings.UrlParameterFormatter.Format(
                            enumerator.Current,
                            customAttributeProvider,
                            type));
                }

                return builder.ToString();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}
