// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Refit;

/// <summary>Shared runtime helpers used by source-generated request construction.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static partial class GeneratedRequestRunner
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

        RequireLeadingSlashUnderLegacy(relativePath);
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

        RequireLeadingSlashUnderLegacy(relativePath);
        var basePath = client.BaseAddress?.AbsolutePath
                       ?? throw new InvalidOperationException("BaseAddress must be set on the HttpClient instance");
        basePath = basePath == "/" ? string.Empty : basePath.TrimEnd('/');
        var absolute = new Uri(QueryUriFormatBase, basePath + relativePath);
        return new(absolute.GetComponents(UriComponents.PathAndQuery, queryUriFormat), UriKind.Relative);
    }

    /// <summary>Validates that a <c>[Url]</c> parameter value is an absolute URI, returning its string form as the
    /// base for a full-URL request that bypasses the client's base address. A <see cref="string"/> value is used as
    /// written; a <see cref="Uri"/> value contributes its <see cref="Uri.OriginalString"/>.</summary>
    /// <param name="url">The <c>[Url]</c> parameter value: a <see cref="string"/> or a <see cref="Uri"/>.</param>
    /// <returns>The absolute URI's string form.</returns>
    /// <exception cref="ArgumentException"><paramref name="url"/> is <see langword="null"/>, empty, or not an absolute URI.</exception>
    public static string RequireAbsoluteUrl(object? url)
    {
        var text = url is Uri uri ? uri.OriginalString : url as string;
        return Uri.TryCreate(text, UriKind.Absolute, out _)
            ? text!
            : throw new ArgumentException(FormatAbsoluteUrlError(url), nameof(url));
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
        var sb = new ValueStringBuilder(stackalloc char[256]);
        try
        {
            var pos = 0;
            foreach (var ((startIdx, endIdx), value) in uriParams)
            {
                sb.Append(pathSpan[pos..startIdx]);
                if (value is not null)
                {
                    sb.Append(StringHelpers.EscapeDataString(value));
                }
                else
                {
                    DropOptionalSegmentSeparator(ref sb, pathSpan, endIdx);
                }

                pos = endIdx;
            }

            sb.Append(pathSpan[pos..]);
            return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
        }
        finally
        {
            sb.Dispose();
        }
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

#if NET8_0_OR_GREATER
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
        var sb = new ValueStringBuilder(stackalloc char[256]);
        try
        {
            sb.Append(pathSpan[..range.startIdx]);

            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out var written, format.AsSpan(), System.Globalization.CultureInfo.InvariantCulture))
            {
                StringHelpers.AppendUriDataEscaped(ref sb, buffer[..written]);
            }
            else
            {
                sb.Append(StringHelpers.EscapeDataString(value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)));
            }

            sb.Append(pathSpan[range.endIdx..]);
            return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
        }
        finally
        {
            sb.Dispose();
        }
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
        var sb = new ValueStringBuilder(stackalloc char[256]);
        try
        {
            var pos = 0;
            foreach (var ((startIdx, endIdx), value, preEncoded) in uriParams)
            {
                sb.Append(pathSpan[pos..startIdx]);
                if (value is not null)
                {
                    sb.Append(preEncoded ? value : StringHelpers.EscapeDataString(value));
                }
                else
                {
                    DropOptionalSegmentSeparator(ref sb, pathSpan, endIdx);
                }

                pos = endIdx;
            }

            sb.Append(pathSpan[pos..]);
            return ThrowIfUnmatchedParameter(sb.ToString(), relativePathTemplate, allowUnmatchedParameter);
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>Round-trips a catch-all <c>{**param}</c> path value: each <c>/</c>-separated section is formatted and
    /// escaped while the separators are preserved, matching the reflection request builder.</summary>
    /// <param name="value">The value's string form (from <c>ToString</c>), or null.</param>
    /// <param name="settings">The Refit settings supplying the URL parameter formatter registry and default.</param>
    /// <param name="attributeProvider">The parameter's attribute provider passed to the formatter.</param>
    /// <param name="type">The parameter's declared type passed to the formatter.</param>
    /// <returns>The round-trip-escaped path fragment, ready to append verbatim.</returns>
    public static string RoundTripEscapePath(
        string? value,
        RefitSettings settings,
        ICustomAttributeProvider attributeProvider,
        Type type)
    {
        if (value is null)
        {
            return StringHelpers.EscapeDataString(FormatUrlParameter(settings, null, attributeProvider, type) ?? string.Empty);
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
            _ = sb.Append(StringHelpers.EscapeDataString(FormatUrlParameter(settings, section, attributeProvider, type) ?? string.Empty));
            sectionStart = i + 1;
        }

        return sb.ToString();
    }

    /// <summary>Resolves the <see cref="IUrlParameterFormatter"/> for a value, consulting the per-type registry on the
    /// settings before falling back to the configured default, then formats the value with it.</summary>
    /// <param name="settings">The Refit settings supplying the registry and default formatter.</param>
    /// <param name="value">The value to render into the URL, or null.</param>
    /// <param name="attributeProvider">The attribute provider passed to the formatter.</param>
    /// <param name="type">The declared type passed to the formatter.</param>
    /// <returns>The formatted value, or null when <paramref name="value"/> is null.</returns>
    /// <remarks>This is the single point both the reflection and source-generated request builders call, so a formatter
    /// registered in <see cref="RefitSettings.UrlParameterFormatterMap"/> is applied identically by both. Lookup is by
    /// exact runtime type; every other value uses <see cref="RefitSettings.UrlParameterFormatter"/>.</remarks>
    public static string? FormatUrlParameter(
        RefitSettings settings,
        object? value,
        ICustomAttributeProvider attributeProvider,
        Type type) =>
        ResolveUrlParameterFormatter(settings, value).Format(value, attributeProvider, type);

    /// <summary>Determines whether the settings use the pristine default URL parameter formatter, letting
    /// generated code format statically-known values inline without calling the formatter.</summary>
    /// <param name="settings">The Refit settings to inspect.</param>
    /// <returns><see langword="true"/> when generated inline formatting matches the configured formatter.</returns>
    public static bool UsesDefaultUrlParameterFormatting(RefitSettings settings) =>
        settings.UrlParameterFormatterMap.Count == 0
        && settings.UrlParameterFormatter is DefaultUrlParameterFormatter formatter
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
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
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

        var element = formatting.ElementProviderType;
        if (collectionFormat == CollectionFormat.Multi)
        {
            foreach (var value in values)
            {
                var formatted = FormatUrlParameter(settings, value, element, element);
                builder.Add(key, FormatUrlParameter(settings, formatted, formatting.JoinedProvider, formatting.JoinedType), preEncoded);
            }

            return;
        }

        var joined = JoinFormattedElements(values, settings, element, CollectionDelimiter(collectionFormat));
        builder.Add(key, FormatUrlParameter(settings, joined, formatting.JoinedProvider, formatting.JoinedType), preEncoded);
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

    /// <summary>Adds the configured request options/properties, along with the HTTP version and version policy, shared by every generated request.</summary>
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

#if NET6_0_OR_GREATER
        request.Version = settings.Version;
        request.VersionPolicy = settings.VersionPolicy;
#endif
    }

    /// <summary>Adds one generated request property or option value.</summary>
    /// <typeparam name="TValue">The property value type.</typeparam>
    /// <param name="request">The request to modify.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Generated callers specify the declared property type to avoid call-site boxing.")]
    public static void AddRequestProperty<TValue>(HttpRequestMessage request, string key, TValue value)
    {
#if NET6_0_OR_GREATER
        request.Options.Set(new(key), value);
#else
        request.Properties[key] = value;
#endif
    }

    /// <summary>Serializes a multipart part through the content serializer, wrapping a failure with the same descriptive
    /// argument exception the reflection request builder raises for an unserializable part.</summary>
    /// <typeparam name="T">The declared part type.</typeparam>
    /// <param name="settings">The Refit settings to use.</param>
    /// <param name="value">The part value to serialize.</param>
    /// <param name="fieldName">The multipart field name, named in the failure message.</param>
    /// <returns>The serialized HTTP content.</returns>
    /// <exception cref="ArgumentException">The content serializer could not serialize the value.</exception>
    public static HttpContent SerializeMultipartPart<T>(RefitSettings settings, T value, string fieldName)
    {
        try
        {
            return settings.ContentSerializer.ToHttpContent(value);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"Unexpected parameter type in a Multipart request. Parameter {fieldName} is of type {value?.GetType().Name}, "
                    + "whereas allowed types are String, Stream, FileInfo, Byte array and anything that's JSON serializable",
                nameof(value),
                ex);
        }
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
    /// <param name="settings">The Refit settings supplying the URL parameter formatter registry and default.</param>
    /// <param name="elementProviderType">The declared property type used as the attribute provider and type.</param>
    /// <param name="delimiter">The delimiter between formatted values.</param>
    /// <returns>The joined formatted values, empty when the collection has no elements.</returns>
    [SuppressMessage(
        "Correctness",
        "SST2410:A created disposable is never disposed",
        Justification = "ValueStringBuilder.ToString() disposes the builder and returns its pooled buffer; Dispose is idempotent.")]
    private static string JoinFormattedElements(
        IEnumerable values,
        RefitSettings settings,
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
            builder.Append(FormatUrlParameter(settings, value, elementProviderType, elementProviderType));
        }

        return builder.ToString();
    }

    /// <summary>Drops the '/' preceding an optional <c>{name?}</c> segment whose bound value was null.</summary>
    /// <param name="sb">The path builder written so far, ending in the separator in front of the placeholder.</param>
    /// <param name="template">The relative path template, scanned to detect the optional <c>?}</c> marker.</param>
    /// <param name="endIdx">The placeholder's end offset in the template (one past its closing brace).</param>
    /// <remarks>An optional placeholder ends with <c>?}</c>; a plain <c>{name}</c> ends with <c>}</c>, so a null value
    /// there still renders an empty segment exactly as before. The separator is trimmed only when it is a '/', so a null
    /// value in a query position (for example <c>?key={value?}</c>) collapses to an empty value rather than eating an
    /// unrelated character.</remarks>
    private static void DropOptionalSegmentSeparator(ref ValueStringBuilder sb, ReadOnlySpan<char> template, int endIdx)
    {
        // An optional {name?} placeholder ends with the two-character "?}" suffix, so the '?' sits one char before endIdx.
        const int optionalMarkerSuffixLength = 2;
        var isOptional = endIdx >= optionalMarkerSuffixLength
            && template[endIdx - optionalMarkerSuffixLength] == '?';
        if (!isOptional || sb.Length == 0 || sb[sb.Length - 1] != '/')
        {
            return;
        }

        sb.Length--;
    }

    /// <summary>Resolves the formatter for a value: its <see cref="RefitSettings.UrlParameterFormatterMap"/> entry, else <see cref="RefitSettings.UrlParameterFormatter"/>.</summary>
    /// <param name="settings">The Refit settings supplying the registry and default formatter.</param>
    /// <param name="value">The value about to be formatted, or null.</param>
    /// <returns>The registered formatter for the value's exact runtime type, or the configured default formatter.</returns>
    private static IUrlParameterFormatter ResolveUrlParameterFormatter(RefitSettings settings, object? value) =>
        value is not null
        && settings.UrlParameterFormatterMap.Count > 0
        && settings.UrlParameterFormatterMap.TryGetValue(value.GetType(), out var formatter)
            ? formatter
            : settings.UrlParameterFormatter;

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

    /// <summary>Rejects a no-leading-slash path under legacy resolution, matching the reflection request builder.</summary>
    /// <param name="relativePath">The resolved relative request path.</param>
    /// <exception cref="ArgumentException">The path is non-empty and does not start with '/'.</exception>
    private static void RequireLeadingSlashUnderLegacy(string relativePath)
    {
        if (relativePath.Length == 0 || relativePath[0] == '/')
        {
            return;
        }

        throw new ArgumentException(
            $"URL path {relativePath} must start with '/' and be of the form '/foo/bar/baz'");
    }

    /// <summary>Builds the message describing an invalid <c>[Url]</c> parameter value.</summary>
    /// <param name="value">The rejected value.</param>
    /// <returns>The exception message.</returns>
    private static string FormatAbsoluteUrlError(object? value) =>
        $"The [Url] parameter value \"{value}\" must be an absolute URI (for example \"https://host/path\").";

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
