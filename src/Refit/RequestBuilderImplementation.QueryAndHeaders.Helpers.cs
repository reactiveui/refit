// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Refit
{
    /// <summary>Internal query and header helpers exposed to focused tests.</summary>
    internal partial class RequestBuilderImplementation
    {
        /// <summary>Determines whether a value should be emitted directly rather than expanded into a query map.</summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns><see langword="true"/> if the value is a simple/formattable type; otherwise <see langword="false"/>.</returns>
        [RequiresUnreferencedCode("Refit's reflection-based request building is not trim-safe; use the Refit source generator for trimmed/AOT apps.")]
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
        internal static void SetHeader(HttpRequestMessage request, string name, string? value)
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
    }
}
