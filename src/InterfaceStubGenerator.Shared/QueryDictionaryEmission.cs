// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Flattens a dictionary-shaped query parameter into query-string statements.</content>
internal static class QueryDictionaryEmission
{
    /// <summary>Appends the statements turning one dictionary parameter's entries into query pairs.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <remarks>
    /// Mirrors the reflection builder's <c>BuildQueryMap(IDictionary)</c>: entries with a null value are skipped, the
    /// key is rendered by the URL parameter formatter using the key's own <see cref="System.Type"/> as both the
    /// attribute provider and the declared type, a blank key drops the pair, and the value is rendered using the
    /// enclosing parameter's attributes and declared type.
    /// </remarks>
    internal static void AppendDictionaryQueryStatements(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in QueryParameterEmission.InlineValueEmission emission)
    {
        var dictionary = query.Dictionary!;
        var bodyIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation);
        var guarded = parameter.Identity.CanBeNull;
        var indent = guarded ? $"{bodyIndent}    " : bodyIndent;
        var entryLocal = $"{emission.QueryValueLocal}_entry";
        var keyLocal = $"{emission.QueryValueLocal}_key";
        var valueLocal = emission.QueryValueLocal + QueryParameterEmission.ValueLocalSuffix;

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(QueryParameterEmission.IfParameterPrefix).Append(parameter.Identity.Name).AppendLine(QueryParameterEmission.NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        _ = sb.Append(indent).Append(QueryParameterEmission.ForeachVarKeyword).Append(entryLocal).Append(" in @").Append(parameter.Identity.Name).AppendLine(")")
            .Append(indent).AppendLine("{");

        var entryIndent = $"{indent}    ";
        _ = sb.Append(entryIndent).Append("var ").Append(valueLocal).Append(" = ").Append(entryLocal).AppendLine(".Value;");

        var valueIndent = entryIndent;
        if (dictionary.ValueCanBeNull)
        {
            // The reflection builder skips an entry whose value is null before it ever formats the key.
            _ = sb.Append(entryIndent).Append("if (").Append(valueLocal).AppendLine(QueryParameterEmission.NotNullCheckSuffix)
                .Append(entryIndent).AppendLine("{");
            valueIndent = $"{entryIndent}    ";
        }

        var entry = new QueryParameterEmission.DictionaryEntrySite(entryLocal, keyLocal, valueLocal, valueIndent);
        AppendDictionaryEntryStatements(sb, parameter, query, providerField, emission, entry);

        if (dictionary.ValueCanBeNull)
        {
            _ = sb.Append(entryIndent).AppendLine("}");
        }

        _ = sb.Append(indent).AppendLine("}");

        if (!guarded)
        {
            return;
        }

        _ = sb.Append(bodyIndent).AppendLine("}");
    }

    /// <summary>Appends the statements emitting one dictionary entry's query pair.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="entry">The generated locals and indentation for the current entry.</param>
    internal static void AppendDictionaryEntryStatements(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in QueryParameterEmission.InlineValueEmission emission,
        in QueryParameterEmission.DictionaryEntrySite entry)
    {
        var dictionary = query.Dictionary!;
        var (entryLocal, keyLocal, valueLocal, indent) = entry;
        var keyTypeOf = $"typeof({dictionary.KeyTypeName})";
        var customKey = QueryObjectEmission.EmitFormatUrlParameter($"{entryLocal}.Key", keyTypeOf, keyTypeOf, emission);
        var fastKey = QueryValueEmission.BuildFastFormatExpression($"{entryLocal}.Key", dictionary.KeyFormat, emission);
        var keyExpression = fastKey is null
            ? customKey
            : $"{emission.UseDefaultFormattingLocal} ? ({fastKey}) : {customKey}";

        _ = sb.Append(indent).Append("var ").Append(keyLocal).Append(" = ").Append(keyExpression).AppendLine(";")
            .Append(indent).Append("if (!string.IsNullOrWhiteSpace(").Append(keyLocal).AppendLine("))")
            .Append(indent).AppendLine("{");

        var innerIndent = $"{indent}    ";
        if (dictionary.ValueProperties is { } valueProperties)
        {
            AppendDictionaryValueFlatten(sb, parameter, query, providerField, emission, entry, valueProperties);
        }
        else
        {
            var customValue = QueryObjectEmission.BuildUrlFormatterCall(valueLocal, parameter.Identity.Type, providerField, emission);
            var fastValue = QueryValueEmission.BuildFastFormatExpression(valueLocal, query.Rendering.ValueFormat, emission);
            var valueExpression = fastValue is null
                ? customValue
                : $"{emission.UseDefaultFormattingLocal} ? ({fastValue}) : {customValue}";
            var keyArgument = dictionary.PrefixSegment is { } prefix
                ? $"{Emitter.ToCSharpStringLiteral(prefix)} + {keyLocal}"
                : keyLocal;
            _ = sb.Append(innerIndent).Append(emission.QueryBuilderLocal)
                .Append(QueryParameterEmission.AddQueryPairCall).Append(keyArgument).Append(", ").Append(valueExpression).Append(", ")
                .Append(Emitter.ToLowerInvariantString(query.Rendering.PreEncoded)).AppendLine(");");
        }

        _ = sb.Append(indent).AppendLine("}");
    }

    /// <summary>Appends the statements flattening a sealed complex dictionary value under the entry's key.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="entry">The generated locals and indentation for the current entry.</param>
    /// <param name="valueProperties">The flattened property descriptors of the value type.</param>
    /// <remarks>Reuses the query-object flattening walk with the entry key as the runtime parent key, so each value
    /// property emits an <c>entryKey.property=value</c> pair, matching the reflection builder's nested <c>BuildQueryMap</c>.</remarks>
    internal static void AppendDictionaryValueFlatten(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in QueryParameterEmission.InlineValueEmission emission,
        in QueryParameterEmission.DictionaryEntrySite entry,
        QueryObjectPropertyModel[] valueProperties)
    {
        var dictionary = query.Dictionary!;
        var (_, keyLocal, valueLocal, entryIndent) = entry;
        var indent = $"{entryIndent}    ";

        var parentKeyExpression = keyLocal;
        if (dictionary.PrefixSegment is { } prefix)
        {
            var parentKeyLocal = $"{keyLocal}_prefixed";
            _ = sb.Append(indent).Append("var ").Append(parentKeyLocal).Append(" = ")
                .Append(Emitter.ToCSharpStringLiteral(prefix)).Append(" + ").Append(keyLocal).AppendLine(";");
            parentKeyExpression = parentKeyLocal;
        }

        var context = new QueryParameterEmission.QueryObjectContext(
            parameter,
            providerField,
            query.Rendering.CollectionFormatValue,
            Emitter.ToLowerInvariantString(query.Rendering.PreEncoded));
        var scope = new QueryParameterEmission.ObjectFlattenScope(valueLocal, parentKeyExpression, query.NestingDelimiter, QueryParameterEmission.ValueLocalSuffix, indent);
        QueryObjectEmission.AppendObjectPropertyList(sb, context, valueProperties, scope, emission);
    }
}
