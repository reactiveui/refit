// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Internal request parser helpers that are directly covered by focused tests.</summary>
internal static partial class Parser
{
    /// <summary>The underlying value for <c>BodySerializationMethod.UrlEncoded</c>.</summary>
    private const int BodySerializationUrlEncoded = 2;

    /// <summary>The underlying value for <c>BodySerializationMethod.Serialized</c>.</summary>
    private const int BodySerializationSerialized = 3;

    /// <summary>The underlying value for <c>BodySerializationMethod.JsonLines</c>.</summary>
    private const int BodySerializationJsonLines = 4;

    /// <summary>Finds the HTTP method attribute on a Refit method.</summary>
    /// <param name="methodSymbol">The method to inspect.</param>
    /// <param name="httpMethodAttribute">The Refit HTTP method base attribute symbol.</param>
    /// <returns>The matching attribute, if any.</returns>
    internal static AttributeData? FindHttpMethodAttribute(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol httpMethodAttribute)
    {
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass!.InheritsFromOrEquals(httpMethodAttribute))
            {
                return attributeData;
            }
        }

        return null;
    }

    /// <summary>Determines whether the initial inline emitter can use the path as a literal URI.</summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> when the path is supported.</returns>
    /// <remarks>A no-leading-slash path is supported: it resolves against the base address under RFC 3986 and throws
    /// under legacy resolution at request time, exactly as the reflection request builder validates it.</remarks>
    internal static bool IsPathSupported(string path)
    {
        return IsPathTemplateValid(path)
            && path.IndexOf('\\') < 0
            && path.IndexOf('\r') < 0
            && path.IndexOf('\n') < 0;
    }

    /// <summary>Prepends the client interface's shared route prefix to a method's relative path.</summary>
    /// <param name="prefix">The shared route prefix, or an empty/whitespace string for a no-op.</param>
    /// <param name="path">The method's relative path template.</param>
    /// <returns>The path with the prefix prepended, joined by exactly one slash; the path unchanged when the prefix is empty.</returns>
    /// <remarks>Kept byte-for-byte identical to the reflection builder's <c>RestMethodInfoInternal.CombineWithPathPrefix</c>
    /// so both request paths stay at parity.</remarks>
    internal static string CombinePathPrefix(string prefix, string path)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return path;
        }

        var trimmedPrefix = prefix.TrimEnd('/');
        if (trimmedPrefix.Length == 0)
        {
            return path;
        }

        var trimmedPath = path.TrimStart('/');
        return trimmedPath.Length == 0
            ? trimmedPrefix
            : trimmedPrefix + "/" + trimmedPath;
    }

    /// <summary>Normalizes constant inline paths to match the reflection request builder URI cleanup.</summary>
    /// <param name="path">The source path from the HTTP method attribute.</param>
    /// <returns>The normalized path used by generated request construction.</returns>
    internal static string NormalizeConstantPathForInline(string path)
    {
        var fragmentIndex = path.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            path = path[..fragmentIndex];
        }

        var queryIndex = path.IndexOf('?');
        if (queryIndex < 0)
        {
            return path;
        }

        var pathOnly = path[..queryIndex];
        if (queryIndex == path.Length - 1)
        {
            return pathOnly;
        }

        var queryStart = queryIndex + 1;
        var partStart = queryStart;
        char[]? queryBuffer = null;
        var queryLength = 0;

        for (var i = queryStart; i <= path.Length; i++)
        {
            if (i < path.Length && path[i] != '&')
            {
                continue;
            }

            var partLength = i - partStart;
            AppendNonEmptyQueryPart(path, queryStart, partStart, partLength, ref queryBuffer, ref queryLength);
            partStart = i + 1;
        }

        return queryBuffer is null
            ? pathOnly
            : $"{pathOnly}?{new string(queryBuffer, 0, queryLength)}";
    }

    /// <summary>Determines whether a string slice is empty or all whitespace.</summary>
    /// <param name="value">The string containing the slice.</param>
    /// <param name="start">The slice start index.</param>
    /// <param name="length">The slice length.</param>
    /// <returns><see langword="true"/> if the slice is empty or all whitespace.</returns>
    internal static bool IsWhiteSpace(string value, int start, int length)
    {
        if (length <= 0)
        {
            return true;
        }

        var end = start + length;
        for (var i = start; i < end; i++)
        {
            if (!char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Adds one static header to the final header list.</summary>
    /// <param name="headers">The mutable header list.</param>
    /// <param name="header">The raw header declaration.</param>
    internal static void AddStaticHeader(List<HeaderModel> headers, string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return;
        }

        var separator = header.IndexOf(':');
        var name = separator >= 0
            ? header[..separator].Trim()
            : header.Trim();
        var value = separator >= 0
            ? header[(separator + 1)..].Trim()
            : null;

        for (var i = 0; i < headers.Count; i++)
        {
            if (!string.Equals(headers[i].Name, name, StringComparison.Ordinal))
            {
                continue;
            }

            headers[i] = new(name, value);
            return;
        }

        headers.Add(new(name, value));
    }

    /// <summary>Tries to parse a body buffered constructor argument.</summary>
    /// <param name="argument">The constructor argument.</param>
    /// <param name="buffered">Receives the buffered value.</param>
    /// <returns><see langword="true"/> when the argument is a boolean buffered argument.</returns>
    internal static bool TryGetBodyBufferedValue(in TypedConstant argument, out bool buffered)
    {
        if (argument is
            {
                Type.SpecialType: SpecialType.System_Boolean,
                Value: bool boolValue
            })
        {
            buffered = boolValue;
            return true;
        }

        buffered = false;
        return false;
    }

    /// <summary>Gets the Refit body serialization enum member name for an underlying value.</summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The enum member name.</returns>
    internal static string GetBodySerializationMethodName(int value) =>
        value switch
        {
            0 => "Default",
            1 => "Json",
            BodySerializationUrlEncoded => "UrlEncoded",
            BodySerializationSerialized => "Serialized",
            BodySerializationJsonLines => "JsonLines",
            _ => string.Empty
        };

    /// <summary>Determines whether all body bindings are supported by the initial inline emitter.</summary>
    /// <param name="parameters">The parsed request parameter models.</param>
    /// <returns><see langword="true"/> when every body binding is supported.</returns>
    internal static bool IsSupportedInlineBody(ImmutableEquatableArray<RequestParameterModel> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Kind != RequestParameterKind.Body)
            {
                continue;
            }

            if (parameter.BodySerializationMethod.Length == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the shared runner should dispose the response.</summary>
    /// <param name="deserializedResultType">The deserialized result type.</param>
    /// <returns><see langword="true"/> when the runner owns response disposal.</returns>
    internal static bool ShouldDisposeResponse(string deserializedResultType) =>
        deserializedResultType is not
            "global::System.Net.Http.HttpResponseMessage" and not
            "global::System.Net.Http.HttpContent" and not
            "global::System.IO.Stream";

    /// <summary>Determines whether the braces in a path template are balanced within each segment.</summary>
    /// <param name="path">The path template to validate.</param>
    /// <returns><see langword="true"/> when every segment has balanced braces.</returns>
    internal static bool IsPathTemplateValid(in ReadOnlySpan<char> path)
    {
        var openingBraces = 0;
        var closingBraces = 0;

        foreach (var c in path)
        {
            switch (c)
            {
                case '/' when openingBraces != closingBraces:
                    return false;
                case '/':
                    {
                        openingBraces = 0;
                        closingBraces = 0;
                        break;
                    }

                case '{':
                    {
                        ++openingBraces;
                        break;
                    }

                case '}':
                    {
                        ++closingBraces;
                        break;
                    }
            }
        }

        return openingBraces == closingBraces;
    }
}
