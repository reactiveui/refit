// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
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

    /// <summary>A dummy absolute base used only to re-encode a relative path and query for <c>[QueryUriFormat]</c>. The
    /// host is irrelevant because <see cref="UriComponents.PathAndQuery"/> ignores it; it mirrors the reflection builder's
    /// own base so the two produce byte-identical output.</summary>
    private static readonly Uri QueryUriFormatBase = new("https://api", UriKind.Absolute);

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

    /// <summary>Builds the relative request URI, re-encoding the whole path and query with a <c>[QueryUriFormat]</c> mode.</summary>
    /// <param name="client">The HTTP client whose base address is used under legacy resolution.</param>
    /// <param name="relativePath">The method's relative path, including any leading slash and query string.</param>
    /// <param name="urlResolution">The configured URL resolution mode.</param>
    /// <param name="queryUriFormat">The escaping mode from the method's <c>[QueryUriFormat]</c> attribute.</param>
    /// <returns>A relative <see cref="Uri"/> whose path and query are re-encoded with <paramref name="queryUriFormat"/>.</returns>
    /// <remarks>Mirrors the reflection request builder: it always assembles the path and query with the escaping query
    /// builder, then re-encodes the whole thing through <see cref="Uri.GetComponents(UriComponents, UriFormat)"/> with the
    /// method's <c>QueryUriFormat</c> (so <see cref="UriFormat.Unescaped"/> decodes it). Rfc3986 resolution ignores the
    /// format, exactly as the reflection builder does.</remarks>
    public static Uri BuildRelativeUri(HttpClient client, string relativePath, UrlResolutionMode urlResolution, UriFormat queryUriFormat)
    {
        if (urlResolution == UrlResolutionMode.Rfc3986)
        {
            return new(relativePath, UriKind.Relative);
        }

        var basePath = client.BaseAddress?.AbsolutePath
                       ?? throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");
        basePath = basePath == "/" ? string.Empty : basePath.TrimEnd('/');
        var absolute = new Uri(QueryUriFormatBase, basePath + relativePath);
        return new(absolute.GetComponents(UriComponents.PathAndQuery, queryUriFormat), UriKind.Relative);
    }

    /// <summary>Builds the request path for a generated request from a template.</summary>
    /// <param name="relativePathTemplate">The method's relative path, including any leading slash and query string.</param>
    /// <param name="allowUnmatchedParameter">Whether to allow unmatched URL parameters.</param>
    /// <param name="uriParams">The replacement uri parameters, ordered by template position.</param>
    /// <returns>A path with all the placeholder parameters in the path template replaced.</returns>
    /// <exception cref="ArgumentException">
    /// A URI template parameter is not available in the provided parameter span and unmatched URL parameters aren't allowed.
    /// </exception>
    /// <remarks>Generated call sites pass the replacements as a collection expression. On C# 12 and a runtime with inline
    /// array support (net8.0+) that materializes on the stack, so any number of path parameters is expanded without a heap
    /// allocation; older consumers pass a small array via the same signature.</remarks>
    public static string BuildRequestPath(
        string relativePathTemplate,
        bool allowUnmatchedParameter,
        ReadOnlySpan<((int startIdx, int endIdx) range, string? value)> uriParams)
    {
        if (uriParams.IsEmpty && allowUnmatchedParameter)
        {
            return relativePathTemplate;
        }

        var pathSpan = relativePathTemplate.AsSpan();
        using var sb = new ValueStringBuilder(stackalloc char[256]);
        var pos = 0;
        foreach (var ((startIdx, endIdx), value) in uriParams)
        {
            sb.Append(pathSpan[pos..startIdx]);
            if (value is not null)
            {
                sb.Append(StringHelpers.EscapeDataString(value));
            }

            pos = endIdx;
        }

        sb.Append(pathSpan[pos..]);
        return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
    }

