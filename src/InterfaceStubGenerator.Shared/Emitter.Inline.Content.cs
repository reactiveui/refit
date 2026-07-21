// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits the inline request content assignment, including the form-body unroll fast path.</summary>
internal static partial class Emitter
{
    /// <summary>Builds request content assignment for an inline generated method.</summary>
    /// <param name="bodyParameter">The body parameter model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="formFieldsFieldName">The cached form field descriptor array name, or null to use the reflection path.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="locals">The method-scope unique local name builder.</param>
    /// <returns>The generated content assignment.</returns>
    internal static string BuildInlineContent(
        in RequestParameterModel bodyParameter,
        string requestLocal,
        string settingsLocal,
        string? formFieldsFieldName,
        bool supportsNullable,
        in InlineValueEmission emission,
        UniqueNameBuilder locals)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        if (bodyParameter.BodySerializationMethod == "UrlEncoded")
        {
            if (IsUnrollableFormBody(bodyParameter))
            {
                return BuildInlineFormUnroll(bodyParameter, requestLocal, supportsNullable, emission, locals);
            }

            return formFieldsFieldName is not null
                ? $$"""
                    {{bodyIndent}}{{requestLocal}}.Content = global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<{{bodyParameter.Type}}>(
                    {{bodyIndent}}    {{settingsLocal}},
                    {{bodyIndent}}    @{{bodyParameter.Name}},
                    {{bodyIndent}}    {{formFieldsFieldName}});

                    """
                : $$"""
                    {{bodyIndent}}{{requestLocal}}.Content = global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<{{bodyParameter.Type}}>(
                    {{bodyIndent}}    {{settingsLocal}},
                    {{bodyIndent}}    @{{bodyParameter.Name}});

                    """;
        }

        if (bodyParameter.BodySerializationMethod == "JsonLines")
        {
            return $$"""
                {{bodyIndent}}{{requestLocal}}.Content = global::Refit.GeneratedRequestRunner.CreateJsonLinesBodyContent<{{bodyParameter.Type}}>(
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    @{{bodyParameter.Name}});

                """;
        }

        var streamBodyExpression = BuildStreamBodyExpression(bodyParameter, settingsLocal);
        var serializationMethodExpression = BuildBodySerializationMethodExpression(bodyParameter);

