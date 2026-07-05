// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Refit;

/// <summary>Shared runtime helpers used by source-generated request construction.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class GeneratedRequestRunner
{
    /// <summary>The underlying value of the obsolete <c>BodySerializationMethod.Json</c> member.</summary>
    private const int ObsoleteJsonBodySerializationMethodValue = 1;

    /// <summary>
    /// An internal cache that maps a composite key of a parent <see cref="Type"/> and a property name 
    /// to its corresponding <see cref="PropertyInfo"/>, avoiding redundant reflection overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type ParentType, string Property), PropertyInfo> _propertyCache = new ();

    /// <summary>
    /// An internal cache that maps a composite key of a parent <see cref="Type"/>, a method name, and a parameter name 
    /// to its corresponding <see cref="ParameterInfo"/>, avoiding redundant reflection overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type ParentType, string MethodName, string ParameterName), ParameterInfo> _parameterCache = new ();

    /// <summary>Builds the relative request URI for a generated request, joining the client base address with the method path.</summary>
    /// <param name="client">The HTTP client whose base address is used under legacy resolution.</param>
    /// <param name="relativePath">The method's relative path, including any leading slash and query string.</param>
    /// <param name="urlResolution">The configured URL resolution mode.</param>
    /// <returns>A relative <see cref="Uri"/> to assign to the request, which the client merges with its base address.</returns>
    public static Uri BuildRelativeUri(HttpClient client, string relativePath, UrlResolutionMode urlResolution)
    {
        if (urlResolution == UrlResolutionMode.Rfc3986)
        {
            // Let the HttpClient merge the base address with the relative path per RFC 3986; emit the path verbatim.
            return new(relativePath, UriKind.Relative);
        }

        var basePath = client.BaseAddress?.AbsolutePath
            ?? throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");
        basePath = basePath == "/" ? string.Empty : basePath.TrimEnd('/');
        return new(basePath + relativePath, UriKind.Relative);
    }

    /// <summary>Sends a generated request with no response body, throwing on HTTP errors.</summary>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The generated request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    public static async Task SendVoidAsync(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool bufferBody,
        CancellationToken cancellationToken)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using (request)
        {
            await RequestExecutionHelpers.SendVoidAsync(
                    client,
                    request,
                    settings,
                    bufferBody,
                    true,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Sends a generated request and deserializes or wraps its response.</summary>
    /// <typeparam name="T">The result type returned to the caller.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The generated request message.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="isApiResponse">Whether the result type is an API response wrapper.</param>
    /// <param name="shouldDisposeResponse">Whether the response should be disposed by this helper.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The deserialized or wrapped response.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    public static async Task<T?> SendAsync<T, TBody>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        bool isApiResponse,
        bool shouldDisposeResponse,
        bool bufferBody,
        CancellationToken cancellationToken)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using (request)
        {
            return await RequestExecutionHelpers.SendAndProcessResponseAsync<T, TBody>(
                    client,
                    request,
                    settings,
                    new(
                        isApiResponse,
                        shouldDisposeResponse,
                        bufferBody,
                        true),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Sends a generated request and streams the response as an <see cref="IAsyncEnumerable{T}"/>.</summary>
    /// <typeparam name="T">The element type yielded to the caller.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="request">The generated request message; disposed when streaming completes.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="methodCancellationToken">The cancellation token supplied as a method argument, if any.</param>
    /// <param name="cancellationToken">The token supplied by the consumer's enumeration.</param>
    /// <returns>An asynchronous sequence of deserialized elements.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "The optional CancellationToken carries the [EnumeratorCancellation] token for the await-foreach WithCancellation pattern.")]
    public static async IAsyncEnumerable<T?> StreamAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        RefitSettings settings,
        CancellationToken methodCancellationToken,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        RequestExecutionHelpers.ThrowIfBaseAddressMissing(client);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(methodCancellationToken, cancellationToken);
        await foreach (var item in RequestExecutionHelpers
            .StreamResponseAsync<T>(client, request, settings, true, linked.Token)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>Serializes a generated request body using Refit body rules.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="serializationMethod">The configured body serialization method.</param>
    /// <param name="streamBody">Whether serialized content should be streamed into the request.</param>
    /// <returns>The HTTP content for the body.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by generated callers.")]
    [SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification = "The exception matches existing Refit body-serialization behavior.")]
    public static HttpContent CreateBodyContent<TBody>(
        RefitSettings settings,
        TBody body,
        BodySerializationMethod serializationMethod,
        bool streamBody)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        if (serializationMethod == BodySerializationMethod.Default && body is string stringBody)
        {
            return new StringContent(stringBody);
        }

        var content = CreateSerializedBodyContent(settings, body, serializationMethod);

        // A synchronously-serialized body is already a buffer (and lets the fast-path engage), so never re-stream it.
        return streamBody && !UsesSynchronousSerialization(settings)
            ? new PushStreamContent(
                async (stream, _, _) =>
                {
                    using (stream)
                    {
                        await content.CopyToAsync(stream).ConfigureAwait(false);
                    }
                },
                content.Headers.ContentType)
            : content;
    }

    /// <summary>Serializes a generated request body as JSON Lines (newline-delimited JSON).</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The enumerable body value.</param>
    /// <returns>The HTTP content for the JSON Lines body.</returns>
    public static HttpContent CreateJsonLinesBodyContent<TBody>(
        RefitSettings settings,
        TBody body)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        var items = body is System.Collections.IEnumerable enumerable and not string
            ? enumerable
            : new[] { (object?)body };

        return new JsonLinesContent(items, settings.ContentSerializer);
    }

    /// <summary>Serializes a generated URL-encoded request body using the declared body type.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <returns>The HTTP content for the URL-encoded body.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Generated callers specify the declared body type for AOT-safe form property discovery.")]
    public static HttpContent CreateUrlEncodedBodyContent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TBody>(
        RefitSettings settings,
        TBody body)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        return body is string stringBody
            ? new StringContent(
                StringHelpers.EscapeDataString(stringBody),
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
            : new FormUrlEncodedContent(FormValueMultimap.Create(body, settings));
    }

    /// <summary>Serializes a generated URL-encoded request body using source-generated field descriptors.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="fields">The compile-time field descriptors for the body type.</param>
    /// <returns>The HTTP content for the URL-encoded body.</returns>
    /// <remarks>
    /// The reflection-free path runs only when the configured content serializer resolves field names purely
    /// from attributes the generator already inlined (the built-in <see cref="SystemTextJsonContentSerializer"/>).
    /// For any other serializer the field-name hook may need the runtime <see cref="System.Reflection.PropertyInfo"/>,
    /// so this falls back to the reflection-based <see cref="FormValueMultimap"/>.
    /// </remarks>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Generated callers specify the declared body type for AOT-safe form property discovery.")]
    public static HttpContent CreateUrlEncodedBodyContent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TBody>(
        RefitSettings settings,
        TBody body,
        FormField<TBody>[] fields)
    {
        if (body is HttpContent httpContent)
        {
            return httpContent;
        }

        if (body is Stream stream)
        {
            return new StreamContent(stream);
        }

        if (body is string stringBody)
        {
            return new StringContent(
                StringHelpers.EscapeDataString(stringBody),
                Encoding.UTF8,
                "application/x-www-form-urlencoded");
        }

        // The descriptor path only matches the reflection path when the serializer resolves field names from
        // attributes the generator already inlined (the built-in System.Text.Json serializer); otherwise fall back.
        var useDescriptors = body is not null and not System.Collections.IDictionary
            && settings.ContentSerializer is SystemTextJsonContentSerializer;

        return new FormUrlEncodedContent(
            useDescriptors
                ? FormValueMultimap.CreateFromFields(body, fields, settings)
                : FormValueMultimap.Create(body, settings));
    }

    /// <summary>Sets, replaces, or removes a generated request header.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value, or null to remove the header.</param>
    public static void SetHeader(HttpRequestMessage request, string name, string? value)
    {
        if (ContainsHeader(request.Headers, name))
        {
            _ = request.Headers.Remove(name);
        }

        if (request.Content is not null && ContainsHeader(request.Content.Headers, name))
        {
            _ = request.Content.Headers.Remove(name);
        }

        if (value is null)
        {
            return;
        }

        if (request.Content is null && !IsBodyless(request.Method))
        {
            request.Content = new ByteArrayContent([]);
        }

        name = EnsureSafeHeaderValue(name);
        value = EnsureSafeHeaderValue(value);

        var added = request.Headers.TryAddWithoutValidation(name, value);
        if (added || request.Content is null)
        {
            return;
        }

        _ = request.Content.Headers.TryAddWithoutValidation(name, value);
    }

    /// <summary>Adds a generated request header collection, replacing earlier values by key.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="headers">The header collection argument.</param>
    public static void AddHeaderCollection(
        HttpRequestMessage request,
        IDictionary<string, string>? headers)
    {
        if (headers is null)
        {
            return;
        }

        foreach (var header in headers)
        {
            SetHeader(request, header.Key, header.Value);
        }
    }

    /// <summary>Adds configured request options/properties shared by every generated request.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="interfaceType">The generated interface type.</param>
    public static void AddConfiguredRequestOptions(
        HttpRequestMessage request,
        RefitSettings settings,
        Type interfaceType)
    {
        if (settings.HttpRequestMessageOptions is not null)
        {
            foreach (var option in settings.HttpRequestMessageOptions)
            {
                AddBoxedRequestProperty(request, option.Key, option.Value);
            }
        }

        AddRequestProperty<Type>(request, HttpRequestMessageOptions.InterfaceType, interfaceType);
    }

    /// <summary>Adds one generated request property or option value.</summary>
    /// <typeparam name="TValue">The property value type.</typeparam>
    /// <param name="request">The request to modify.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Generated callers specify the declared property type to avoid call-site boxing.")]
    public static void AddRequestProperty<TValue>(HttpRequestMessage request, string key, TValue value)
    {
#if NET6_0_OR_GREATER
        request.Options.Set(new(key), value);
#else
        request.Properties[key] = value;
#endif
    }

    /// <summary>Adds a parameter to the route, round-tripping segments when required.</summary>
    /// <param name="vsb">The path builder to append to.</param>
    /// <param name="value">The argument value to be added.</param>
    /// <param name="settings">The Refit settings controlling formatting.</param>
    /// <param name="parameterName">Name of the parameter to be appended, used to get the ParameterInfo.</param>
    /// <param name="typeParameters">The types of the parameters of the method.</param>
    /// <param name="roundTripping">If the fragment is round tripping.</param>
    /// <param name="genericCount">The number of generic type parameters of the method.</param>
    /// <param name="callerMethod">Name of the calling method, used to get ParameterInfo.</param>
    /// <typeparam name="TClass">Type of calling methods class, used to get ParameterInfo.</typeparam>
    public static void AddRouteParameter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]TClass>(
                ref ValueStringBuilder vsb,
                object value,
                RefitSettings settings,
                string parameterName,
                Type[] typeParameters,
                bool roundTripping = false,
                int genericCount = 0,
                [CallerMemberName]string callerMethod = ""
                )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callerMethod);
        var parameterInfo = GetParameterInfo(typeof(TClass), callerMethod, parameterName, genericCount, typeParameters);

        if (!roundTripping)
        {
            vsb.Append(StringHelpers.EscapeDataString(
                settings.UrlParameterFormatter.Format(
                    value,
                    parameterInfo!,
                    parameterInfo!.ParameterType) ?? string.Empty));
            return;
        }

        // If round tripping, format each path segment independently.
        var paramValue = (string)value;
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
                    settings.UrlParameterFormatter.Format(
                        section,
                        parameterInfo!,
                        parameterInfo!.ParameterType) ?? string.Empty));
            sectionStart = i + 1;
        }
    }

    /// <summary>Adds an object-property to the route.</summary>
    /// <param name="vsb">The path builder to append to.</param>
    /// <param name="value">The parameter property value to be appended.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="propertyName">The name of the property to be appended.</param>
    /// <typeparam name="TParameter">Type of the parameter, used to get PropertyInfo.</typeparam>
    public static void AddRouteObjectProperty<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]TParameter>(
        ref ValueStringBuilder vsb,
        object value,
        RefitSettings settings,
        string propertyName)
    {
        var propertyInfo = GetPropertyInfo(typeof(TParameter), propertyName);

        vsb.Append(StringHelpers.EscapeDataString(settings.UrlParameterFormatter.Format(
            value,
            propertyInfo,
            propertyInfo.PropertyType) ?? string.Empty));
    }

    /// <summary>Runtime check for unmatched route support.</summary>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="exceptionMessage">Error message for when an unmatched placeholder is not supported.</param>
    public static void UnmatchedRouteParameterGuard(
        RefitSettings settings,
        string exceptionMessage)
    {
        if (settings.AllowUnmatchedRouteParameters)
        {
            return;
        }

        throw new ArgumentException(exceptionMessage);
    }

    /// <summary>Adds one pre-boxed configured request property or option value.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The pre-boxed property value.</param>
    private static void AddBoxedRequestProperty(HttpRequestMessage request, string key, object value)
    {
#if NET6_0_OR_GREATER
        request.Options.Set(new(key), value);
#else
        request.Properties[key] = value;
#endif
    }

    /// <summary>Serializes a non-special body value through the configured content serializer.</summary>
    /// <typeparam name="TBody">The declared body type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="body">The body value.</param>
    /// <param name="serializationMethod">The configured body serialization method.</param>
    /// <returns>The serialized HTTP content.</returns>
    private static HttpContent CreateSerializedBodyContent<TBody>(
        RefitSettings settings,
        TBody body,
        BodySerializationMethod serializationMethod)
    {
        if (serializationMethod is BodySerializationMethod.Default or BodySerializationMethod.Serialized
            || IsObsoleteJsonSerializationMethod(serializationMethod))
        {
            if (settings.ContentSerializer is ISynchronousContentSerializer synchronousSerializer)
            {
                switch (settings.RequestBodySerialization)
                {
                    case RequestBodySerializationMode.Buffered:
                        return synchronousSerializer.ToHttpContentSynchronous(body);
                    case RequestBodySerializationMode.Streamed:
                        return synchronousSerializer.ToStreamingHttpContent(body);
                }
            }

            return settings.ContentSerializer.ToHttpContent(body);
        }

        throw new ArgumentOutOfRangeException(nameof(serializationMethod), serializationMethod, null);
    }

    /// <summary>Determines whether request bodies should be serialized synchronously through the configured serializer.</summary>
    /// <param name="settings">The Refit settings to inspect.</param>
    /// <returns><see langword="true"/> when synchronous body serialization is enabled and supported.</returns>
    private static bool UsesSynchronousSerialization(RefitSettings settings) =>
        settings.RequestBodySerialization != RequestBodySerializationMode.Default
        && settings.ContentSerializer is ISynchronousContentSerializer;

    /// <summary>Determines whether the body should use the legacy JSON enum member.</summary>
    /// <param name="serializationMethod">The body serialization method.</param>
    /// <returns><see langword="true"/> for the legacy JSON value.</returns>
    private static bool IsObsoleteJsonSerializationMethod(BodySerializationMethod serializationMethod) =>
        (int)serializationMethod == ObsoleteJsonBodySerializationMethodValue;

    /// <summary>Determines whether the HTTP method must not carry generated placeholder content for content headers.</summary>
    /// <param name="method">The HTTP method to inspect.</param>
    /// <returns><see langword="true"/> for bodyless methods.</returns>
    private static bool IsBodyless(HttpMethod method) =>
        method == HttpMethod.Get || method == HttpMethod.Head;

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

    /// <summary>Removes CR and LF characters from a generated header name or value.</summary>
    /// <param name="value">The header name or value.</param>
    /// <returns>The sanitized value.</returns>
    private static string EnsureSafeHeaderValue(string value) => StringHelpers.RemoveCrOrLf(value);

    /// <summary>Retrieves the <see cref="ParameterInfo"/> for a specified method parameter, utilizing an internal cache to optimize subsequent lookups.</summary>
    /// <param name="type">The <see cref="Type"/> that contains the method.</param>
    /// <param name="methodName">The name of the method to reflect upon.</param>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <param name="genericCount">The number of generic type parameters of the method.</param>
    /// <param name="typeParameters">The types of the parameters of the method.</param>
    /// <returns>The <see cref="ParameterInfo"/> matching the specified criteria.</returns>
    private static ParameterInfo GetParameterInfo(
        [
            DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type type,
        string methodName,
        string parameterName,
        int genericCount,
        Type[] typeParameters)
    {
        // need to update key.
        var cacheKey = (type, methodName, parameterName);

        if (_parameterCache.TryGetValue(cacheKey, out var cachedParameter))
        {
            return cachedParameter;
        }

        var method = type.GetMethod(
            methodName,
            genericCount,
            typeParameters)
                     ?? throw new UnreachableException($"Method '{methodName}' was not found on type '{type.Name}'.");

        ParameterInfo? parameter = null;
        foreach (var parameterInfo in method.GetParameters())
        {
            if (string.Equals(parameterInfo.Name, parameterName, StringComparison.Ordinal))
            {
                parameter = parameterInfo;
                break;
            }
        }

        if (parameter is null)
        {
            throw new UnreachableException($"Parameter '{parameterName}' was not found on method '{methodName}'.");
        }

        _parameterCache.TryAdd(cacheKey, parameter);
        return parameter;
    }

    /// <summary>Retrieves the <see cref="PropertyInfo"/> for a specified property, utilizing an internal cache to optimize subsequent lookups.</summary>
    /// <param name="type">The <see cref="Type"/> that contains the property.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="PropertyInfo"/> matching the specified criteria.</returns>
    private static PropertyInfo GetPropertyInfo(
        [
            DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type type,
        string propertyName)
    {
        var cacheKey = (type, propertyName);

        if (_propertyCache.TryGetValue(cacheKey, out var cachedParameter))
        {
            return cachedParameter;
        }

        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new UnreachableException($"Property '{propertyName}' was not found on type '{type.Name}'.");

        _propertyCache.TryAdd(cacheKey, property);
        return property;
    }
}
