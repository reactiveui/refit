// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Refit;

/// <summary>Shared runtime helpers used by source-generated request construction.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class GeneratedRequestRunner
{
    /// <summary>The underlying value of the obsolete <c>BodySerializationMethod.Json</c> member.</summary>
    private const int ObsoleteJsonBodySerializationMethodValue = 1;

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
                    new RequestExecutionOptions(
                        isApiResponse,
                        shouldDisposeResponse,
                        bufferBody,
                        true),
                    cancellationToken)
                .ConfigureAwait(false);
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

        if (!streamBody)
        {
            return content;
        }

        return new PushStreamContent(
            async (stream, _, _) =>
            {
                using (stream)
                {
                    await content.CopyToAsync(stream).ConfigureAwait(false);
                }
            },
            content.Headers.ContentType);
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

        if (body is string stringBody)
        {
            return new StringContent(
                StringHelpers.EscapeDataString(stringBody),
                Encoding.UTF8,
                "application/x-www-form-urlencoded");
        }

        return new FormUrlEncodedContent(FormValueMultimap.Create(body, settings));
    }

    /// <summary>Sets, replaces, or removes a generated request header.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value, or null to remove the header.</param>
    public static void SetHeader(HttpRequestMessage request, string name, string? value)
    {
        if (ContainsHeader(request.Headers, name))
        {
            request.Headers.Remove(name);
        }

        if (request.Content is not null && ContainsHeader(request.Content.Headers, name))
        {
            request.Content.Headers.Remove(name);
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

        request.Content.Headers.TryAddWithoutValidation(name, value);
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
            return settings.ContentSerializer.ToHttpContent(body);
        }

        throw new ArgumentOutOfRangeException(nameof(serializationMethod), serializationMethod, null);
    }

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
}
