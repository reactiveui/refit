// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web;

namespace Refit;

/// <summary>Reflection-based request builder that turns Refit interface calls into HTTP requests.</summary>
internal partial class RequestBuilderImplementation
{
    /// <summary>Caches per-type query-property metadata without keeping collectible types alive.</summary>
    internal static readonly ConditionalWeakTable<Type, QueryPropertyMetadata[]> QueryPropertyCache = new();

    /// <summary>Caches an attribute provider per parameter/property/type that materializes the <see cref="QueryAttribute"/>
    /// lookup once, so repeated formatting never re-reads it from metadata.</summary>
    internal static readonly ConditionalWeakTable<ICustomAttributeProvider, CachedAttributeProvider> AttributeProviderCache = new();

    /// <summary>Reuses one delegate for the cache factory so the flattening path never allocates a callback.</summary>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2111:Method with DynamicallyAccessedMembersAttribute is accessed via reflection",
        Justification = "The cache callback receives the same Type key that carries the public property metadata requirement.")]
    private static readonly ConditionalWeakTable<Type, QueryPropertyMetadata[]>.CreateValueCallback QueryPropertyFactory =
        BuildQueryPropertyMetadata;

    /// <summary>Determines whether a property should be skipped when building the query map.</summary>
    /// <param name="metadata">The cached metadata for the property to inspect.</param>
    /// <param name="parameterInfo">Optional parameter info used to skip path-bound properties.</param>
    /// <returns><see langword="true"/> when the property is ignored or already bound to the path.</returns>
    internal static bool ShouldSkipQueryProperty(in QueryPropertyMetadata metadata, RestMethodParameterInfo? parameterInfo)
    {
        if (metadata.IsIgnored)
        {
            return true;
        }

        if (parameterInfo is not { IsObjectPropertyParameter: true })
        {
            return false;
        }

        // Compare by name rather than PropertyInfo reference: for a derived runtime
        // type the inherited property is a different PropertyInfo instance, which
        // would otherwise be wrongly emitted as a duplicate query parameter.
        // A manual scan avoids the per-call predicate closure a List.Exists lambda would allocate.
        var propertyName = metadata.Property.Name;
        var properties = parameterInfo.ParameterProperties;
        for (var i = 0; i < properties.Count; i++)
        {
            if (properties[i].PropertyChain[0].Name == propertyName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a property is marked to be ignored during serialization.</summary>
    /// <param name="propertyInfo">The property to inspect.</param>
    /// <returns><see langword="true"/> if the property carries an ignore attribute; otherwise <see langword="false"/>.</returns>
    internal static bool ShouldIgnorePropertyInQueryMap(PropertyInfo propertyInfo)
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
    /// <param name="validateHeaders">Whether header values are validated as they are applied; see <see cref="SetHeader"/>.</param>
    internal static void AddHeadersToRequest(Dictionary<string, string?>? headersToAdd, HttpRequestMessage ret, bool validateHeaders)
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
            SetHeader(ret, header.Key, header.Value, validateHeaders);
        }
    }

    /// <summary>Extracts any query parameters already present on the URI into the pending list.</summary>
    /// <param name="uri">The URI builder whose query is read.</param>
    /// <param name="queryParamsToAdd">The pending query parameter list, created if needed.</param>
    internal static void ParseExistingQueryString(UriBuilder uri, ref List<QueryParameterEntry>? queryParamsToAdd) =>
        ParseQueryStringInto(uri.Query, ref queryParamsToAdd);

    /// <summary>Assigns the request URI as a bare relative reference so the <see cref="HttpClient"/> merges it
    /// with the base address using RFC 3986 rules, preserving whether the path has a leading slash.</summary>
    /// <param name="ret">The request message being populated.</param>
    /// <param name="urlTarget">The expanded relative path, with dynamic segments already escaped.</param>
    /// <param name="queryParamsToAdd">The query parameters collected for the request, if any.</param>
    internal static void AssignRequestUriRfc3986(
        HttpRequestMessage ret,
        string urlTarget,
        List<QueryParameterEntry>? queryParamsToAdd)
    {
        var path = urlTarget;
        var queryIndex = urlTarget.IndexOf('?');
        if (queryIndex >= 0)
        {
            ParseQueryStringInto(urlTarget[queryIndex..], ref queryParamsToAdd);
            path = urlTarget[..queryIndex];
        }

        var query = queryParamsToAdd is not null && queryParamsToAdd.Count != 0
            ? CreateQueryString(queryParamsToAdd)
            : string.Empty;

        ret.RequestUri = new(path + query, UriKind.Relative);
    }

    /// <summary>Assigns the request URI directly from a <c>[Url]</c> parameter's absolute value, appending any
    /// collected query parameters. Mirrors the generated absolute-URL path so both builders produce the same URI.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="ret">The request message being populated.</param>
    /// <param name="paramList">The argument values for the call.</param>
    /// <param name="queryParamsToAdd">The query parameters collected for the request, if any.</param>
    internal static void AssignAbsoluteRequestUri(
        RestMethodInfoInternal restMethod,
        HttpRequestMessage ret,
        object[] paramList,
        List<QueryParameterEntry>? queryParamsToAdd)
    {
        var urlString = GeneratedRequestRunner.RequireAbsoluteUrl(paramList[restMethod.UrlParameterInfo]);

        var builder = new UriBuilder(new Uri(urlString, UriKind.Absolute));
        ParseExistingQueryString(builder, ref queryParamsToAdd);

        builder.Query = queryParamsToAdd is not null && queryParamsToAdd.Count != 0
            ? CreateQueryString(queryParamsToAdd)
            : null;

        ret.RequestUri = builder.Uri;
    }

    /// <summary>Parses a raw query string into the pending query parameter list, ahead of any already-collected entries.</summary>
    /// <param name="queryString">The raw query string, with or without a leading '?'.</param>
    /// <param name="queryParamsToAdd">The pending query parameter list, created if needed.</param>
    /// <remarks>Mirrors the reference query-string parser without materializing a name/value collection: a single leading
    /// '?' is dropped, keys and values are URL-decoded (<c>+</c> to space, percent escapes), a segment with no '=' or a
    /// blank key is skipped, and keys that match case-insensitively are joined with commas under the first-seen key.</remarks>
    internal static void ParseQueryStringInto(string? queryString, ref List<QueryParameterEntry>? queryParamsToAdd)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return;
        }

        // Assign to a non-null local; the older reference assemblies' string.IsNullOrEmpty lacks a nullable-flow annotation.
        var query = queryString!;
        var position = query[0] == '?' ? 1 : 0;
        List<QueryParameterEntry>? parsed = null;
        while (position < query.Length)
        {
            var ampersand = query.IndexOf('&', position);
            var segmentEnd = ampersand < 0 ? query.Length : ampersand;
            var equals = query.IndexOf('=', position, segmentEnd - position);

            // A segment with no '=' has a null name in the reference parser and is dropped, as is a blank key.
            if (equals >= 0)
            {
                var key = HttpUtility.UrlDecode(query.Substring(position, equals - position));
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var value = HttpUtility.UrlDecode(query.Substring(equals + 1, segmentEnd - equals - 1));
                    AppendOrJoinQueryValue(ref parsed, key, value);
                }
            }

            position = segmentEnd + 1;
        }

        if (parsed is null)
        {
            return;
        }

        queryParamsToAdd ??= [];
        for (var i = 0; i < parsed.Count; i++)
        {
            queryParamsToAdd.Insert(i, parsed[i]);
        }
    }

    /// <summary>Adds a parsed query key/value, joining the value under an existing case-insensitively equal key.</summary>
    /// <param name="parsed">The accumulating parsed entries, created on first use.</param>
    /// <param name="key">The decoded query key.</param>
    /// <param name="value">The decoded query value.</param>
    internal static void AppendOrJoinQueryValue(ref List<QueryParameterEntry>? parsed, string key, string? value)
    {
        parsed ??= [];
        for (var i = 0; i < parsed.Count; i++)
        {
            if (string.Equals(parsed[i].Key, key, StringComparison.InvariantCultureIgnoreCase))
            {
                parsed[i] = new(parsed[i].Key, parsed[i].Value + "," + value);
                return;
            }
        }

        parsed.Add(new(key, value));
    }

    /// <summary>Builds an escaped query string from the collected key/value pairs.</summary>
    /// <param name="queryParamsToAdd">The query parameters to encode.</param>
    /// <returns>The encoded query string.</returns>
    [SuppressMessage(
        "Correctness",
        "SST2410:A created disposable is never disposed",
        Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
    internal static string CreateQueryString(List<QueryParameterEntry> queryParamsToAdd)
    {
        var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
        var firstQuery = true;
        for (var i = 0; i < queryParamsToAdd.Count; i++)
        {
            var queryParam = queryParamsToAdd[i];

            // A formatter may render a value as null; such parameters are omitted entirely.
            if (queryParam.Value is null)
            {
                continue;
            }

            if (!firstQuery)
            {
                // for all items after the first we add a & symbol
                vsb.Append('&');
            }

            var key = queryParam.KeyPreEscaped ? queryParam.Key : StringHelpers.EscapeDataString(queryParam.Key);
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
            vsb.Append(StringHelpers.EscapeDataString(queryParam.Value));
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
    internal static string EnsureSafe(string value) => StringHelpers.RemoveCrOrLf(value);

    /// <summary>Determines whether the HTTP method must not carry a request body.</summary>
    /// <param name="method">The HTTP method to check.</param>
    /// <returns><see langword="true"/> for GET and HEAD; otherwise <see langword="false"/>.</returns>
    internal static bool IsBodyless(HttpMethod method) => method == HttpMethod.Get || method == HttpMethod.Head;

    /// <summary>Checks whether a header collection contains a key without throwing for unsupported header types.</summary>
    /// <param name="headers">The header collection to inspect.</param>
    /// <param name="name">The header name.</param>
    /// <returns><see langword="true"/> when the header key exists; otherwise <see langword="false"/>.</returns>
    internal static bool ContainsHeader(System.Net.Http.Headers.HttpHeaders headers, string name)
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

    /// <summary>Gets the cached query-property metadata for the given type.</summary>
    /// <param name="type">The object type to inspect.</param>
    /// <returns>The readable public instance properties with their attribute-derived facts.</returns>
    internal static QueryPropertyMetadata[] GetCachedQueryProperties(Type type) =>
        QueryPropertyCache.GetValue(type, QueryPropertyFactory);

    /// <summary>Gets an attribute provider for a parameter, property or type whose <see cref="QueryAttribute"/> lookup is
    /// materialized once and reused for every formatted value.</summary>
    /// <param name="provider">The underlying attribute provider.</param>
    /// <returns>The cached attribute provider.</returns>
    internal static ICustomAttributeProvider GetCachedAttributeProvider(ICustomAttributeProvider provider) =>
        AttributeProviderCache.GetValue(provider, static p => new CachedAttributeProvider(p));

    /// <summary>Builds the per-type query-property metadata, reading each property's serialization-ignore, query and
    /// alias attributes once so the per-request flattening path never touches attribute metadata.</summary>
    /// <param name="type">The object type to inspect.</param>
    /// <returns>The metadata for each readable public instance property.</returns>
    internal static QueryPropertyMetadata[] BuildQueryPropertyMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type)
    {
        var properties = ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(type);
        var metadata = new QueryPropertyMetadata[properties.Length];
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            metadata[i] = new(
                property,
                ShouldIgnorePropertyInQueryMap(property),
                property.GetCustomAttribute<QueryAttribute>(),
                property.GetCustomAttribute<AliasAsAttribute>());
        }

        return metadata;
    }

    /// <summary>Populates the Authorization header from the configured token getter when present.</summary>
    /// <param name="request">The request to add the header to.</param>
    /// <param name="cancellationToken">A token to cancel the getter.</param>
    /// <returns>A task that completes when the header has been set.</returns>
    internal Task AddAuthorizationHeadersFromGetterAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        RequestExecutionHelpers.AddAuthorizationHeaderFromGetterAsync(request, _settings, cancellationToken);

    /// <summary>Adds configured options/properties and Refit metadata to the request.</summary>
    /// <param name="restMethod">The rest method being invoked.</param>
    /// <param name="ret">The request message to populate.</param>
    /// <param name="paramList">The argument values for the call, with cancellation tokens removed.</param>
    /// <param name="declaredArguments">The full declared-order argument values, including any cancellation token.</param>
    internal void AddPropertiesToRequest(
        RestMethodInfoInternal restMethod,
        HttpRequestMessage ret,
        object[] paramList,
        object?[] declaredArguments)
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
        ret.Options.Set(
            new(HttpRequestMessageOptions.MethodName),
            restMethod.RestMethodInfo.Name);
        ret.Options.Set(
            new(HttpRequestMessageOptions.RelativePathTemplate),
            restMethod.RestMethodInfo.RelativePath);
#else
        ret.Properties[HttpRequestMessageOptions.InterfaceType] = TargetType;
        ret.Properties[HttpRequestMessageOptions.RestMethodInfo] =
            restMethod.RestMethodInfo;
        ret.Properties[HttpRequestMessageOptions.MethodName] =
            restMethod.RestMethodInfo.Name;
        ret.Properties[HttpRequestMessageOptions.RelativePathTemplate] =
            restMethod.RestMethodInfo.RelativePath;
#endif

        if (!_settings.CaptureMethodArguments)
        {
            return;
        }

#if NET6_0_OR_GREATER
        ret.Options.Set(
            new(HttpRequestMessageOptions.MethodArguments),
            declaredArguments);
#else
        ret.Properties[HttpRequestMessageOptions.MethodArguments] = declaredArguments;
#endif
    }

#if NET6_0_OR_GREATER
    /// <summary>Applies the configured HTTP version and version policy to the request.</summary>
    /// <param name="ret">The request message to populate.</param>
    internal void AddVersionToRequest(HttpRequestMessage ret)
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
    internal List<QueryMapEntry> BuildQueryMap(
        object @object,
        string? delimiter = null,
        RestMethodParameterInfo? parameterInfo = null,
        CollectionFormat? collectionFormat = null)
    {
        if (@object is IDictionary idictionary)
        {
            return BuildQueryMap(idictionary, delimiter, collectionFormat);
        }

        var props = GetCachedQueryProperties(@object.GetType());
        var kvps = new List<QueryMapEntry>(props.Length);
        for (var i = 0; i < props.Length; i++)
        {
            AppendPropertyToQueryMap(@object, in props[i], kvps, delimiter, parameterInfo, collectionFormat);
        }

        return kvps;
    }

    /// <summary>Flattens a dictionary into query-string key/value pairs.</summary>
    /// <param name="dictionary">The dictionary to flatten.</param>
    /// <param name="delimiter">The delimiter used between nested keys.</param>
    /// <param name="collectionFormat">The collection format for enumerable values.</param>
    /// <returns>The query-string key/value pairs.</returns>
    internal List<QueryMapEntry> BuildQueryMap(
        IDictionary dictionary,
        string? delimiter = null,
        CollectionFormat? collectionFormat = null)
    {
        var kvps = new List<QueryMapEntry>();

        foreach (var key in dictionary.Keys)
        {
            var obj = dictionary[key];
            if (obj is null)
            {
                continue;
            }

            var keyType = key.GetType();
            var formattedKey = GeneratedRequestRunner.FormatUrlParameter(_settings, key, GetCachedAttributeProvider(keyType), keyType);

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
    /// <param name="metadata">The cached metadata for the property to flatten.</param>
    /// <param name="kvps">The accumulating list of query pairs.</param>
    /// <param name="delimiter">The delimiter used between nested property names.</param>
    /// <param name="parameterInfo">Optional parameter info used to skip path-bound properties.</param>
    /// <param name="collectionFormat">The collection format for enumerable values.</param>
    internal void AppendPropertyToQueryMap(
        object @object,
        in QueryPropertyMetadata metadata,
        List<QueryMapEntry> kvps,
        string? delimiter,
        RestMethodParameterInfo? parameterInfo,
        CollectionFormat? collectionFormat)
    {
        var propertyInfo = metadata.Property;
        var obj = propertyInfo.GetValue(@object);
        if (ShouldSkipQueryProperty(in metadata, parameterInfo))
        {
            return;
        }

        var queryAttribute = metadata.QueryAttribute;
        var key = BuildPropertyQueryKey(propertyInfo, metadata.AliasAttribute, queryAttribute);

        if (obj is null)
        {
            // Null properties are skipped unless the property opts in via [Query(SerializeNull = true)].
            if (queryAttribute?.SerializeNull == true)
            {
                kvps.Add(new(key, string.Empty));
            }

            return;
        }

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

    /// <summary>Builds the query key for a property, honoring its alias and any query prefix/delimiter.</summary>
    /// <param name="propertyInfo">The property being flattened.</param>
    /// <param name="aliasAttribute">The property's alias attribute, if any.</param>
    /// <param name="queryAttribute">The property's query attribute, if any.</param>
    /// <returns>The query key for the property.</returns>
    internal string BuildPropertyQueryKey(PropertyInfo propertyInfo, AliasAsAttribute? aliasAttribute, QueryAttribute? queryAttribute)
    {
        // Match the form-encoded field-naming precedence (FormValueMultimap.GetFieldNameForProperty): an [AliasAs]
        // wins, then the configured content serializer's field name ([JsonPropertyName] for System.Text.Json,
        // [JsonProperty] for Newtonsoft.Json) unless disabled, then the URL parameter key formatter over the CLR name.
        var name = aliasAttribute?.Name
            ?? (_settings.HonorContentSerializerPropertyNamesInQuery
                ? _settings.ContentSerializer.GetFieldNameForProperty(propertyInfo)
                : null)
            ?? _settings.UrlParameterKeyFormatter.Format(propertyInfo.Name);

        // Honor a property-level [Query(delimiter, prefix)], matching how form-encoded fields are named.
        return queryAttribute is not null && !string.IsNullOrWhiteSpace(queryAttribute.Prefix)
            ? queryAttribute.Prefix + queryAttribute.Delimiter + name
            : name;
    }

    /// <summary>Applies a property-level query format, if present.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="queryAttribute">The query attribute, if any.</param>
    /// <param name="value">The value to format.</param>
    /// <param name="formattedValue">Receives the formatted value.</param>
    /// <returns><see langword="true"/> when a non-null value remains.</returns>
    internal bool TryFormatQueryPropertyValue<T>(
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
    internal void AppendEnumerablePropertyValues(
        IEnumerable values,
        PropertyInfo propertyInfo,
        string key,
        List<QueryMapEntry> kvps,
        QueryAttribute? queryAttribute,
        CollectionFormat? collectionFormat) =>
        AppendFormattedEnumerableValues(
            values,
            propertyInfo,
            propertyInfo.PropertyType,
            queryAttribute,
            collectionFormat,
            new QueryMapEntrySink(kvps, key));

    /// <summary>Flattens a nested object or dictionary value into prefixed query-string key/value pairs.</summary>
    /// <param name="obj">The nested value to flatten.</param>
    /// <param name="key">The key prefix for the nested value.</param>
    /// <param name="kvps">The accumulating list of query pairs.</param>
    /// <param name="delimiter">The delimiter used between nested keys.</param>
    /// <param name="collectionFormat">The collection format for enumerable values.</param>
    internal void AppendNestedQueryMap(
        object obj,
        string key,
        List<QueryMapEntry> kvps,
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
