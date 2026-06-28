// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Internal request parser helpers that are directly covered by focused tests.</summary>
internal static partial class Parser
{
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
            if (attributeData.AttributeClass?.InheritsFromOrEquals(httpMethodAttribute) == true)
            {
                return attributeData;
            }
        }

        return null;
    }

    /// <summary>Determines whether the initial inline emitter can use the path as a literal URI.</summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> when the path is supported.</returns>
    internal static bool IsConstantPathSupported(string path) =>
        (path.Length == 0 || path[0] == '/')
        && path.IndexOf('\\') < 0
        && path.IndexOf('\r') < 0
        && path.IndexOf('\n') < 0;

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
}
