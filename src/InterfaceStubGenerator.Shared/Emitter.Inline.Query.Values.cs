// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Emits the value-formatting expressions for inline query and path parameters.</content>
internal static partial class Emitter
{
    /// <summary>Builds the expression that formats one bound value.</summary>
    /// <param name="valueExpression">The value expression.</param>
    /// <param name="canBeNullAtEvaluation">Whether the value may still be null when this expression runs. The
    /// fast path renders null as null (omitting the value) while the custom formatter always receives the value,
    /// matching the reflection builder's contract for null collection elements and path values.</param>
    /// <param name="parameterTypeName">The declared parameter type passed to the custom formatter.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The generated formatting expression.</returns>
    internal static string BuildFormattedValueExpression(
        string valueExpression,
        bool canBeNullAtEvaluation,
        string parameterTypeName,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        // TreatAsString stringifies the raw value before the formatter runs, mirroring the reflection builder.
        var customValue = query.TreatAsString ? valueExpression + ToStringCall : valueExpression;
        var customExpression =
            EmitFormatUrlParameter(customValue, providerField, $"typeof({parameterTypeName})", emission);

        var fastExpression = query.TreatAsString
            ? valueExpression + ToStringCall
            : BuildFastFormatExpression(valueExpression, query.ValueFormat, emission);
        if (fastExpression is null)
        {
            return customExpression;
        }

        // When the fast path is the value itself (strings), a null value already renders as null.
        if (canBeNullAtEvaluation && fastExpression != valueExpression)
        {
            fastExpression = $"{valueExpression} == null ? null : {fastExpression}";
        }

        return $"{emission.UseDefaultFormattingLocal} ? ({fastExpression}) : {customExpression}";
    }

    /// <summary>Builds the expression that formats one path parameter value.</summary>
    /// <param name="parameter">The path parameter model.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The generated formatting expression.</returns>
    internal static string BuildPathValueExpression(
        in RequestParameterModel parameter,
        string providerField,
        in InlineValueEmission emission)
    {
        if (parameter.IsRoundTrip)
        {
            // A {**param} catch-all: split the value on '/', format and escape each segment, keep the separators.
            var roundTripValue = parameter.CanBeNull ? $"@{parameter.Name}?.ToString()" : $"@{parameter.Name}.ToString()";
            return $"global::Refit.GeneratedRequestRunner.RoundTripEscapePath({roundTripValue}, {emission.SettingsLocal}, {providerField}, typeof({parameter.Type}))";
        }

        return BuildPathValueExpressionCore(
            "@" + parameter.Name,
            parameter.Type,
            parameter.ValueFormat,
            parameter.CanBeNull,
            providerField,
            emission);
    }

    /// <summary>Builds the formatted path value expression for a value accessor, choosing the fast or formatter path.</summary>
    /// <param name="valueAccessor">The C# expression yielding the value (for example <c>@param</c> or <c>@param.Prop</c>).</param>
    /// <param name="typeName">The value's declared type, passed to the URL parameter formatter.</param>
    /// <param name="valueFormat">The reflection-free rendering strategy, or null to always use the formatter.</param>
    /// <param name="canBeNull">Whether the value requires a null guard before the fast path formats it.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The path value expression.</returns>
    internal static string BuildPathValueExpressionCore(
        string valueAccessor,
        string typeName,
        InlineValueFormatModel? valueFormat,
        bool canBeNull,
        string providerField,
        in InlineValueEmission emission)
    {
        var customExpression =
            EmitFormatUrlParameter(valueAccessor, providerField, $"typeof({typeName})", emission);
        var fastExpression = ComputeFastPathExpression(valueAccessor, valueFormat, emission);
        if (fastExpression is null)
        {
            return customExpression;
        }

        // When the fast path is the value itself (strings), a null value already renders as null.
        if (canBeNull && fastExpression != valueAccessor)
        {
            fastExpression = $"{valueAccessor} == null ? null : {fastExpression}";
        }

        return $"{emission.UseDefaultFormattingLocal} ? ({fastExpression}) : {customExpression}";
    }

