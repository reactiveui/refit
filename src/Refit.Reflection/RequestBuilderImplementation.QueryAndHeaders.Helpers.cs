// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Refit;

/// <summary>Internal query and header helpers exposed to focused tests.</summary>
internal partial class RequestBuilderImplementation
{
    /// <summary>Determines whether a value should be emitted directly rather than expanded into a query map.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> if the value is a simple/formattable type; otherwise <see langword="false"/>.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:UnrecognizedReflectionPattern",
        Justification = "Query-map formatting only checks implemented enumerable interfaces on the runtime value.")]
    internal static bool DoNotConvertToQueryMap(object value)
    {
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
        Type? intType = null;
        var interfaces = type.GetInterfaces();
        for (var i = 0; i < interfaces.Length; i++)
        {
            var interfaceType = interfaces[i];
            if (interfaceType.GetTypeInfo().IsGenericType
                && interfaceType.GetGenericTypeDefinition() == ienu)
            {
                intType = interfaceType;
                break;
            }
        }

        if (intType is null)
        {
            return false;
        }

        type = intType.GetGenericArguments()[0];
        return ShouldReturn(type);
    }

    /// <summary>Sets or replaces a header on the request or its content, with CRLF-injection protection.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value, or null to only remove the header.</param>
    /// <param name="validateHeaders">
    /// When <see langword="true"/> the value is added with <see cref="System.Net.Http.Headers.HttpHeaders.Add(string, string?)"/>
    /// so a malformed value throws <see cref="FormatException"/>; when <see langword="false"/> it is added verbatim with
    /// <c>TryAddWithoutValidation</c>. CR/LF stripping applies in both modes.
    /// </param>
    internal static void SetHeader(HttpRequestMessage request, string name, string? value, bool validateHeaders)
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

        // CRLF injection protection
        name = EnsureSafe(name);
        value = EnsureSafe(value);
        ApplyHeaderValue(request, name, value, validateHeaders);
    }

    /// <summary>Applies a sanitized header value to the request or its content, validating it when requested.</summary>
    /// <param name="request">The request to modify.</param>
    /// <param name="name">The sanitized header name.</param>
    /// <param name="value">The sanitized header value.</param>
    /// <param name="validateHeaders">Whether the value is validated (<c>Add</c>) or added verbatim (<c>TryAddWithoutValidation</c>).</param>
    /// <exception cref="FormatException">Validation is enabled and the value is malformed for the header's parser.</exception>
    private static void ApplyHeaderValue(HttpRequestMessage request, string name, string value, bool validateHeaders)
    {
        if (validateHeaders)
        {
            // Add validates the value against the header's parser, surfacing malformed values as FormatException.
            if (TryAddValidated(request.Headers, name, value) || request.Content is null)
            {
                return;
            }

            request.Content.Headers.Add(name, value);
            return;
        }

        var added = request.Headers.TryAddWithoutValidation(name, value);

        // Don't even bother trying to add the header as a content header
        // if we just added it to the other collection.
        if (added || request.Content is null)
        {
            return;
        }

        _ = request.Content.Headers.TryAddWithoutValidation(name, value);
    }

    /// <summary>Adds a header with framework validation, reporting whether it belongs to this collection.</summary>
    /// <param name="headers">The header collection to add to.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns><see langword="true"/> when the header was stored; <see langword="false"/> when it belongs on the
    /// content collection instead, mirroring the <c>false</c> return of <c>TryAddWithoutValidation</c>.</returns>
    /// <exception cref="FormatException">The value is malformed for this header's parser.</exception>
    private static bool TryAddValidated(System.Net.Http.Headers.HttpHeaders headers, string name, string value)
    {
        try
        {
            headers.Add(name, value);
            return true;
        }
        catch (InvalidOperationException)
        {
            // The header name belongs on the request's content headers, not this collection; a malformed value would
            // instead surface as FormatException, which is allowed to propagate.
            return false;
        }
    }

    /// <summary>Determines whether a type is a simple string or <see cref="IFormattable"/> type.</summary>
    /// <param name="type">The type to inspect; a nullable value type is unwrapped first.</param>
    /// <returns><see langword="true"/> if the type formats directly into a query value.</returns>
    [ExcludeFromCodeCoverage] // The CultureInfo query-value arm needs a settable-collection value that SST2305 forbids, so the branch cannot be covered by a test.
    private static bool ShouldReturn(Type type) =>
        Nullable.GetUnderlyingType(type) is { } underlyingType
            ? ShouldReturn(underlyingType)
            : type == typeof(string)
              || type == typeof(bool)
              || type == typeof(char)
              || typeof(IFormattable).IsAssignableFrom(type)
              || type == typeof(Uri)
              || typeof(CultureInfo).IsAssignableFrom(type);
}