#if NET6_0_OR_GREATER
    /// <summary>Builds a single-placeholder request path, formatting an unformatted integer straight into the path
    /// buffer with no intermediate string and no escaping.</summary>
    /// <typeparam name="T">The integer value type.</typeparam>
    /// <param name="relativePathTemplate">The method's relative path, including any leading slash and query string.</param>
    /// <param name="allowUnmatchedParameter">Whether to allow unmatched URL parameters.</param>
    /// <param name="range">The replacement range for the placeholder.</param>
    /// <param name="value">The integer value to render.</param>
    /// <returns>A path with the placeholder replaced.</returns>
    /// <remarks>The generator emits this only for an unformatted integer parameter, whose invariant rendering is digits
    /// and an optional leading <c>-</c> - all URL-unreserved - so the formatted span is appended without escaping.</remarks>
    public static string BuildRequestPath<T>(
        string relativePathTemplate,
        bool allowUnmatchedParameter,
        (int startIdx, int endIdx) range,
        T value)
        where T : ISpanFormattable
    {
        var pathSpan = relativePathTemplate.AsSpan();
        using var sb = new ValueStringBuilder(stackalloc char[256]);
        sb.Append(pathSpan[..range.startIdx]);

        // long.MinValue and ulong.MaxValue both render in 20 characters, so 32 always succeeds for an integer.
        Span<char> buffer = stackalloc char[32];
        if (value.TryFormat(buffer, out var written, default, System.Globalization.CultureInfo.InvariantCulture))
        {
            sb.Append(buffer[..written]);
        }
        else
        {
            sb.Append(StringHelpers.EscapeDataString(value.ToString(null, System.Globalization.CultureInfo.InvariantCulture)));
        }

        sb.Append(pathSpan[range.endIdx..]);
        return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
    }
#endif

#if NET9_0_OR_GREATER
    /// <summary>Builds a single-placeholder request path, formatting an <see cref="ISpanFormattable"/> value into a stack
    /// buffer and escaping the span directly, so the intermediate formatted string is never allocated.</summary>
    /// <typeparam name="T">The span-formattable value type.</typeparam>
    /// <param name="relativePathTemplate">The method's relative path, including any leading slash and query string.</param>
    /// <param name="allowUnmatchedParameter">Whether to allow unmatched URL parameters.</param>
    /// <param name="range">The replacement range for the placeholder.</param>
    /// <param name="value">The value to render.</param>
    /// <param name="format">The compile-time format from <c>[Query(Format = ...)]</c>, or null.</param>
    /// <returns>A path with the placeholder replaced.</returns>
    public static string BuildRequestPath<T>(
        string relativePathTemplate,
        bool allowUnmatchedParameter,
        (int startIdx, int endIdx) range,
        T value,
        string? format)
        where T : ISpanFormattable
    {
        var pathSpan = relativePathTemplate.AsSpan();
        using var sb = new ValueStringBuilder(stackalloc char[256]);
        sb.Append(pathSpan[..range.startIdx]);

        Span<char> buffer = stackalloc char[128];
        if (value.TryFormat(buffer, out var written, format.AsSpan(), System.Globalization.CultureInfo.InvariantCulture))
        {
            sb.Append(Uri.EscapeDataString((ReadOnlySpan<char>)buffer[..written]));
        }
        else
        {
            sb.Append(StringHelpers.EscapeDataString(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)));
        }

        sb.Append(pathSpan[range.endIdx..]);
        return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
    }
