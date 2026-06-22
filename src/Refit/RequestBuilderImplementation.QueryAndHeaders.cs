// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web;

namespace Refit
{
    /// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
    internal partial class RequestBuilderImplementation
    {
        /// <summary>Caches query-map properties by type without keeping collectible types alive.</summary>
        private static readonly ConditionalWeakTable<Type, PropertyInfo[]> QueryPropertyCache = new();

        /// <summary>Determines whether a property should be skipped when building the query map.</summary>
        /// <param name="propertyInfo">The property to inspect.</param>
        /// <param name="parameterInfo">Optional parameter info used to skip path-bound properties.</param>
        /// <returns><see langword="true"/> when the property is ignored or already bound to the path.</returns>
        private static bool ShouldSkipQueryProperty(PropertyInfo propertyInfo, RestMethodParameterInfo? parameterInfo)
        {
            // Compare by name rather than PropertyInfo reference: for a derived runtime
            // type the inherited property is a different PropertyInfo instance, which
            // would otherwise be wrongly emitted as a duplicate query parameter.
            return ShouldIgnorePropertyInQueryMap(propertyInfo)
                || (parameterInfo is { IsObjectPropertyParameter: true }
                    && parameterInfo.ParameterProperties.Exists(x => x.PropertyInfo.Name == propertyInfo.Name));
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
            var keys = query.AllKeys;
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
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
            for (var i = 0; i < queryParamsToAdd.Count; i++)
            {
                var queryParam = queryParamsToAdd[i];
                if (queryParam is not { Key: not null, Value: not null })
                {
                    continue;
                }

                if (!firstQuery)
                {
                    // for all items after the first we add a & symbol
                    vsb.Append('&');
                }

                var key = StringHelpers.EscapeDataString(queryParam.Key);
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
                vsb.Append(StringHelpers.EscapeDataString(queryParam.Value ?? string.Empty));
                if (firstQuery)
                {
                    firstQuery = false;
                }
            }

            return vsb.ToString();
        }

        /// <summary>Strips CR and LF characters from a header value to prevent header injection.</summary>
        /// <param name="value">The value to sanitize.</param>
        /// <returns>The value with carriage-return and line-feed characters removed.</returns>
        private static string EnsureSafe(string value) => StringHelpers.RemoveCrOrLf(value);

        /// <summary>Determines whether the HTTP method must not carry a request body.</summary>
        /// <param name="method">The HTTP method to check.</param>
        /// <returns><see langword="true"/> for GET and HEAD; otherwise <see langword="false"/>.</returns>
        private static bool IsBodyless(HttpMethod method) => method == HttpMethod.Get || method == HttpMethod.Head;

        /// <summary>Checks whether a header collection contains a key without throwing for unsupported header types.</summary>
        /// <param name="headers">The header collection to inspect.</param>
        /// <param name="name">The header name.</param>
        /// <returns><see langword="true"/> when the header key exists; otherwise <see langword="false"/>.</returns>
        private static bool ContainsHeader(System.Net.Http.Headers.HttpHeaders headers, string name)
        {
            foreach (var header in headers)
            {
                if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets cached query-map properties for the given type.</summary>
        /// <param name="type">The object type to inspect.</param>
        /// <returns>The readable public instance properties.</returns>
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2111:Method with DynamicallyAccessedMembersAttribute is accessed via reflection",
            Justification = "The cache callback receives the same Type key that carries the public property metadata requirement.")]
        private static PropertyInfo[] GetCachedQueryProperties(Type type) =>
            QueryPropertyCache.GetValue(type, ReflectionPropertyHelpers.GetReadablePublicInstanceProperties);

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
        private List<KeyValuePair<string, object?>> BuildQueryMap(
            object @object,
            string? delimiter = null,
            RestMethodParameterInfo? parameterInfo = null,
            CollectionFormat? collectionFormat = null)
        {
            if (@object is IDictionary idictionary)
            {
                return BuildQueryMap(idictionary, delimiter, collectionFormat);
            }

            var kvps = new List<KeyValuePair<string, object?>>();

            var props = GetCachedQueryProperties(@object.GetType());
            for (var i = 0; i < props.Length; i++)
            {
                var propertyInfo = props[i];
                AppendPropertyToQueryMap(@object, propertyInfo, kvps, delimiter, parameterInfo, collectionFormat);
            }

            return kvps;
        }

        /// <summary>Flattens a dictionary into query-string key/value pairs.</summary>
        /// <param name="dictionary">The dictionary to flatten.</param>
        /// <param name="delimiter">The delimiter used between nested keys.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
        /// <returns>The query-string key/value pairs.</returns>
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
                    var nestedQueryMap = BuildQueryMap(obj, delimiter, null, collectionFormat);
                    for (var i = 0; i < nestedQueryMap.Count; i++)
                    {
                        var keyValuePair = nestedQueryMap[i];
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

            var queryAttribute = propertyInfo.GetCustomAttribute<QueryAttribute>();
            if (!TryFormatQueryPropertyValue(queryAttribute, obj, out var formattedObj))
            {
                return;
            }

            obj = formattedObj;

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

        /// <summary>Applies a property-level query format, if present.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="queryAttribute">The query attribute, if any.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formattedValue">Receives the formatted value.</param>
        /// <returns><see langword="true"/> when a non-null value remains.</returns>
        private bool TryFormatQueryPropertyValue<T>(
            QueryAttribute? queryAttribute,
            T value,
            [NotNullWhen(true)] out object? formattedValue)
        {
            if (queryAttribute is not { Format: not null })
            {
                formattedValue = value!;
                return true;
            }

            var formatted = _settings.FormUrlEncodedParameterFormatter.Format(
                value,
                queryAttribute.Format);
            if (formatted is null)
            {
                formattedValue = null;
                return false;
            }

            formattedValue = formatted;
            return true;
        }

        /// <summary>Appends each formatted element of an enumerable property to the query map under one key.</summary>
        /// <param name="values">The enumerable property value.</param>
        /// <param name="propertyInfo">The property being flattened.</param>
        /// <param name="key">The query key for the property.</param>
        /// <param name="kvps">The accumulating list of query pairs.</param>
        /// <param name="queryAttribute">The property's query attribute, if any.</param>
        /// <param name="collectionFormat">The collection format for enumerable values.</param>
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
        private void AppendNestedQueryMap(
            object obj,
            string key,
            List<KeyValuePair<string, object?>> kvps,
            string? delimiter,
            CollectionFormat? collectionFormat)
        {
            var nested = obj is IDictionary idict
                ? BuildQueryMap(idict, delimiter, collectionFormat)
                : BuildQueryMap(obj, delimiter, null, collectionFormat);

            for (var i = 0; i < nested.Count; i++)
            {
                var keyValuePair = nested[i];
                kvps.Add(
                    new(
                        key + delimiter + keyValuePair.Key,
                        keyValuePair.Value));
            }
        }
    }
}
