// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace Refit
{
    /// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
    internal partial class RequestBuilderImplementation
    {
        /// <summary>Determines whether a property should be skipped when building the query map.</summary>
        /// <param name="propertyInfo">The property to inspect.</param>
        /// <param name="parameterInfo">Optional parameter info used to skip path-bound properties.</param>
        /// <returns><see langword="true"/> when the property is ignored or already bound to the path.</returns>
        private static bool ShouldSkipQueryProperty(PropertyInfo propertyInfo, RestMethodParameterInfo? parameterInfo)
        {
            if (ShouldIgnorePropertyInQueryMap(propertyInfo))
            {
                return true;
            }

            // Compare by name rather than PropertyInfo reference: for a derived runtime
            // type the inherited property is a different PropertyInfo instance, which
            // would otherwise be wrongly emitted as a duplicate query parameter.
            return parameterInfo is { IsObjectPropertyParameter: true }
                && parameterInfo.ParameterProperties.Exists(x => x.PropertyInfo.Name == propertyInfo.Name);
        }

        /// <summary>Determines whether a property is marked to be ignored during serialization.</summary>
        /// <param name="propertyInfo">The property to inspect.</param>
        /// <returns><see langword="true"/> if the property carries an ignore attribute; otherwise <see langword="false"/>.</returns>
        private static bool ShouldIgnorePropertyInQueryMap(PropertyInfo propertyInfo)
        {
            foreach (var attributeData in propertyInfo.GetCustomAttributesData())
            {
                var fullName = attributeData.AttributeType.FullName;
                if (fullName is "System.Runtime.Serialization.IgnoreDataMemberAttribute"
                    or "System.Text.Json.Serialization.JsonIgnoreAttribute"
                    or "Newtonsoft.Json.JsonIgnoreAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Applies the collected headers to the request, creating empty content if needed.</summary>
        /// <param name="headersToAdd">The headers to apply, or null.</param>
        /// <param name="ret">The request message to populate.</param>
        private static void AddHeadersToRequest(Dictionary<string, string?>? headersToAdd, HttpRequestMessage ret)
        {
            // NB: We defer setting headers until the body has been
            // added so any custom content headers don't get left out.
            if (headersToAdd is null || headersToAdd.Count == 0)
            {
                return;
            }

            // We could have content headers, so we need to make
            // sure we have an HttpContent object to add them to,
            // provided the HttpClient will allow it for the method
            if (ret.Content is null && !IsBodyless(ret.Method))
            {
                ret.Content = new ByteArrayContent([]);
            }

            foreach (var header in headersToAdd)
            {
                SetHeader(ret, header.Key, header.Value);
            }
        }

        /// <summary>Extracts any query parameters already present on the URI into the pending list.</summary>
        /// <param name="uri">The URI builder whose query is read.</param>
        /// <param name="queryParamsToAdd">The pending query parameter list, created if needed.</param>
        private static void ParseExistingQueryString(UriBuilder uri, ref List<KeyValuePair<string, string?>>? queryParamsToAdd)
        {
            if (string.IsNullOrEmpty(uri.Query))
            {
                return;
            }

            queryParamsToAdd ??= [];
            var query = HttpUtility.ParseQueryString(uri.Query);
            var index = 0;
            foreach (var key in query.AllKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    queryParamsToAdd.Insert(
                        index++,
                        new(key, query[key]));
                }
            }
        }

        /// <summary>Builds an escaped query string from the collected key/value pairs.</summary>
        /// <param name="queryParamsToAdd">The query parameters to encode.</param>
        /// <returns>The encoded query string.</returns>
        [SuppressMessage(
            "Major Code Smell",
            "S2930:\"IDisposables\" should be disposed",
            Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
        private static string CreateQueryString(List<KeyValuePair<string, string?>> queryParamsToAdd)
        {
            var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
            var firstQuery = true;
            foreach (var queryParam in queryParamsToAdd)
            {
                if (queryParam is not { Key: not null, Value: not null })
                {
                    continue;
                }

                if (!firstQuery)
                {
                    // for all items after the first we add a & symbol
                    vsb.Append('&');
                }

                var key = Uri.EscapeDataString(queryParam.Key);
#if NET6_0_OR_GREATER
                // if first query does not start with ? then prepend it
                if (vsb.Length == 0 && key.Length > 0 && key[0] != '?')
                {
                    // query starts with ?
                    vsb.Append('?');
                }
#endif
                vsb.Append(key);
                vsb.Append('=');
                vsb.Append(Uri.EscapeDataString(queryParam.Value ?? string.Empty));
                if (firstQuery)
                {
                    firstQuery = false;
                }
            }

            return vsb.ToString();
        }

        /// <summary>Determines whether a value should be emitted directly rather than expanded into a query map.</summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns><see langword="true"/> if the value is a simple/formattable type; otherwise <see langword="false"/>.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
#endif
        private static bool DoNotConvertToQueryMap(object? value)
        {
            if (value is null)
            {
                return false;
            }

            var type = value.GetType();

            // Bail out early & match string
            if (ShouldReturn(type))
            {
                return true;
            }

            if (value is not IEnumerable)
            {
                return false;
            }

            // Get the element type for enumerables
            var ienu = typeof(IEnumerable<>);

            // We don't want to enumerate to get the type, so we'll just look for IEnumerable<T>
            var intType = type.GetInterfaces()
                .FirstOrDefault(
                    i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == ienu);

            if (intType is null)
            {
                return false;
            }

            type = intType.GetGenericArguments()[0];
            return ShouldReturn(type);

            // Check if type is a simple string or IFormattable type, check underlying type if Nullable<T>
            static bool ShouldReturn(Type type) =>
                Nullable.GetUnderlyingType(type) is { } underlyingType
                    ? ShouldReturn(underlyingType)
                    : type == typeof(string)
                      || type == typeof(bool)
                      || type == typeof(char)
                      || typeof(IFormattable).IsAssignableFrom(type)
                      || type == typeof(Uri)
                      || typeof(CultureInfo).IsAssignableFrom(type);
        }

        /// <summary>Sets or replaces a header on the request or its content, with CRLF-injection protection.</summary>
        /// <param name="request">The request to modify.</param>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value, or null to only remove the header.</param>
        private static void SetHeader(HttpRequestMessage request, string name, string? value)
        {
            // Clear any existing version of this header that might be set, because
            // we want to allow removal/redefinition of headers.
            // We also don't want to double up content headers which may have been
            // set for us automatically.
            // NB: We have to enumerate the header names to check existence because
            // Contains throws if it's the wrong header type for the collection.
            // HTTP header names are case-insensitive, so compare them that way; otherwise a
            // differently cased header (e.g. "Content-type" vs "Content-Type") is not removed
            // and ends up duplicated.
            if (request.Headers.Any(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase)))
            {
                request.Headers.Remove(name);
            }

            if (request.Content?.Headers.Any(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase)) == true)
            {
                request.Content.Headers.Remove(name);
            }

            if (value is null)
            {
                return;
            }

            // CRLF injection protection
            name = EnsureSafe(name);
            value = EnsureSafe(value);

            var added = request.Headers.TryAddWithoutValidation(name, value);

            // Don't even bother trying to add the header as a content header
            // if we just added it to the other collection.
            if (added || request.Content is null)
            {
                return;
            }

            request.Content.Headers.TryAddWithoutValidation(name, value);
        }

        /// <summary>Strips CR and LF characters from a header value to prevent header injection.</summary>
        /// <param name="value">The value to sanitize.</param>
        /// <returns>The value with carriage-return and line-feed characters removed.</returns>
        private static string EnsureSafe(string value) =>
#if NET8_0_OR_GREATER
            value.Replace("\r", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal);
#else
            value.Replace("\r", string.Empty)
                .Replace("\n", string.Empty);
#endif

        /// <summary>Determines whether the HTTP method must not carry a request body.</summary>
        /// <param name="method">The HTTP method to check.</param>
        /// <returns><see langword="true"/> for GET and HEAD; otherwise <see langword="false"/>.</returns>
        private static bool IsBodyless(HttpMethod method) => method == HttpMethod.Get || method == HttpMethod.Head;

        /// <summary>Populates the Authorization header from the configured token getter when present.</summary>
        /// <param name="request">The request to add the header to.</param>
        /// <param name="cancellationToken">A token to cancel the getter.</param>
        /// <returns>A task that completes when the header has been set.</returns>
        private Task AddAuthorizationHeadersFromGetterAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            RequestExecutionHelpers.AddAuthorizationHeaderFromGetterAsync(request, _settings, cancellationToken);

        /// <summary>Adds configured options/properties and Refit metadata to the request.</summary>
        /// <param name="restMethod">The rest method being invoked.</param>
        /// <param name="ret">The request message to populate.</param>
        /// <param name="paramList">The argument values for the call.</param>
        private void AddPropertiesToRequest(RestMethodInfoInternal restMethod, HttpRequestMessage ret, object[] paramList)
        {
            // Add RefitSetting.HttpRequestMessageOptions to the HttpRequestMessage
            if (_settings.HttpRequestMessageOptions is not null)
            {
                foreach (var p in _settings.HttpRequestMessageOptions)
                {
#if NET6_0_OR_GREATER
                    ret.Options.Set(new(p.Key), p.Value);
#else
                    ret.Properties.Add(p);
#endif
                }
            }

            for (var i = 0; i < paramList.Length; i++)
            {
                if (restMethod.PropertyParameterMap.TryGetValue(i, out var propertyKey))
                {
#if NET6_0_OR_GREATER
                    ret.Options.Set(
                        new(propertyKey),
                        paramList[i]);
#else
                    ret.Properties[propertyKey] = paramList[i];
#endif
                }
            }

            // Always add the top-level type of the interface to the properties
#if NET6_0_OR_GREATER
            ret.Options.Set(
                new(HttpRequestMessageOptions.InterfaceType),
                TargetType);
            ret.Options.Set(
                new(
                    HttpRequestMessageOptions.RestMethodInfo),
                restMethod.RestMethodInfo);
#else
            ret.Properties[HttpRequestMessageOptions.InterfaceType] = TargetType;
            ret.Properties[HttpRequestMessageOptions.RestMethodInfo] =
                restMethod.RestMethodInfo;
#endif
        }

#if NET6_0_OR_GREATER
        /// <summary>Applies the configured HTTP version and version policy to the request.</summary>
        /// <param name="ret">The request message to populate.</param>
        private void AddVersionToRequest(HttpRequestMessage ret)
        {
            ret.Version = _settings.Version;
            ret.VersionPolicy = _settings.VersionPolicy;
        }
#endif

        /// <summary>Flattens an object's public properties into query-string key/value pairs.</summary>
        /// <param name="object">The object to flatten, or null.</param>
        /// <param name="delimiter">The delimiter used between nested property names.</param>
        /// <param name="parameterInfo">Optional parameter info used to skip path-bound properties.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
        /// <returns>The query-string key/value pairs.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private List<KeyValuePair<string, object?>> BuildQueryMap(
            object? @object,
            string? delimiter = null,
            RestMethodParameterInfo? parameterInfo = null,
            CollectionFormat? collectionFormat = null)
        {
            if (@object is IDictionary idictionary)
            {
                return BuildQueryMap(idictionary, delimiter, collectionFormat);
            }

            var kvps = new List<KeyValuePair<string, object?>>();

            if (@object is null)
            {
                return kvps;
            }

            var props = @object
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetMethod?.IsPublic == true);

            foreach (var propertyInfo in props)
            {
                AppendPropertyToQueryMap(@object, propertyInfo, kvps, delimiter, parameterInfo, collectionFormat);
            }

            return kvps;
        }

        /// <summary>Flattens a dictionary into query-string key/value pairs.</summary>
        /// <param name="dictionary">The dictionary to flatten.</param>
        /// <param name="delimiter">The delimiter used between nested keys.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
        /// <returns>The query-string key/value pairs.</returns>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private List<KeyValuePair<string, object?>> BuildQueryMap(
            IDictionary dictionary,
            string? delimiter = null,
            CollectionFormat? collectionFormat = null)
        {
            var kvps = new List<KeyValuePair<string, object?>>();

            foreach (var key in dictionary.Keys)
            {
                var obj = dictionary[key];
                if (obj is null)
                {
                    continue;
                }

                var keyType = key.GetType();
                var formattedKey = _settings.UrlParameterFormatter.Format(key, keyType, keyType);

                if (string.IsNullOrWhiteSpace(formattedKey)) // blank keys can't be put in the query string
                {
                    continue;
                }

                if (DoNotConvertToQueryMap(obj))
                {
                    kvps.Add(new(formattedKey!, obj));
                }
                else
                {
                    foreach (var keyValuePair in BuildQueryMap(obj, delimiter, null, collectionFormat))
                    {
                        kvps.Add(
                            new(
                                formattedKey + delimiter + keyValuePair.Key,
                                keyValuePair.Value));
                    }
                }
            }

            return kvps;
        }

        /// <summary>Appends a single property's query-string key/value pairs to the accumulating list.</summary>
        /// <param name="object">The object the property belongs to.</param>
        /// <param name="propertyInfo">The property to flatten.</param>
        /// <param name="kvps">The accumulating list of query pairs.</param>
        /// <param name="delimiter">The delimiter used between nested property names.</param>
        /// <param name="parameterInfo">Optional parameter info used to skip path-bound properties.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private void AppendPropertyToQueryMap(
            object @object,
            PropertyInfo propertyInfo,
            List<KeyValuePair<string, object?>> kvps,
            string? delimiter,
            RestMethodParameterInfo? parameterInfo,
            CollectionFormat? collectionFormat)
        {
            var obj = propertyInfo.GetValue(@object);
            if (obj is null || ShouldSkipQueryProperty(propertyInfo, parameterInfo))
            {
                return;
            }

            var aliasAttribute = propertyInfo.GetCustomAttribute<AliasAsAttribute>();
            var key = aliasAttribute?.Name ?? _settings.UrlParameterKeyFormatter.Format(propertyInfo.Name);

            // Look to see if the property has a Query attribute, and if so, format it accordingly
            var queryAttribute = propertyInfo.GetCustomAttribute<QueryAttribute>();
            if (queryAttribute is { Format: not null })
            {
                obj = _settings.FormUrlEncodedParameterFormatter.Format(
                    obj,
                    queryAttribute.Format);
            }

            // If obj is IEnumerable - format it accounting for Query attribute and CollectionFormat
            if (obj is not string and IEnumerable ienu and not IDictionary)
            {
                AppendEnumerablePropertyValues(ienu, propertyInfo, key, kvps, queryAttribute, collectionFormat);
                return;
            }

            if (DoNotConvertToQueryMap(obj))
            {
                kvps.Add(new(key, obj));
                return;
            }

            AppendNestedQueryMap(obj, key, kvps, delimiter, collectionFormat);
        }

        /// <summary>Appends each formatted element of an enumerable property to the query map under one key.</summary>
        /// <param name="values">The enumerable property value.</param>
        /// <param name="propertyInfo">The property being flattened.</param>
        /// <param name="key">The query key for the property.</param>
        /// <param name="kvps">The accumulating list of query pairs.</param>
        /// <param name="queryAttribute">The property's query attribute, if any.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private void AppendEnumerablePropertyValues(
            IEnumerable values,
            PropertyInfo propertyInfo,
            string key,
            List<KeyValuePair<string, object?>> kvps,
            QueryAttribute? queryAttribute,
            CollectionFormat? collectionFormat)
        {
            foreach (var value in ParseEnumerableQueryParameterValue(
                values,
                propertyInfo,
                propertyInfo.PropertyType,
                queryAttribute,
                collectionFormat))
            {
                kvps.Add(new(key, value));
            }
        }

        /// <summary>Flattens a nested object or dictionary value into prefixed query-string key/value pairs.</summary>
        /// <param name="obj">The nested value to flatten.</param>
        /// <param name="key">The key prefix for the nested value.</param>
        /// <param name="kvps">The accumulating list of query pairs.</param>
        /// <param name="delimiter">The delimiter used between nested keys.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
        [RequiresDynamicCode("Refit's reflection-based request building requires runtime code generation; use the Refit source generator for AOT apps.")]
#endif
        private void AppendNestedQueryMap(
            object? obj,
            string key,
            List<KeyValuePair<string, object?>> kvps,
            string? delimiter,
            CollectionFormat? collectionFormat)
        {
            var nested = obj is IDictionary idict
                ? BuildQueryMap(idict, delimiter, collectionFormat)
                : BuildQueryMap(obj, delimiter, null, collectionFormat);

            foreach (var keyValuePair in nested)
            {
                kvps.Add(
                    new(
                        key + delimiter + keyValuePair.Key,
                        keyValuePair.Value));
            }
        }
    }
}