    /// <summary>Builds the reflection-free fast-path expression for a value, or null to always use the formatter.</summary>
    /// <param name="valueAccessor">The C# expression yielding the value.</param>
    /// <param name="valueFormat">The reflection-free rendering strategy, or null to always use the formatter.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The fast-path expression, or null when no fast path applies.</returns>
    /// <remarks>Path bindings always carry a rendering strategy, so the absent-format arm is only reachable for a path
    /// parameter without one, which the shared fixtures never present.</remarks>
    [ExcludeFromCodeCoverage]
    internal static string? ComputeFastPathExpression(
        string valueAccessor,
        InlineValueFormatModel? valueFormat,
        in InlineValueEmission emission) =>
        valueFormat is null ? null : BuildFastFormatExpression(valueAccessor, valueFormat, emission);

    /// <summary>Determines whether a scalar query value renders straight into the query builder as an
    /// <c>ISpanFormattable</c>, skipping the per-value intermediate string.</summary>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="format">The compile-time format passed to <c>AddFormatted</c>, or null.</param>
    /// <returns><see langword="true"/> when the value has a span-formattable fast write on the consumer target.</returns>
    /// <remarks>Reuses the shared span-formattable tiers computed by the parser: an unformatted URL-safe integer (net6+)
    /// or any span-escapable value (net9+). A <c>TreatAsString</c> value stringifies first and stays on the string path.</remarks>
    internal static bool IsSpanFormattableFast(QueryParameterModel query, out string? format)
    {
        var valueFormat = query.ValueFormat;
        format = valueFormat.Format;
        return !query.TreatAsString
            && valueFormat.Kind == InlineFormatKind.Formattable
            && (valueFormat.IsUrlSafeSpanFormattable || valueFormat.IsSpanFormattableEscapable);
    }

    /// <summary>Determines whether a collection element renders straight into the query builder as an
    /// <c>ISpanFormattable</c>, skipping the per-element intermediate string.</summary>
    /// <param name="query">The query-binding metadata.</param>
    /// <returns><see langword="true"/> when each element has an unformatted span-formattable fast write on the target.</returns>
    /// <remarks><c>AddCollectionValueFormatted</c> takes no format, so a per-element <c>[Query(Format)]</c> keeps the
    /// string-formatted path; only unformatted span-formattable elements qualify.</remarks>
    internal static bool IsCollectionSpanFormattableFast(QueryParameterModel query)
    {
        var valueFormat = query.ValueFormat;
        return !query.TreatAsString
            && valueFormat.Kind == InlineFormatKind.Formattable
            && valueFormat.Format is null
            && !valueFormat.IsNullableValueType
            && (valueFormat.IsUrlSafeSpanFormattable || valueFormat.IsSpanFormattableEscapable);
    }

    /// <summary>Builds the reflection-free fast-path expression for one non-null value.</summary>
    /// <param name="valueExpression">The value expression, evaluated only when non-null.</param>
    /// <param name="valueFormat">The rendering strategy.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The fast-path expression, or <see langword="null"/> when the formatter must always run.</returns>
    internal static string? BuildFastFormatExpression(
        string valueExpression,
        InlineValueFormatModel valueFormat,
        in InlineValueEmission emission)
    {
        var unwrapped = valueFormat.IsNullableValueType ? valueExpression + NullableValueAccess : valueExpression;
        return valueFormat.Kind switch
        {
            InlineFormatKind.String => unwrapped,
            InlineFormatKind.ToStringOnly => unwrapped + ToStringCall,
            InlineFormatKind.Formattable =>
                $"global::Refit.GeneratedRequestRunner.FormatInvariant({unwrapped}, {ToNullableCSharpStringLiteral(valueFormat.Format)})",
            InlineFormatKind.Enum =>
                $"{GetOrAddEnumFormatter(valueFormat, emission)}({unwrapped})",
            _ => null
        };
    }
}
