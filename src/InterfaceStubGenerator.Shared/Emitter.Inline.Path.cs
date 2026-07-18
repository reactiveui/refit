// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits the inline URL path expression and its runtime replacement tuples.</summary>
internal static partial class Emitter
{
    /// <summary>Wraps the path replacement tuples as the collection argument passed to <c>BuildRequestPath</c>.</summary>
    /// <param name="tuples">The comma-separated tuple expressions.</param>
    /// <param name="supportsCollectionExpressions">Whether the consumer supports C# 12 collection expressions.</param>
    /// <returns>The <c>, &lt;collection&gt;</c> argument fragment.</returns>
    /// <remarks>A C# 12 consumer receives a <c>[...]</c> collection expression, which the compiler materializes on the
    /// stack (net8.0+ inline arrays) so no array is allocated; an older consumer receives an inferred array that the same
    /// <c>ReadOnlySpan</c> overload accepts via the array-to-span conversion. The array element type is inferred from the
    /// tuple values rather than stated, so no nullable reference annotation is emitted into a pre-C# 8 consumer.</remarks>
    internal static string WrapPathReplacements(string tuples, bool supportsCollectionExpressions) =>
        supportsCollectionExpressions ? ", [" + tuples + "]" : ", new[] { " + tuples + " }";

    /// <summary>Determines whether any path parameter passes its value through pre-encoded.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when a path parameter carries <c>[Encoded]</c>.</returns>
    internal static bool HasPreEncodedPathParameter(RequestModel request)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Path && parameter.PreEncoded)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Appends one <c>((start, end), value[, preEncoded])</c> tuple to the path replacement argument list.</summary>
    /// <param name="sb">The argument list builder.</param>
    /// <param name="start">The placeholder start offset.</param>
    /// <param name="end">The placeholder end offset.</param>
    /// <param name="valueExpression">The replacement value expression.</param>
    /// <param name="includePreEncoded">Whether the tuple carries the per-value pre-encoded flag.</param>
    /// <param name="preEncoded">The pre-encoded flag value, emitted only when <paramref name="includePreEncoded"/> is set.</param>
    internal static void AppendPathTuple(
        PooledStringBuilder sb,
        int start,
        int end,
        string valueExpression,
        bool includePreEncoded,
        bool preEncoded)
    {
        _ = sb.Append("((").Append(start).Append(", ").Append(end).Append("), ").Append(valueExpression);
        if (includePreEncoded)
        {
            _ = sb.Append(", ").Append(ToLowerInvariantString(preEncoded));
        }

        _ = sb.Append(')');
    }

    /// <summary>Builds the request path expression, preferring the span-formattable fast path when it applies.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="parameterInfoNames">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="parameters">The default path builder argument fragment.</param>
    /// <returns>The generated path expression.</returns>
    internal static string BuildInlinePathExpression(
        RequestModel request,
        Dictionary<string, string> parameterInfoNames,
        in InlineValueEmission emission,
        string settingsLocal,
        string parameters)
    {
        // A template with placeholders but no bound path parameters still runs the unmatched-placeholder
        // check so AllowUnmatchedRouteParameters keeps its reflection-path semantics.
        return TryBuildInlinePathFastExpression(request, parameterInfoNames, emission)
            ?? (parameters.Length > 0 || request.Path.IndexOf('{') >= 0
                ? $"global::Refit.GeneratedRequestRunner.BuildRequestPath({ToCSharpStringLiteral(request.Path)}, {settingsLocal}.AllowUnmatchedRouteParameters{parameters})"
                : ToCSharpStringLiteral(request.Path));
    }

    /// <summary>Builds the allocation-free path expression for a single span-formattable path parameter, or null.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="parameterInfoNames">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The path expression using the span-formattable fast overload, or null to use the default path building.</returns>
    /// <remarks>The default-formatting branch formats the value straight into the path buffer (net6+ integers with no
    /// escaping, net10+ span-escaped values); a customized <c>IUrlParameterFormatter</c> falls back to the string overload.</remarks>
    internal static string? TryBuildInlinePathFastExpression(
        RequestModel request,
        Dictionary<string, string> parameterInfoNames,
        in InlineValueEmission emission)
    {
        RequestParameterModel? pathParameter = null;
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind != RequestParameterKind.Path)
            {
                continue;
            }

            // The single-placeholder fast overloads model one path parameter with one location; anything else falls back.
            if (pathParameter is not null)
            {
                return null;
            }

            pathParameter = parameter;
        }

        if (pathParameter is not { Locations: { Count: 1 } locations, PreEncoded: false, ValueFormat: { } valueFormat }
            || valueFormat.IsNullableValueType
            || (!valueFormat.IsUrlSafeSpanFormattable && !valueFormat.IsSpanFormattableEscapable))
        {
            // A nullable value type keeps the string-formatting path, which null-guards and unwraps .Value itself.
            return null;
        }

        var pathLength = request.Path.Length;
        var location = locations.AsArray()[0];
        var start = location.Start.GetOffset(pathLength);
        var end = location.End.GetOffset(pathLength);
        var template = ToCSharpStringLiteral(request.Path);
        var settingsLocal = emission.SettingsLocal;
        var allowUnmatched = $"{settingsLocal}.AllowUnmatchedRouteParameters";
        var valueExpression = "@" + pathParameter.Name;
        _ = parameterInfoNames.TryGetValue(pathParameter.Name, out var providerField);
        const string runner = "global::Refit.GeneratedRequestRunner.BuildRequestPath";

        var fastExpression = valueFormat.IsUrlSafeSpanFormattable
            ? $"{runner}({template}, {allowUnmatched}, ({start}, {end}), {valueExpression})"
            : $"{runner}({template}, {allowUnmatched}, ({start}, {end}), {valueExpression}, {ToNullableCSharpStringLiteral(valueFormat.Format)})";
        var customTuple =
            $"(({start}, {end}), {EmitFormatUrlParameter(valueExpression, providerField, $"typeof({pathParameter.Type})", emission)})";
        var customReplacements = WrapPathReplacements(customTuple, emission.SupportsCollectionExpressions);
        var customExpression = $"{runner}({template}, {allowUnmatched}{customReplacements})";

        return $"({emission.UseDefaultFormattingLocal} ? {fastExpression} : {customExpression})";
    }
}