        return $$"""
            {{bodyIndent}}{{requestLocal}}.Content = global::Refit.GeneratedRequestRunner.CreateBodyContent<{{bodyParameter.Type}}>(
            {{bodyIndent}}    {{settingsLocal}},
            {{bodyIndent}}    @{{bodyParameter.Name}},
            {{bodyIndent}}    {{serializationMethodExpression}},
            {{bodyIndent}}    {{streamBodyExpression}});

            """;
    }

    /// <summary>Emits straight-line form-url-encoded body serialization for an all-scalar body, mirroring the descriptor
    /// path's wire output without the descriptor array, getter delegates, or value boxing on the fast path.</summary>
    /// <param name="bodyParameter">The URL-encoded body parameter model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="locals">The method-scope unique local name builder.</param>
    /// <returns>The generated content assignment.</returns>
    internal static string BuildInlineFormUnroll(
        in RequestParameterModel bodyParameter,
        string requestLocal,
        bool supportsNullable,
        in InlineValueEmission emission,
        UniqueNameBuilder locals)
    {
        var settingsLocal = emission.SettingsLocal;
        var bodyIndent = Indent(MethodBodyIndentation);
        var inner = $"{bodyIndent}    ";
        var fields = bodyParameter.FormFields!.Value.AsArray();
        var bodyExpr = $"@{bodyParameter.Name}";
        var entriesLocal = locals.New("______formEntries");

        // Nullable reference annotations are a C# 8 feature; older consumers get the unannotated types, which also match
        // the .NET Framework/netstandard FormUrlEncodedContent constructor signature. The generated code stays
        // compilable down to the C# 7.3 floor (explicit KeyValuePair construction, != null guards - no C# 9 syntax).
        var nullable = supportsNullable ? "?" : string.Empty;
        var kvpType = $"global::System.Collections.Generic.KeyValuePair<string{nullable}, string{nullable}>";
        var site = new FormUnrollSite(bodyExpr, entriesLocal, inner, $"new {kvpType}", locals);

        var adds = new PooledStringBuilder();
        foreach (var field in fields)
        {
            AppendFormFieldUnroll(adds, field, in site, emission);
        }

        // CanUnrollForm rejects the null, HttpContent, Stream, string, and dictionary bodies the reflection path
        // special-cases; a non-System.Text.Json serializer resolves field names differently, so it falls back too.
        return $$"""
            {{bodyIndent}}if ({{settingsLocal}}.ContentSerializer is global::Refit.SystemTextJsonContentSerializer
            {{inner}}    && global::Refit.GeneratedRequestRunner.CanUnrollForm({{bodyExpr}}))
            {{bodyIndent}}{
            {{inner}}var {{entriesLocal}} = new global::System.Collections.Generic.List<{{kvpType}}>({{fields.Length}});
            {{adds}}{{inner}}{{requestLocal}}.Content = new global::System.Net.Http.FormUrlEncodedContent({{entriesLocal}});
            {{bodyIndent}}}
            {{bodyIndent}}else
            {{bodyIndent}}{
            {{inner}}{{requestLocal}}.Content = global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<{{bodyParameter.Type}}>({{settingsLocal}}, {{bodyExpr}});
            {{bodyIndent}}}

            """;
    }

    /// <summary>Appends the statements adding one scalar field to the unrolled form entry list.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="field">The form field descriptor.</param>
    /// <param name="site">The shared locals and rendered fragments for the enclosing body.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendFormFieldUnroll(
        PooledStringBuilder sb,
        FormFieldModel field,
        in FormUnrollSite site,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var valueLocal = site.Locals.New("______formValue");
        var propertyNameLiteral = ToCSharpStringLiteral(field.PropertyName);
        var explicitNameLiteral = ToNullableCSharpStringLiteral(field.ExplicitName);
        var prefixSegmentLiteral = ToNullableCSharpStringLiteral(field.PrefixSegment);
        var keyExpr =
            $"global::Refit.GeneratedRequestRunner.BuildQueryKey({emission.SettingsLocal}, {propertyNameLiteral}, {explicitNameLiteral}, {prefixSegmentLiteral})";

        _ = sb.Append(indent).Append("var ").Append(valueLocal).Append(" = ").Append(site.BodyExpr).Append(".@").Append(field.PropertyName).AppendLine(";");

        var valueExpr = BuildFormFieldValueExpression(field, valueLocal, emission);

        // A non-nullable value type is always present, so it is added unconditionally.
        if (!field.CanBeNull)
        {
            AppendFormEntryAdd(sb, in site, indent, keyExpr, valueExpr);
            return;
        }

        // "!= null" (not the C# 9 "is not null" pattern) keeps the emitted null guard compilable down to C# 7.3.
        var childIndent = $"{indent}    ";
        _ = sb.Append(indent).Append("if (").Append(valueLocal).AppendLine(" != null)")
            .Append(indent).AppendLine("{");
        AppendFormEntryAdd(sb, in site, childIndent, keyExpr, valueExpr);
        _ = sb.Append(indent).AppendLine("}");

        // A null value is omitted unless the field opts in via [Query(SerializeNull = true)], which emits an empty value.
        if (!field.SerializeNull)
        {
            return;
        }

        _ = sb.Append(indent).AppendLine("else")
            .Append(indent).AppendLine("{");
        AppendFormEntryAdd(sb, in site, childIndent, keyExpr, "string.Empty");
        _ = sb.Append(indent).AppendLine("}");
    }

    /// <summary>Appends one entry-list <c>Add</c> using an explicit <c>KeyValuePair</c> construction (no C# 9 target-typed new).</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="site">The shared locals and rendered fragments for the enclosing body.</param>
    /// <param name="indent">The statement indentation.</param>
    /// <param name="keyExpr">The field key expression.</param>
    /// <param name="valueExpr">The field value expression.</param>
    internal static void AppendFormEntryAdd(PooledStringBuilder sb, in FormUnrollSite site, string indent, string keyExpr, string valueExpr) =>
        _ = sb.Append(indent).Append(site.EntriesLocal).Append(".Add(").Append(site.KvpNew)
            .Append('(').Append(keyExpr).Append(", ").Append(valueExpr).AppendLine("));");

    /// <summary>Builds the value expression for one scalar form field, matching the configured form formatter.</summary>
    /// <param name="field">The form field descriptor.</param>
    /// <param name="valueLocal">The non-null value local name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The rendering expression, branching to the formatter when it is customized.</returns>
    internal static string BuildFormFieldValueExpression(
        FormFieldModel field,
        string valueLocal,
        in InlineValueEmission emission)
    {
        var formatterExpression =
            $"{emission.SettingsLocal}.FormUrlEncodedParameterFormatter.Format({valueLocal}, {ToNullableCSharpStringLiteral(field.Format)})";
        var fastExpression = field.ValueFormat!.Kind == InlineFormatKind.FormatterOnly
            ? null
            : BuildFastFormatExpression(valueLocal, field.ValueFormat, emission);

        return fastExpression is null
            ? formatterExpression
            : $"{emission.UseDefaultFormFormattingLocal} ? ({fastExpression}) : {formatterExpression}";
    }

    /// <summary>Determines whether a URL-encoded body can be serialized by the straight-line unrolled fast path.</summary>
    /// <param name="bodyParameter">The body parameter model, or null when the method has no body.</param>
    /// <returns><see langword="true"/> when every field is a simple scalar carrying a compile-time rendering strategy,
    /// so the body needs neither the descriptor array nor reflection on the common System.Text.Json path.</returns>
    internal static bool IsUnrollableFormBody(RequestParameterModel? bodyParameter)
    {
        if (bodyParameter is not { BodySerializationMethod: "UrlEncoded", FormFields: { Count: > 0 } formFields })
        {
            return false;
        }

        // A collection or complex field leaves ValueFormat null; it needs the descriptor path's collection-format and
        // nested handling, so the whole body falls back rather than the generator guessing the wire format.
        foreach (var field in formFields)
        {
            if (field.ValueFormat is null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether an unrollable form body has at least one field with a reflection-free fast path.</summary>
    /// <param name="bodyParameter">The body parameter model, or null when the method has no body.</param>
    /// <returns><see langword="true"/> when a field renders through the default-form-formatting branch, so the generated
    /// method must declare that branch local.</returns>
    internal static bool FormBodyHasFastPath(RequestParameterModel? bodyParameter)
    {
        if (!IsUnrollableFormBody(bodyParameter))
        {
            return false;
        }

        foreach (var field in bodyParameter!.Value.FormFields!)
        {
            if (field.ValueFormat!.Kind != InlineFormatKind.FormatterOnly)
            {
                return true;
            }
        }

        return false;
    }
}