#endif

    /// <summary>Validates a parameterless request path template, throwing for unmatched placeholders.</summary>
    /// <param name="relativePathTemplate">The method's relative path, including any leading slash and query string.</param>
    /// <param name="allowUnmatchedParameter">Whether to allow unmatched URL parameters.</param>
    /// <returns>The template, unchanged.</returns>
    /// <exception cref="ArgumentException">
    /// The template contains a placeholder and unmatched URL parameters aren't allowed.
    /// </exception>
    public static string BuildRequestPath(string relativePathTemplate, bool allowUnmatchedParameter) =>
        ThrowIfUnmatchedParameter(relativePathTemplate, relativePathTemplate, allowUnmatchedParameter);

    /// <summary>Builds the request path for a generated request from a template, honoring per-value encoding opt-outs.</summary>
    /// <param name="relativePathTemplate">The method's relative path, including any leading slash and query string.</param>
    /// <param name="allowUnmatchedParameter">Whether to allow unmatched URL parameters.</param>
    /// <param name="uriParams">The replacement uri parameters, ordered by template position; a <c>preEncoded</c> value is appended verbatim.</param>
    /// <returns>A path with all the placeholder parameters in the path template replaced.</returns>
    /// <exception cref="ArgumentException">
    /// A URI template parameter is not available in the provided parameter span and unmatched URL parameters aren't allowed.
    /// </exception>
    /// <remarks>Generated call sites pass the replacements as a collection expression, stack-allocated on C# 12 with a
    /// net8.0+ runtime and passed as a small array otherwise.</remarks>
    public static string BuildRequestPath(
        string relativePathTemplate,
        bool allowUnmatchedParameter,
        ReadOnlySpan<((int startIdx, int endIdx) range, string? value, bool preEncoded)> uriParams)
    {
        if (uriParams.IsEmpty && allowUnmatchedParameter)
        {
            return relativePathTemplate;
        }

        var pathSpan = relativePathTemplate.AsSpan();
        using var sb = new ValueStringBuilder(stackalloc char[256]);
        var pos = 0;
        foreach (var ((startIdx, endIdx), value, preEncoded) in uriParams)
        {
            sb.Append(pathSpan[pos..startIdx]);
            if (value is not null)
            {
                sb.Append(preEncoded ? value : StringHelpers.EscapeDataString(value));
            }

            pos = endIdx;
        }

        sb.Append(pathSpan[pos..]);
        return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
    }

    /// <summary>Round-trips a catch-all <c>{**param}</c> path value: each <c>/</c>-separated section is formatted and
    /// escaped while the separators are preserved, matching the reflection request builder.</summary>
    /// <param name="value">The value's string form (from <c>ToString</c>), or null.</param>
    /// <param name="formatter">The configured URL parameter formatter.</param>
    /// <param name="attributeProvider">The parameter's attribute provider passed to the formatter.</param>
    /// <param name="type">The parameter's declared type passed to the formatter.</param>
    /// <returns>The round-trip-escaped path fragment, ready to append verbatim.</returns>
    public static string RoundTripEscapePath(
        string? value,
        IUrlParameterFormatter formatter,
        ICustomAttributeProvider attributeProvider,
        Type type)
    {
        if (value is null)
        {
            return StringHelpers.EscapeDataString(formatter.Format(null, attributeProvider, type) ?? string.Empty);
        }

        var sb = new StringBuilder(value.Length);
        var sectionStart = 0;
        for (var i = 0; i <= value.Length; i++)
        {
            if (i != value.Length && value[i] != '/')
            {
                continue;
            }

            if (sectionStart > 0)
            {
                _ = sb.Append('/');
            }

            var section = value.Substring(sectionStart, i - sectionStart);
            _ = sb.Append(StringHelpers.EscapeDataString(formatter.Format(section, attributeProvider, type) ?? string.Empty));
            sectionStart = i + 1;
        }

        return sb.ToString();
    }

    /// <summary>Determines whether the settings use the pristine default URL parameter formatter, letting
    /// generated code format statically-known values inline without calling the formatter.</summary>
    /// <param name="settings">The Refit settings to inspect.</param>
    /// <returns><see langword="true"/> when generated inline formatting matches the configured formatter.</returns>
    public static bool UsesDefaultUrlParameterFormatting(RefitSettings settings) =>
        settings.UrlParameterFormatter is DefaultUrlParameterFormatter formatter
        && formatter.IsPristineDefault;

    /// <summary>Determines whether the settings use the pristine default form-url-encoded parameter formatter.</summary>
    /// <param name="settings">The Refit settings to inspect.</param>
    /// <returns><see langword="true"/> when generated inline formatting matches the configured formatter.</returns>
    /// <remarks>
    /// A property-level <c>[Query(Format = ...)]</c> on a flattened query object is applied by
    /// <see cref="IFormUrlEncodedParameterFormatter"/>, not <see cref="IUrlParameterFormatter"/>. Its default renders
    /// <c>string.Format(InvariantCulture, "{0:format}", enumMemberValue ?? value)</c>, which is what generated inline
    /// formatting reproduces. <c>Format</c> is virtual, so a derived formatter must disable the fast path.
    /// </remarks>
    public static bool UsesDefaultFormUrlEncodedParameterFormatting(RefitSettings settings) =>
        settings.FormUrlEncodedParameterFormatter.GetType() == typeof(DefaultFormUrlEncodedParameterFormatter);

    /// <summary>Determines whether the settings use the pristine default URL parameter key formatter, letting
    /// generated code use a compile-time constant query key instead of calling the formatter.</summary>
    /// <param name="settings">The Refit settings to inspect.</param>
    /// <returns><see langword="true"/> when a property's CLR name is its query key verbatim.</returns>
    public static bool UsesDefaultUrlParameterKeyFormatting(RefitSettings settings) =>
        settings.UrlParameterKeyFormatter.GetType() == typeof(DefaultUrlParameterKeyFormatter);

    /// <summary>Composes the query key for a property flattened out of a query object.</summary>
    /// <param name="settings">The Refit settings supplying the key formatter.</param>
    /// <param name="clrName">The declared CLR property name.</param>
    /// <param name="explicitName">The name from <c>[AliasAs]</c> or <c>[JsonPropertyName]</c>, which bypasses the key formatter, or <see langword="null"/>.</param>
    /// <param name="prefixSegment">The compile-time <c>prefix + delimiter</c> from <c>[Query(Prefix = ...)]</c>, or <see langword="null"/>.</param>
    /// <returns>The query key.</returns>
    /// <remarks>
    /// Matches the reflection request builder's <c>BuildPropertyQueryKey</c>: an explicit <c>[AliasAs]</c> or
    /// <c>[JsonPropertyName]</c> name (resolved by the generator and passed as <paramref name="explicitName"/>) is used
    /// verbatim; otherwise the CLR name passes through <see cref="IUrlParameterKeyFormatter"/>.
    /// </remarks>
    public static string BuildQueryKey(
        RefitSettings settings,
        string clrName,
        string? explicitName,
        string? prefixSegment)
    {
        var name = explicitName
            ?? (UsesDefaultUrlParameterKeyFormatting(settings)
                ? clrName
                : settings.UrlParameterKeyFormatter.Format(clrName));

        return prefixSegment is null ? name : prefixSegment + name;
    }

    /// <summary>Formats a value with the invariant culture, matching the default URL parameter formatter's
    /// rendering for <see cref="IFormattable"/> values without boxing or reflection.</summary>
    /// <typeparam name="T">The formattable value type.</typeparam>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The compile-time format from <c>[Query(Format = ...)]</c>, or null.</param>
    /// <returns>The formatted value.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally inferred from generated call sites to avoid boxing.")]
    public static string FormatInvariant<T>(T value, string? format)
        where T : IFormattable =>
        value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Expands a query-object collection property through a customized <see cref="IUrlParameterFormatter"/>,
    /// reproducing the reflection request builder's two formatting passes.</summary>
    /// <param name="builder">The query-string builder to append to.</param>
    /// <param name="settings">The Refit settings supplying the URL parameter formatter.</param>
    /// <param name="values">The collection value; a null collection appends nothing.</param>
    /// <param name="key">The already-composed query key.</param>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <param name="preEncoded">Whether the key and values are caller-encoded and appended verbatim.</param>
    /// <param name="formatting">The two-pass formatting targets: the declared property type used as both the attribute
    /// provider and type for each element (matching the reflection builder's <c>propertyInfo.PropertyType</c> element
    /// pass), and the enclosing parameter's attribute provider and declared type for the second pass.</param>
    /// <remarks>
    /// Each element is formatted with the property's provider and type; the results are joined (or, under
    /// <see cref="CollectionFormat.Multi"/>, kept separate) and formatted again with the parameter's provider and type.
    /// A pristine default formatter makes the second pass a no-op, so generated code takes this slow path only when the
    /// formatter is customized; the fast path uses <see cref="GeneratedQueryStringBuilder"/> directly.
    /// </remarks>
    public static void AddFormattedCollectionProperty(
        ref GeneratedQueryStringBuilder builder,
        RefitSettings settings,
        IEnumerable? values,
        string key,
        CollectionFormat collectionFormat,
        bool preEncoded,
        (Type ElementProviderType, ICustomAttributeProvider JoinedProvider, Type JoinedType) formatting)
    {
        if (values is null)
        {
            return;
        }

        var formatter = settings.UrlParameterFormatter;
        var element = formatting.ElementProviderType;
        if (collectionFormat == CollectionFormat.Multi)
        {
            foreach (var value in values)
            {
                var formatted = formatter.Format(value, element, element);
                builder.Add(key, formatter.Format(formatted, formatting.JoinedProvider, formatting.JoinedType), preEncoded);
            }

            return;
        }

        var joined = JoinFormattedElements(values, formatter, element, CollectionDelimiter(collectionFormat));
        builder.Add(key, formatter.Format(joined, formatting.JoinedProvider, formatting.JoinedType), preEncoded);
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

    /// <summary>Sends a generated request as a cold <see cref="IObservable{T}"/>: each subscription rebuilds and sends
    /// the request, mirroring the reflection request builder.</summary>
    /// <typeparam name="T">The result type yielded to subscribers.</typeparam>
    /// <typeparam name="TBody">The deserialized body type for API response wrappers.</typeparam>
    /// <param name="client">The HTTP client to send with.</param>
    /// <param name="requestFactory">Builds a fresh request per subscription, so a second subscription never reuses a
    /// disposed request.</param>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="isApiResponse">Whether the result type is an API response wrapper.</param>
    /// <param name="shouldDisposeResponse">Whether the response should be disposed by this helper.</param>
    /// <param name="bufferBody">Whether request content should be buffered before sending.</param>
    /// <param name="methodCancellationToken">The cancellation token supplied as a method argument, if any.</param>
    /// <returns>A cold observable of the deserialized or wrapped response.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameters intentionally specified explicitly by generated callers.")]
    public static IObservable<T?> SendObservable<T, TBody>(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        RefitSettings settings,
        bool isApiResponse,
        bool shouldDisposeResponse,
        bool bufferBody,
        CancellationToken methodCancellationToken) =>
        new ReactiveUI.Primitives.Advanced.FromAsyncSignal<T?>(async subscriptionToken =>
        {
            // Link the method's CancellationToken argument (if any) with the per-subscription token, allocating a linked
            // source only when both can cancel - mirroring StreamAsync.
            CancellationTokenSource? linked = null;
            CancellationToken token;
            if (methodCancellationToken.CanBeCanceled && subscriptionToken.CanBeCanceled)
            {
                linked = CancellationTokenSource.CreateLinkedTokenSource(methodCancellationToken, subscriptionToken);
                token = linked.Token;
            }
            else
            {
                token = methodCancellationToken.CanBeCanceled ? methodCancellationToken : subscriptionToken;
            }

            try
            {
                return await SendAsync<T, TBody>(client, requestFactory(), settings, isApiResponse, shouldDisposeResponse, bufferBody, token)
                    .ConfigureAwait(false);
            }
            finally
            {
                linked?.Dispose();
            }
        });

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

        // Only allocate a linked source when both tokens can actually cancel; linking a non-cancelable token is a
        // no-op, so when the method has no CancellationToken parameter or the consumer enumerates without
        // WithCancellation the request runs against whichever token can cancel (or none) with no CTS allocation.
        CancellationTokenSource? linked = null;
        CancellationToken token;
        if (methodCancellationToken.CanBeCanceled && cancellationToken.CanBeCanceled)
        {
            linked = CancellationTokenSource.CreateLinkedTokenSource(methodCancellationToken, cancellationToken);
            token = linked.Token;
        }
        else
        {
            token = methodCancellationToken.CanBeCanceled ? methodCancellationToken : cancellationToken;
        }

        try
        {
            await foreach (var item in RequestExecutionHelpers
                               .StreamResponseAsync<T>(client, request, settings, true, token)
                               .ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            linked?.Dispose();
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
#if NET8_0_OR_GREATER
                    await using (stream.ConfigureAwait(false))
#else
                    using (stream)
#endif
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

        var items = body is IEnumerable enumerable and not string
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

    /// <summary>Determines whether a form body can be serialized by the generated straight-line unrolled fast path.</summary>
    /// <param name="body">The body instance.</param>
    /// <returns><see langword="true"/> when the body is a plain object the unrolled path can flatten field-by-field;
    /// <see langword="false"/> for the <see langword="null"/>, <see cref="HttpContent"/>, <see cref="Stream"/>,
    /// <see cref="string"/>, and <see cref="System.Collections.IDictionary"/> bodies the reflection path special-cases.</returns>
    /// <remarks>The <see cref="NotNullWhenAttribute"/> lets generated code dereference the body directly inside the guard.</remarks>
    public static bool CanUnrollForm([NotNullWhen(true)] object? body) =>
        body is not null
        && body is not HttpContent
        && body is not Stream
        && body is not string
        && body is not System.Collections.IDictionary;

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

    /// <summary>Determines whether the body should use the legacy JSON enum member.</summary>
    /// <param name="serializationMethod">The body serialization method.</param>
    /// <returns><see langword="true"/> for the legacy JSON value.</returns>
    /// <remarks>Compares the underlying value so callers never name the obsolete member and raise CS0618.</remarks>
    internal static bool IsObsoleteJsonSerializationMethod(BodySerializationMethod serializationMethod) =>
        (int)serializationMethod == ObsoleteJsonBodySerializationMethodValue;

    /// <summary>Resolves the single-character delimiter for a non-multi collection format.</summary>
    /// <param name="collectionFormat">The collection format.</param>
    /// <returns>The delimiter character.</returns>
    private static char CollectionDelimiter(CollectionFormat collectionFormat) =>
        collectionFormat switch
        {
            CollectionFormat.Ssv => ' ',
            CollectionFormat.Tsv => '\t',
            CollectionFormat.Pipes => '|',
            _ => ','
        };

    /// <summary>Formats each element with the property's provider and type and joins them with the delimiter.</summary>
    /// <param name="values">The collection value.</param>
    /// <param name="formatter">The URL parameter formatter.</param>
    /// <param name="elementProviderType">The declared property type used as the attribute provider and type.</param>
    /// <param name="delimiter">The delimiter between formatted values.</param>
    /// <returns>The joined formatted values, empty when the collection has no elements.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2930:\"IDisposables\" should be disposed",
        Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
    private static string JoinFormattedElements(
        IEnumerable values,
        IUrlParameterFormatter formatter,
        Type elementProviderType,
        char delimiter)
    {
        var builder = new ValueStringBuilder(stackalloc char[256]);
        var first = true;
        foreach (var value in values)
        {
            if (!first)
            {
                builder.Append(delimiter);
            }

            first = false;
            builder.Append(formatter.Format(value, elementProviderType, elementProviderType));
        }

        return builder.ToString();
    }

    /// <summary>Throws when the expanded path still contains a placeholder and unmatched parameters are not allowed.</summary>
    /// <param name="path">The expanded request path to validate.</param>
    /// <param name="relativePathTemplate">The original path template, used in the error message.</param>
    /// <param name="allowUnmatchedParameter">Whether to allow unmatched URL parameters.</param>
    /// <returns>The validated path, returned unchanged.</returns>
    private static string ThrowIfUnmatchedParameter(string path, string relativePathTemplate, bool allowUnmatchedParameter)
    {
        var i = path.IndexOf('{');
        if (i < 0 || allowUnmatchedParameter)
        {
            return path;
        }

        var j = path.AsSpan(i).IndexOfAny('}', '/');
        if (j < 0 || path[j += i] != '}')
        {
            return path;
        }

        var key = path[(i + 1)..j];
        throw new ArgumentException(
            $"URL {relativePathTemplate} has parameter {{{key}}}, but no method parameter matches");
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
                    default:
                    {
                        // Default (and any undeclared value) falls through to the async serializer below.
                        break;
                    }
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
#if NET6_0_OR_GREATER
        // NonValidated checks key presence (case-insensitively, like the store) without parsing or materializing the
        // stored header values, and never throws for unsupported header shapes, so it preserves the tolerant behavior
        // of the manual scan while avoiding the per-check value enumeration.
        return headers.NonValidated.Contains(name);
#else
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
#endif
    }

    /// <summary>Removes CR and LF characters from a generated header name or value.</summary>
    /// <param name="value">The header name or value.</param>
    /// <returns>The sanitized value.</returns>
    private static string EnsureSafeHeaderValue(string value) => StringHelpers.RemoveCrOrLf(value);
}
