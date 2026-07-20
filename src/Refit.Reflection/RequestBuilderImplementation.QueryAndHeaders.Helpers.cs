// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Internal query and header helpers exposed to focused tests.</summary>
internal partial class RequestBuilderImplementation
{
    /// <summary>Caches, per enumerable type, whether its element type is emitted directly, so the interface scan runs
    /// once per type without keeping collectible types alive.</summary>
    private static readonly ConditionalWeakTable<Type, StrongBox<bool>> EnumerableEmitsDirectlyCache = new();

    /// <summary>Reuses one delegate for the classification factory so classifying never allocates a callback.</summary>
    private static readonly ConditionalWeakTable<Type, StrongBox<bool>>.CreateValueCallback EnumerableEmitsDirectlyFactory =
        ClassifyEnumerableElement;

    /// <summary>The shared "emit directly" classification result.</summary>
    private static readonly StrongBox<bool> EmitDirectly = new(true);

    /// <summary>The shared "expand into a query map" classification result.</summary>
    private static readonly StrongBox<bool> ExpandIntoQueryMap = new(false);

    /// <summary>Determines whether a value should be emitted directly rather than expanded into a query map.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> if the value is a simple/formattable type; otherwise <see langword="false"/>.</returns>
    internal static bool DoNotConvertToQueryMap(object value)
    {
        var type = value.GetType();

        // Scalars and non-enumerable objects are the common case and stay allocation-free and cache-free (matching a
        // string or any IFormattable early); only enumerables reach the cached interface scan.
        return ShouldReturn(type)
            || (value is IEnumerable
                && EnumerableEmitsDirectlyCache.GetValue(type, EnumerableEmitsDirectlyFactory).Value);
    }

    /// <summary>Classifies an enumerable type by whether its <see cref="IEnumerable{T}"/> element type is emitted
    /// directly, reading its interfaces once so the per-request path never re-scans them.</summary>
    /// <param name="type">The runtime enumerable type to classify.</param>
    /// <returns>The shared classification result.</returns>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070:UnrecognizedReflectionPattern",
        Justification = "Query-map formatting only checks implemented enumerable interfaces on the runtime value.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:UnrecognizedReflectionPattern",
        Justification = "Query-map formatting only checks implemented enumerable interfaces on the runtime value.")]
    internal static StrongBox<bool> ClassifyEnumerableElement(Type type)
    {
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
            return ExpandIntoQueryMap;
        }

        return ShouldReturn(intType.GetGenericArguments()[0]) ? EmitDirectly : ExpandIntoQueryMap;
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
        HttpHeaderApplier.Apply(request, name, value, validateHeaders);
    }

    /// <summary>Determines whether a type is a simple string or <see cref="IFormattable"/> type.</summary>
    /// <param name="type">The type to inspect; a nullable value type is unwrapped first.</param>
    /// <returns><see langword="true"/> if the type formats directly into a query value.</returns>
    [ExcludeFromCodeCoverage] // The CultureInfo query-value arm needs a settable-collection value that SST2305 forbids, so the branch cannot be covered by a test.
    internal static bool ShouldReturn(Type type) =>
        Nullable.GetUnderlyingType(type) is { } underlyingType
            ? ShouldReturn(underlyingType)
            : type == typeof(string)
              || type == typeof(bool)
              || type == typeof(char)
              || typeof(IFormattable).IsAssignableFrom(type)
              || type == typeof(Uri)
              || typeof(CultureInfo).IsAssignableFrom(type);
}
