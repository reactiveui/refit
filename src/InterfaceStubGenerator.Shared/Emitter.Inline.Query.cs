// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Query string and value-formatting emission for the inline path.</content>
internal static partial class Emitter
{
    /// <summary>The base member name for a generated enum formatting helper.</summary>
    private const string EnumFormatterBaseName = "______FormatEnumValue";

    /// <summary>The base member name for a cached query-converter instance field.</summary>
    private const string ConverterFieldBaseName = "______queryConverter";

    /// <summary>The tail of a generated non-null guard, closing the <c>if (</c> opened by the caller.</summary>
    private const string NotNullCheckSuffix = " != null)";

    /// <summary>The head of a generated guard on a method parameter, whose name is escaped with <c>@</c>.</summary>
    private const string IfParameterPrefix = "if (@";

    /// <summary>The generated call appending one query pair to the query-string builder.</summary>
    private const string AddQueryPairCall = ".Add(";

    /// <summary>The start of a generated <c>foreach (var …)</c> loop over a collection or dictionary.</summary>
    private const string ForeachVarKeyword = "foreach (var ";

    /// <summary>The <c>RefitSettings</c> property gating serializer-aware query key naming.</summary>
    private const string HonorSerializerNamesFlag = "HonorContentSerializerPropertyNamesInQuery";

    /// <summary>Determines whether any parameter binds to the query string.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when query emission is required.</returns>
    private static bool HasQueryBindings(RequestModel request)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Query is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether the generated method needs the default-formatting branch local.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when at least one bound value has a reflection-free fast path.</returns>
    private static bool NeedsFormattingLocal(RequestModel request)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Path
                && parameter.ValueFormat is { Kind: not InlineFormatKind.FormatterOnly })
            {
                return true;
            }

            if (parameter.Query is not { } query)
            {
                continue;
            }

            // A flattened object's own ValueFormat is unused; each property carries its own rendering strategy.
            var needsLocal = query switch
            {
                // A converter formats its own values, so the generated method needs no default-formatting branch.
                { Converter: not null } => false,
                { ObjectProperties: { } properties } => ObjectPropertiesNeedFormattingLocal(properties),
                { Dictionary: { } dictionary } =>
                    dictionary.KeyFormat.Kind != InlineFormatKind.FormatterOnly
                    || query.ValueFormat.Kind != InlineFormatKind.FormatterOnly,
                _ => query.TreatAsString || query.ValueFormat.Kind != InlineFormatKind.FormatterOnly
            };
            if (needsLocal)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether any flattened property branches on the default-formatting local.</summary>
    /// <param name="properties">The flattened property descriptors.</param>
    /// <returns><see langword="true"/> when at least one property has a reflection-free fast path or a format.</returns>
    private static bool ObjectPropertiesNeedFormattingLocal(ImmutableEquatableArray<QueryObjectPropertyModel> properties)
    {
        foreach (var property in properties)
        {
            if (property.Nested is { } nested)
            {
                if (ObjectPropertiesNeedFormattingLocal(nested))
                {
                    return true;
                }

                continue;
            }

            // A collection property branches on the local to pick the inline or double-pass rendering; a formatted or
            // inline-renderable scalar (or a dictionary with an inline-renderable key) branches on the URL formatter.
            if (property.Collection is not null
                || property.PropertyFormat is not null
                || property.ValueFormat.Kind != InlineFormatKind.FormatterOnly
                || property.Dictionary is { KeyFormat.Kind: not InlineFormatKind.FormatterOnly })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a parameter needs a generated attribute-provider field.</summary>
    /// <param name="parameter">The parameter model.</param>
    /// <returns><see langword="true"/> when formatting may consult the parameter's attributes at runtime.</returns>
    private static bool NeedsAttributeProvider(RequestParameterModel parameter) =>
        parameter.Kind == RequestParameterKind.Path
        || parameter.Query is { Shape: not QueryParameterShape.Converter };

    /// <summary>Determines whether the generated method needs the default-form-formatting branch local.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when a flattened query-object property carries a compile-time format.</returns>
    private static bool NeedsFormFormattingLocal(RequestModel request)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Query?.ObjectProperties is { } properties && ObjectPropertiesHaveFormat(properties))
            {
                return true;
            }

            // An unrolled form body reads its default-form-formatting branch when any field has a fast path.
            if (parameter.Kind == RequestParameterKind.Body && FormBodyHasFastPath(parameter))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether any flattened property (recursing into nested objects) carries a compile-time format.</summary>
    /// <param name="properties">The flattened property descriptors.</param>
    /// <returns><see langword="true"/> when at least one property carries a <c>[Query(Format)]</c>.</returns>
    private static bool ObjectPropertiesHaveFormat(ImmutableEquatableArray<QueryObjectPropertyModel> properties)
    {
        foreach (var property in properties)
        {
            if (property.Nested is { } nested)
            {
                if (ObjectPropertiesHaveFormat(nested))
                {
                    return true;
                }

                continue;
            }

            if (property.PropertyFormat is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the query-appending statements for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="parameterInfoNames">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The generated query statements.</returns>
    private static string BuildInlineQueryStatements(
        RequestModel request,
        Dictionary<string, string> parameterInfoNames,
        in InlineValueEmission emission)
    {
        var sb = new PooledStringBuilder();
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Query is not { } query)
            {
                continue;
            }

            // A converter parameter needs no attribute provider (it formats its own values), so it may be absent.
            _ = parameterInfoNames.TryGetValue(parameter.Name, out var providerField);
            switch (query.Shape)
            {
                case QueryParameterShape.Scalar or QueryParameterShape.Flag:
                {
                    AppendScalarQueryStatement(sb, parameter, query, providerField, emission);
                    break;
                }

                case QueryParameterShape.Object:
                {
                    AppendObjectQueryStatements(sb, parameter, query, providerField, emission);
                    break;
                }

                case QueryParameterShape.Dictionary:
                {
                    AppendDictionaryQueryStatements(sb, parameter, query, providerField, emission);
                    break;
                }

                case QueryParameterShape.Converter:
                {
                    AppendConverterQueryStatements(sb, parameter, query, emission);
                    break;
                }

                default:
                {
                    AppendCollectionQueryStatement(sb, parameter, query, providerField, emission);
                    break;
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>Appends the statement emitting one scalar query value or flag.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendScalarQueryStatement(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);

        // Scalar values are guarded by the outer null check, so the value is never null when formatted.
        var valueExpression = BuildFormattedValueExpression(
            "@" + parameter.Name,
            canBeNullAtEvaluation: false,
            parameter.Type,
            query,
            providerField,
            emission);

        if (parameter.CanBeNull)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{")
                .Append(bodyIndent).Append("    ");
            AppendScalarAddCall(sb, query, valueExpression, emission);
            _ = sb.Append(bodyIndent).AppendLine("}");
            return;
        }

        _ = sb.Append(bodyIndent);
        AppendScalarAddCall(sb, query, valueExpression, emission);
    }

    /// <summary>Appends the query-builder add/addflag call for a scalar value, writing each piece straight into the
    /// builder instead of composing an intermediate interpolated statement string.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="valueExpression">The formatted value expression.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendScalarAddCall(
        PooledStringBuilder sb,
        QueryParameterModel query,
        string valueExpression,
        in InlineValueEmission emission)
    {
        _ = sb.Append(emission.QueryBuilderLocal);
        if (query.Shape == QueryParameterShape.Flag)
        {
            _ = sb.Append(".AddFlag(").Append(valueExpression).Append(", ")
                .Append(ToLowerInvariantString(query.PreEncoded)).AppendLine(");");
            return;
        }

        _ = sb.Append(".Add(").Append(ToCSharpStringLiteral(query.Key)).Append(", ").Append(valueExpression)
            .Append(", ").Append(ToLowerInvariantString(query.PreEncoded)).AppendLine(");");
    }

    /// <summary>Appends the statements emitting one collection-valued query parameter or flag set.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendCollectionQueryStatement(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var elementExpression = BuildFormattedValueExpression(
            emission.QueryValueLocal,
            query.ElementCanBeNull,
            parameter.Type,
            query,
            providerField,
            emission);
        var isFlag = query.Shape == QueryParameterShape.FlagCollection;
        var guarded = parameter.CanBeNull;
        var loopIndent = guarded ? bodyIndent + "    " : bodyIndent;

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        if (!isFlag)
        {
            // Write the BeginCollection call straight into the builder instead of composing interpolated fragments.
            _ = sb.Append(loopIndent).Append(emission.QueryBuilderLocal).Append(".BeginCollection(")
                .Append(ToCSharpStringLiteral(query.Key)).Append(", ");
            if (query.CollectionFormatValue is { } collectionFormatValue)
            {
                _ = sb.Append(CollectionFormatCast).Append(collectionFormatValue);
            }
            else
            {
                _ = sb.Append(emission.SettingsLocal).Append(".CollectionFormat");
            }

            _ = sb.Append(", ").Append(ToLowerInvariantString(query.PreEncoded)).AppendLine(");");
        }

        _ = sb.Append(loopIndent).Append(ForeachVarKeyword).Append(emission.QueryValueLocal).Append(" in @").Append(parameter.Name).AppendLine(")")
            .Append(loopIndent).AppendLine("{")
            .Append(loopIndent).Append("    ").Append(emission.QueryBuilderLocal);
        if (isFlag)
        {
            _ = sb.Append(".AddFlag(").Append(elementExpression).Append(", ")
                .Append(ToLowerInvariantString(query.PreEncoded)).AppendLine(");");
        }
        else
        {
            _ = sb.Append(".AddCollectionValue(").Append(elementExpression).AppendLine(");");
        }

        _ = sb.Append(loopIndent).AppendLine("}");

        if (!isFlag)
        {
            _ = sb.Append(loopIndent).Append(emission.QueryBuilderLocal).AppendLine(".EndCollection();");
        }

        if (!guarded)
        {
            return;
        }

        _ = sb.Append(bodyIndent).AppendLine("}");
    }

    /// <summary>Appends the statement delegating one converter-bound parameter to its <c>IQueryConverter&lt;T&gt;</c>.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendConverterQueryStatements(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        in InlineValueEmission emission)
    {
        var converter = query.Converter!;
        var bodyIndent = Indent(MethodBodyIndentation);
        var guarded = parameter.CanBeNull;
        var indent = guarded ? bodyIndent + "    " : bodyIndent;
        var converterField = GetOrAddConverterField(converter.ConverterTypeName, emission);

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        _ = sb.Append(indent).Append(converterField).Append(".Flatten(@").Append(parameter.Name).Append(", ")
            .Append(ToCSharpStringLiteral(converter.KeyPrefix)).Append(", ref ").Append(emission.QueryBuilderLocal)
            .Append(", ").Append(emission.SettingsLocal).AppendLine(");");

        if (!guarded)
        {
            return;
        }

        _ = sb.Append(bodyIndent).AppendLine("}");
    }

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
    private static void AppendDictionaryQueryStatements(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var dictionary = query.Dictionary!;
        var bodyIndent = Indent(MethodBodyIndentation);
        var guarded = parameter.CanBeNull;
        var indent = guarded ? bodyIndent + "    " : bodyIndent;
        var entryLocal = emission.QueryValueLocal + "_entry";
        var keyLocal = emission.QueryValueLocal + "_key";
        var valueLocal = emission.QueryValueLocal + "_value";

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        _ = sb.Append(indent).Append(ForeachVarKeyword).Append(entryLocal).Append(" in @").Append(parameter.Name).AppendLine(")")
            .Append(indent).AppendLine("{");

        var entryIndent = indent + "    ";
        _ = sb.Append(entryIndent).Append("var ").Append(valueLocal).Append(" = ").Append(entryLocal).AppendLine(".Value;");

        var valueIndent = entryIndent;
        if (dictionary.ValueCanBeNull)
        {
            // The reflection builder skips an entry whose value is null before it ever formats the key.
            _ = sb.Append(entryIndent).Append("if (").Append(valueLocal).AppendLine(NotNullCheckSuffix)
                .Append(entryIndent).AppendLine("{");
            valueIndent = entryIndent + "    ";
        }

        var entry = new DictionaryEntrySite(entryLocal, keyLocal, valueLocal, valueIndent);
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
    private static void AppendDictionaryEntryStatements(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission,
        in DictionaryEntrySite entry)
    {
        var dictionary = query.Dictionary!;
        var (entryLocal, keyLocal, valueLocal, indent) = entry;
        var keyTypeOf = $"typeof({dictionary.KeyTypeName})";
        var customKey =
            $"{emission.SettingsLocal}.UrlParameterFormatter.Format({entryLocal}.Key, {keyTypeOf}, {keyTypeOf})";
        var fastKey = BuildFastFormatExpression(entryLocal + ".Key", dictionary.KeyFormat, emission);
        var keyExpression = fastKey is null
            ? customKey
            : $"{emission.UseDefaultFormattingLocal} ? ({fastKey}) : {customKey}";

        var customValue = BuildUrlFormatterCall(valueLocal, parameter.Type, providerField, emission);
        var fastValue = BuildFastFormatExpression(valueLocal, query.ValueFormat, emission);
        var valueExpression = fastValue is null
            ? customValue
            : $"{emission.UseDefaultFormattingLocal} ? ({fastValue}) : {customValue}";

        var keyArgument = dictionary.PrefixSegment is { } prefix
            ? $"{ToCSharpStringLiteral(prefix)} + {keyLocal}"
            : keyLocal;

        _ = sb.Append(indent).Append("var ").Append(keyLocal).Append(" = ").Append(keyExpression).AppendLine(";")
            .Append(indent).Append("if (!string.IsNullOrWhiteSpace(").Append(keyLocal).AppendLine("))")
            .Append(indent).AppendLine("{")
            .Append(indent).Append("    ").Append(emission.QueryBuilderLocal)
            .Append(AddQueryPairCall).Append(keyArgument).Append(", ").Append(valueExpression).Append(", ")
            .Append(ToLowerInvariantString(query.PreEncoded)).AppendLine(");")
            .Append(indent).AppendLine("}");
    }

    /// <summary>Appends the statements flattening one query object's properties into query pairs.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <remarks>
    /// Mirrors the reflection builder's per-property contract: a null value is omitted unless
    /// <c>[Query(SerializeNull = true)]</c> emits a bare <c>key=</c>; a property-level format runs through the
    /// form-url-encoded formatter first and omits the pair when it yields null; and the surviving value is rendered by
    /// the URL parameter formatter, which receives the enclosing <em>parameter's</em> attributes and declared type.
    /// </remarks>
    private static void AppendObjectQueryStatements(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var guarded = parameter.CanBeNull;
        var indent = guarded ? bodyIndent + "    " : bodyIndent;

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        var context = new QueryObjectContext(
            parameter,
            providerField,
            query.CollectionFormatValue,
            ToLowerInvariantString(query.PreEncoded));
        var scope = new ObjectFlattenScope("@" + parameter.Name, null, query.NestingDelimiter, string.Empty, indent);
        AppendObjectPropertyList(sb, context, query.ObjectProperties!, scope, emission);

        if (!guarded)
        {
            return;
        }

        _ = sb.Append(bodyIndent).AppendLine("}");
    }

    /// <summary>Appends the statements for a list of flattened properties, recursing into nested objects.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <param name="properties">The flattened property descriptors at this nesting level.</param>
    /// <param name="scope">The access expression, parent key, delimiter, local suffix and indentation.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendObjectPropertyList(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        ImmutableEquatableArray<QueryObjectPropertyModel> properties,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        foreach (var property in properties)
        {
            var valueLocal = emission.QueryValueLocal + scope.LocalSuffix + "_" + property.ClrName;
            var keyExpression = scope.ParentKeyExpr is { } parentKey
                ? BuildNestedKeyExpression(property, parentKey, scope.Delimiter, emission)
                : BuildQueryObjectKeyExpression(property, emission);
            var site = new QueryPropertySite(valueLocal, keyExpression, context.PreEncoded, scope.Indentation + "    ");

            if (property.Nested is { } children)
            {
                AppendNestedObjectProperty(sb, context, property, children, site, scope, emission);
            }
            else
            {
                AppendObjectLeafProperty(sb, context, property, site, scope, emission);
            }
        }
    }

    /// <summary>Appends the statements emitting one non-nested flattened query-object property.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="scope">The access expression and indentation for this nesting level.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendObjectLeafProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        in QueryPropertySite site,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        _ = sb.Append(scope.Indentation).AppendLine("{")
            .Append(site.Indentation).Append("var ").Append(site.ValueLocal).Append(" = ").Append(scope.AccessExpr)
            .Append('.').Append(property.ClrName).AppendLine(";");

        if (property.Dictionary is { } dictionary)
        {
            AppendObjectDictionaryProperty(sb, context, property, dictionary, site, scope, emission);
        }
        else if (property.Collection is { } collection)
        {
            AppendObjectQueryCollectionProperty(sb, context, property, collection, site, emission);
        }
        else if (property.CanBeNull)
        {
            AppendNullableObjectQueryProperty(sb, context.Parameter, property, site, context.ProviderField, emission);
        }
        else
        {
            AppendObjectQueryPropertyValue(sb, context.Parameter, property, site, context.ProviderField, emission);
        }

        _ = sb.Append(scope.Indentation).AppendLine("}");
    }

    /// <summary>Appends the statements expanding a dictionary property's entries under this property's key.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and pre-encoded context.</param>
    /// <param name="property">The dictionary property descriptor.</param>
    /// <param name="dictionary">The dictionary key metadata.</param>
    /// <param name="site">The generated value local, composed key expression, and indentation for this property.</param>
    /// <param name="scope">The nesting scope, supplying the key delimiter.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendObjectDictionaryProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        QueryDictionaryModel dictionary,
        in QueryPropertySite site,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var entryLocal = site.ValueLocal + "_entry";
        var entryValueLocal = site.ValueLocal + "_value";
        var entryKeyLocal = site.ValueLocal + "_entrykey";

        var loopIndent = indent;
        if (property.CanBeNull)
        {
            _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
                .Append(indent).AppendLine("{");
            loopIndent = indent + "    ";
        }

        _ = sb.Append(loopIndent).Append(ForeachVarKeyword).Append(entryLocal).Append(" in ").Append(site.ValueLocal).AppendLine(")")
            .Append(loopIndent).AppendLine("{");

        var entryIndent = loopIndent + "    ";
        _ = sb.Append(entryIndent).Append("var ").Append(entryValueLocal).Append(" = ").Append(entryLocal).AppendLine(".Value;");

        var valueIndent = entryIndent;
        if (dictionary.ValueCanBeNull)
        {
            _ = sb.Append(entryIndent).Append("if (").Append(entryValueLocal).AppendLine(NotNullCheckSuffix)
                .Append(entryIndent).AppendLine("{");
            valueIndent = entryIndent + "    ";
        }

        var keyTypeOf = $"typeof({dictionary.KeyTypeName})";
        var customKey = $"{emission.SettingsLocal}.UrlParameterFormatter.Format({entryLocal}.Key, {keyTypeOf}, {keyTypeOf})";
        var fastKey = BuildFastFormatExpression(entryLocal + ".Key", dictionary.KeyFormat, emission);
        var entryKeyExpression = fastKey is null
            ? customKey
            : $"{emission.UseDefaultFormattingLocal} ? ({fastKey}) : {customKey}";

        var customValue = BuildUrlFormatterCall(entryValueLocal, property.ValueFormat.TypeName, context.ProviderField, emission);
        var fastValue = BuildFastFormatExpression(entryValueLocal, property.ValueFormat, emission);
        var valueExpression = fastValue is null
            ? customValue
            : $"{emission.UseDefaultFormattingLocal} ? ({fastValue}) : {customValue}";

        // The entry key composes under this property's key: "propertyKey" + delimiter + entryKey, matching the
        // reflection builder's nested BuildQueryMap. A blank entry key drops the pair, exactly as reflection does.
        _ = sb.Append(valueIndent).Append("var ").Append(entryKeyLocal).Append(" = ").Append(entryKeyExpression).AppendLine(";")
            .Append(valueIndent).Append("if (!string.IsNullOrWhiteSpace(").Append(entryKeyLocal).AppendLine("))")
            .Append(valueIndent).AppendLine("{")
            .Append(valueIndent).Append("    ").Append(emission.QueryBuilderLocal)
            .Append(AddQueryPairCall).Append(site.KeyExpression).Append(" + ").Append(ToCSharpStringLiteral(scope.Delimiter))
            .Append(" + ").Append(entryKeyLocal).Append(", ").Append(valueExpression).Append(", ")
            .Append(context.PreEncoded).AppendLine(");")
            .Append(valueIndent).AppendLine("}");

        if (dictionary.ValueCanBeNull)
        {
            _ = sb.Append(entryIndent).AppendLine("}");
        }

        _ = sb.Append(loopIndent).AppendLine("}");

        if (!property.CanBeNull)
        {
            return;
        }

        _ = sb.Append(indent).AppendLine("}");
    }

    /// <summary>Appends the statements flattening one nested-object property, recursing into its children.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <param name="property">The nested property descriptor.</param>
    /// <param name="children">The nested property's own flattened properties.</param>
    /// <param name="site">The generated value local and composed key expression for this property.</param>
    /// <param name="scope">The access expression, delimiter, local suffix and indentation for this level.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendNestedObjectProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        ImmutableEquatableArray<QueryObjectPropertyModel> children,
        in QueryPropertySite site,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        var indent = scope.Indentation;
        var innerIndent = indent + "    ";
        var keyLocal = site.ValueLocal + "_key";
        var childSuffix = scope.LocalSuffix + "_" + property.ClrName;

        _ = sb.Append(indent).AppendLine("{")
            .Append(innerIndent).Append("var ").Append(site.ValueLocal).Append(" = ").Append(scope.AccessExpr)
            .Append('.').Append(property.ClrName).AppendLine(";")
            .Append(innerIndent).Append("var ").Append(keyLocal).Append(" = ").Append(site.KeyExpression).AppendLine(";");

        // A nullable value-type nested object holds its underlying struct behind .Value; a reference type flattens off
        // the value directly. The null check above still runs against the value itself.
        var childAccess = property.NestedThroughValue ? site.ValueLocal + ".Value" : site.ValueLocal;

        if (!property.CanBeNull)
        {
            AppendObjectPropertyList(sb, context, children, new(childAccess, keyLocal, scope.Delimiter, childSuffix, innerIndent), emission);
            _ = sb.Append(indent).AppendLine("}");
            return;
        }

        // A null nested object is omitted, unless [Query(SerializeNull = true)] emits a bare key=.
        if (property.SerializeNull)
        {
            _ = sb.Append(innerIndent).Append("if (").Append(site.ValueLocal).AppendLine(" == null)")
                .Append(innerIndent).AppendLine("{")
                .Append(innerIndent).Append("    ").Append(emission.QueryBuilderLocal)
                .Append(AddQueryPairCall).Append(keyLocal).Append(", string.Empty, ").Append(site.PreEncoded).AppendLine(");")
                .Append(innerIndent).AppendLine("}")
                .Append(innerIndent).AppendLine("else")
                .Append(innerIndent).AppendLine("{");
            AppendObjectPropertyList(sb, context, children, new(childAccess, keyLocal, scope.Delimiter, childSuffix, innerIndent + "    "), emission);
            _ = sb.Append(innerIndent).AppendLine("}").Append(indent).AppendLine("}");
            return;
        }

        _ = sb.Append(innerIndent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
            .Append(innerIndent).AppendLine("{");
        AppendObjectPropertyList(sb, context, children, new(childAccess, keyLocal, scope.Delimiter, childSuffix, innerIndent + "    "), emission);
        _ = sb.Append(innerIndent).AppendLine("}").Append(indent).AppendLine("}");
    }

    /// <summary>Builds the composed key expression for a nested property under a parent key.</summary>
    /// <param name="property">The nested-level property.</param>
    /// <param name="parentKeyExpr">The runtime key expression (a local) of the enclosing object.</param>
    /// <param name="delimiter">The nesting delimiter.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The composed key expression: parent key, delimiter, this property's own prefix, then its name.</returns>
    private static string BuildNestedKeyExpression(
        QueryObjectPropertyModel property,
        string parentKeyExpr,
        string delimiter,
        in InlineValueEmission emission)
    {
        var prefixExpr = $"{parentKeyExpr} + {ToCSharpStringLiteral(delimiter + (property.PrefixSegment ?? string.Empty))}";

        // An [AliasAs] name always wins and bypasses the key formatter.
        if (property.ExplicitName is { } alias)
        {
            return $"{prefixExpr} + {ToCSharpStringLiteral(alias)}";
        }

        var formatterCall =
            $"global::Refit.GeneratedRequestRunner.BuildQueryKey({emission.SettingsLocal}, {ToCSharpStringLiteral(property.ClrName)}, null, {prefixExpr})";

        // A [JsonPropertyName] name is honored only when the runtime setting is enabled.
        return property.SerializerName is { } serializerName
            ? $"{emission.SettingsLocal}.{HonorSerializerNamesFlag} ? ({prefixExpr} + {ToCSharpStringLiteral(serializerName)}) : ({formatterCall})"
            : formatterCall;
    }

    /// <summary>Appends the statements flattening one collection-valued query-object property.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="collection">The collection descriptor.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <remarks>
    /// The pristine default formatter renders elements inline through the query builder's collection API, where the
    /// reflection builder's second formatting pass is a no-op. A customized formatter takes the runtime slow path,
    /// which reproduces both passes. A null collection is omitted unless <c>[Query(SerializeNull = true)]</c> emits a
    /// bare <c>key=</c>, matching the reflection builder.
    /// </remarks>
    private static void AppendObjectQueryCollectionProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        QueryObjectCollectionModel collection,
        in QueryPropertySite site,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var keyLocal = site.ValueLocal + "_key";
        _ = sb.Append(indent).Append("var ").Append(keyLocal).Append(" = ").Append(site.KeyExpression).AppendLine(";");

        var bodySite = site with { KeyExpression = keyLocal };
        if (!property.CanBeNull)
        {
            AppendCollectionPropertyBody(sb, context, property, collection, bodySite, emission);
            return;
        }

        if (property.SerializeNull)
        {
            _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(" == null)")
                .Append(indent).AppendLine("{")
                .Append(indent).Append("    ").Append(emission.QueryBuilderLocal)
                .Append(AddQueryPairCall).Append(keyLocal).Append(", string.Empty, ").Append(site.PreEncoded).AppendLine(");")
                .Append(indent).AppendLine("}")
                .Append(indent).AppendLine("else")
                .Append(indent).AppendLine("{");
            AppendCollectionPropertyBody(sb, context, property, collection, bodySite with { Indentation = indent + "    " }, emission);
            _ = sb.Append(indent).AppendLine("}");
            return;
        }

        _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
            .Append(indent).AppendLine("{");
        AppendCollectionPropertyBody(sb, context, property, collection, bodySite with { Indentation = indent + "    " }, emission);
        _ = sb.Append(indent).AppendLine("}");
    }

    /// <summary>Appends the fast/slow collection-append body for a non-null collection property.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="collection">The collection descriptor.</param>
    /// <param name="site">The generated locals and indentation, with <c>KeyExpression</c> set to the key local and
    /// <c>Indentation</c> set to the body indentation.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendCollectionPropertyBody(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        QueryObjectCollectionModel collection,
        in QueryPropertySite site,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var formatExpression = BuildCollectionFormatExpression(collection.CollectionFormatValue, context.ParameterCollectionFormat, emission);
        var elementLocal = site.ValueLocal + "_e";
        var elementExpression = BuildFastCollectionElementExpression(elementLocal, property.ValueFormat, collection, emission);
        var innerIndent = indent + "    ";

        _ = sb.Append(indent).Append("if (").Append(emission.UseDefaultFormattingLocal).AppendLine(")")
            .Append(indent).AppendLine("{")
            .Append(innerIndent).Append(emission.QueryBuilderLocal).Append(".BeginCollection(").Append(site.KeyExpression)
            .Append(", ").Append(formatExpression).Append(", ").Append(site.PreEncoded).AppendLine(");")
            .Append(innerIndent).Append(ForeachVarKeyword).Append(elementLocal).Append(" in ").Append(site.ValueLocal).AppendLine(")")
            .Append(innerIndent).AppendLine("{")
            .Append(innerIndent).Append("    ").Append(emission.QueryBuilderLocal).Append(".AddCollectionValue(").Append(elementExpression).AppendLine(");")
            .Append(innerIndent).AppendLine("}")
            .Append(innerIndent).Append(emission.QueryBuilderLocal).AppendLine(".EndCollection();")
            .Append(indent).AppendLine("}")
            .Append(indent).AppendLine("else")
            .Append(indent).AppendLine("{")
            .Append(innerIndent).Append("global::Refit.GeneratedRequestRunner.AddFormattedCollectionProperty(ref ")
            .Append(emission.QueryBuilderLocal).Append(", ").Append(emission.SettingsLocal).Append(", ").Append(site.ValueLocal)
            .Append(", ").Append(site.KeyExpression).Append(", ").Append(formatExpression).Append(", ").Append(site.PreEncoded)
            .Append(", (typeof(").Append(collection.PropertyTypeName).Append("), ").Append(context.ProviderField)
            .Append(", typeof(").Append(context.Parameter.Type).AppendLine(")));")
            .Append(indent).AppendLine("}");
    }

    /// <summary>Builds the resolved <c>CollectionFormat</c> expression for a collection property.</summary>
    /// <param name="propertyCollectionFormat">The property's own <c>[Query(CollectionFormat)]</c>, or null.</param>
    /// <param name="parameterCollectionFormat">The enclosing parameter's <c>[Query(CollectionFormat)]</c>, or null.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>A compile-time cast literal, or the runtime settings default.</returns>
    private static string BuildCollectionFormatExpression(
        int? propertyCollectionFormat,
        int? parameterCollectionFormat,
        in InlineValueEmission emission) =>
        (propertyCollectionFormat ?? parameterCollectionFormat) is { } value
            ? $"{CollectionFormatCast}{value}"
            : $"{emission.SettingsLocal}.CollectionFormat";

    /// <summary>Builds the inline expression rendering one collection element on the default-formatting fast path.</summary>
    /// <param name="elementLocal">The foreach element local.</param>
    /// <param name="elementFormat">The element rendering strategy.</param>
    /// <param name="collection">The collection descriptor.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The reflection-free element expression, or a formatter call for an element with no inline rendering.</returns>
    private static string BuildFastCollectionElementExpression(
        string elementLocal,
        InlineValueFormatModel elementFormat,
        QueryObjectCollectionModel collection,
        in InlineValueEmission emission)
    {
        var fast = BuildFastFormatExpression(elementLocal, elementFormat, emission);
        if (fast is null)
        {
            // An element with no reflection-free rendering (e.g. an enum with duplicate constants) still uses the
            // formatter, which under the pristine default renders it correctly; the type doubles as the provider.
            return $"{emission.SettingsLocal}.UrlParameterFormatter.Format({elementLocal}, typeof({collection.PropertyTypeName}), typeof({collection.PropertyTypeName}))";
        }

        // A string element renders itself, so a null element already renders as null; other formats guard first.
        return collection.ElementCanBeNull && fast != elementLocal
            ? $"{elementLocal} == null ? null : {fast}"
            : fast;
    }

    /// <summary>Appends the null-guarded statements for a flattened property whose value may be null.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The enclosing parameter model.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendNullableObjectQueryProperty(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryObjectPropertyModel property,
        in QueryPropertySite site,
        string providerField,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;

        // A null property is omitted entirely unless it opts in via [Query(SerializeNull = true)], which emits "key=".
        if (!property.SerializeNull)
        {
            _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
                .Append(indent).AppendLine("{");
            AppendObjectQueryPropertyValue(sb, parameter, property, site with { Indentation = indent + "    " }, providerField, emission);
            _ = sb.Append(indent).AppendLine("}");
            return;
        }

        _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(" == null)")
            .Append(indent).AppendLine("{")
            .Append(indent).Append("    ").Append(emission.QueryBuilderLocal)
            .Append(AddQueryPairCall).Append(site.KeyExpression).Append(", string.Empty, ")
            .Append(site.PreEncoded).AppendLine(");")
            .Append(indent).AppendLine("}")
            .Append(indent).AppendLine("else")
            .Append(indent).AppendLine("{");

        AppendObjectQueryPropertyValue(sb, parameter, property, site with { Indentation = indent + "    " }, providerField, emission);

        _ = sb.Append(indent).AppendLine("}");
    }

    /// <summary>Appends the statements rendering one non-null flattened property value.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The enclosing parameter model.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    private static void AppendObjectQueryPropertyValue(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryObjectPropertyModel property,
        in QueryPropertySite site,
        string providerField,
        in InlineValueEmission emission)
    {
        var fastExpression = BuildFastFormatExpression(site.ValueLocal, property.ValueFormat, emission);
        if (property.PropertyFormat is null)
        {
            AppendUnformattedObjectQueryProperty(sb, parameter, site, providerField, emission, fastExpression);
            return;
        }

        AppendFormattedObjectQueryProperty(sb, parameter, property, site, providerField, emission, fastExpression);
    }

    /// <summary>Appends the append call for a flattened property with no property-level format.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The enclosing parameter model.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="fastExpression">The reflection-free expression, or null when the formatter must always run.</param>
    private static void AppendUnformattedObjectQueryProperty(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        in QueryPropertySite site,
        string providerField,
        in InlineValueEmission emission,
        string? fastExpression)
    {
        var customExpression = BuildUrlFormatterCall(site.ValueLocal, parameter.Type, providerField, emission);
        var valueExpression = fastExpression is null
            ? customExpression
            : $"{emission.UseDefaultFormattingLocal} ? ({fastExpression}) : {customExpression}";

        _ = sb.Append(site.Indentation).Append(emission.QueryBuilderLocal)
            .Append(AddQueryPairCall).Append(site.KeyExpression).Append(", ").Append(valueExpression).Append(", ")
            .Append(site.PreEncoded).AppendLine(");");
    }

    /// <summary>Appends the statements for a flattened property carrying a <c>[Query(Format)]</c>.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The enclosing parameter model.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="fastExpression">The reflection-free expression, or null when the formatter must always run.</param>
    private static void AppendFormattedObjectQueryProperty(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryObjectPropertyModel property,
        in QueryPropertySite site,
        string providerField,
        in InlineValueEmission emission,
        string? fastExpression)
    {
        // The form-url-encoded formatter applies the property format; a null result omits the pair entirely, and only
        // the surviving string reaches the URL parameter formatter.
        var indent = site.Indentation;
        var formattedLocal = site.ValueLocal + "_formatted";
        var formatLiteral = ToNullableCSharpStringLiteral(property.PropertyFormat);
        var formExpression =
            $"{emission.SettingsLocal}.FormUrlEncodedParameterFormatter.Format({site.ValueLocal}, {formatLiteral})";
        var formattedExpression = fastExpression is null
            ? formExpression
            : $"{emission.UseDefaultFormFormattingLocal} ? ({fastExpression}) : {formExpression}";
        var customExpression = BuildUrlFormatterCall(formattedLocal, parameter.Type, providerField, emission);

        _ = sb.Append(indent).Append("var ").Append(formattedLocal).Append(" = ").Append(formattedExpression).AppendLine(";")
            .Append(indent).Append("if (").Append(formattedLocal).AppendLine(NotNullCheckSuffix)
            .Append(indent).AppendLine("{")
            .Append(indent).Append("    ").Append(emission.QueryBuilderLocal)
            .Append(AddQueryPairCall).Append(site.KeyExpression).Append(", ")
            .Append(emission.UseDefaultFormattingLocal).Append(" ? ").Append(formattedLocal)
            .Append(" : ").Append(customExpression)
            .Append(", ").Append(site.PreEncoded).AppendLine(");")
            .Append(indent).AppendLine("}");
    }

    /// <summary>Builds a call to the configured URL parameter formatter for a flattened property value.</summary>
    /// <param name="valueExpression">The value expression to format.</param>
    /// <param name="parameterTypeName">The enclosing parameter's declared type.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The formatter call expression.</returns>
    private static string BuildUrlFormatterCall(
        string valueExpression,
        string parameterTypeName,
        string providerField,
        in InlineValueEmission emission) =>
        $"{emission.SettingsLocal}.UrlParameterFormatter.Format({valueExpression}, {providerField}, typeof({parameterTypeName}))";

    /// <summary>Builds the query key expression for one flattened property.</summary>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>A constant literal when the key is fully known, otherwise a helper call or a settings-gated choice.</returns>
    private static string BuildQueryObjectKeyExpression(QueryObjectPropertyModel property, in InlineValueEmission emission)
    {
        // An [AliasAs] name always wins and bypasses the key formatter, so prefix + alias is fully known at compile time.
        if (property.ExplicitName is { } explicitName)
        {
            return ToCSharpStringLiteral(property.PrefixSegment + explicitName);
        }

        // A [JsonPropertyName] name is honored only when the runtime setting is enabled; otherwise the CLR name goes
        // through the key formatter, so both keys are emitted and the setting selects between them at runtime.
        if (property.SerializerName is { } serializerName)
        {
            var serializerLiteral = ToCSharpStringLiteral(property.PrefixSegment + serializerName);
            return $"{emission.SettingsLocal}.{HonorSerializerNamesFlag} ? {serializerLiteral} : {BuildQueryKeyHelperCall(property, emission)}";
        }

        return BuildQueryKeyHelperCall(property, emission);
    }

    /// <summary>Builds the runtime key-composition call for a flattened property with no alias.</summary>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The <c>BuildQueryKey</c> call expression.</returns>
    private static string BuildQueryKeyHelperCall(QueryObjectPropertyModel property, in InlineValueEmission emission)
    {
        var clrName = ToCSharpStringLiteral(property.ClrName);
        var prefix = ToNullableCSharpStringLiteral(property.PrefixSegment);
        return $"global::Refit.GeneratedRequestRunner.BuildQueryKey({emission.SettingsLocal}, {clrName}, null, {prefix})";
    }

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
    private static string BuildFormattedValueExpression(
        string valueExpression,
        bool canBeNullAtEvaluation,
        string parameterTypeName,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        // TreatAsString stringifies the raw value before the formatter runs, mirroring the reflection builder.
        var customValue = query.TreatAsString ? valueExpression + ".ToString()" : valueExpression;
        var customExpression =
            $"{emission.SettingsLocal}.UrlParameterFormatter.Format({customValue}, {providerField}, typeof({parameterTypeName}))";

        var fastExpression = query.TreatAsString
            ? valueExpression + ".ToString()"
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
    private static string BuildPathValueExpression(
        RequestParameterModel parameter,
        string providerField,
        in InlineValueEmission emission)
    {
        if (parameter.IsRoundTrip)
        {
            // A {**param} catch-all: split the value on '/', format and escape each segment, keep the separators.
            var roundTripValue = parameter.CanBeNull ? $"@{parameter.Name}?.ToString()" : $"@{parameter.Name}.ToString()";
            return $"global::Refit.GeneratedRequestRunner.RoundTripEscapePath({roundTripValue}, {emission.SettingsLocal}.UrlParameterFormatter, {providerField}, typeof({parameter.Type}))";
        }

        var customExpression =
            $"{emission.SettingsLocal}.UrlParameterFormatter.Format(@{parameter.Name}, {providerField}, typeof({parameter.Type}))";
        var valueExpression = "@" + parameter.Name;
        var fastExpression = parameter.ValueFormat is null
            ? null
            : BuildFastFormatExpression(valueExpression, parameter.ValueFormat, emission);
        if (fastExpression is null)
        {
            return customExpression;
        }

        // When the fast path is the value itself (strings), a null value already renders as null.
        if (parameter.CanBeNull && fastExpression != valueExpression)
        {
            fastExpression = $"{valueExpression} == null ? null : {fastExpression}";
        }

        return $"{emission.UseDefaultFormattingLocal} ? ({fastExpression}) : {customExpression}";
    }

    /// <summary>Builds the reflection-free fast-path expression for one non-null value.</summary>
    /// <param name="valueExpression">The value expression, evaluated only when non-null.</param>
    /// <param name="valueFormat">The rendering strategy.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The fast-path expression, or <see langword="null"/> when the formatter must always run.</returns>
    private static string? BuildFastFormatExpression(
        string valueExpression,
        InlineValueFormatModel valueFormat,
        in InlineValueEmission emission)
    {
        var unwrapped = valueFormat.IsNullableValueType ? valueExpression + ".Value" : valueExpression;
        return valueFormat.Kind switch
        {
            InlineFormatKind.String => unwrapped,
            InlineFormatKind.ToStringOnly => unwrapped + ".ToString()",
            InlineFormatKind.Formattable =>
                $"global::Refit.GeneratedRequestRunner.FormatInvariant({unwrapped}, {ToNullableCSharpStringLiteral(valueFormat.Format)})",
            InlineFormatKind.Enum =>
                $"{GetOrAddEnumFormatter(valueFormat, emission)}({unwrapped})",
            _ => null
        };
    }

    /// <summary>Gets the generated enum formatting helper for an enum type and format, emitting it on first use.</summary>
    /// <param name="valueFormat">The enum rendering strategy.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The helper method name.</returns>
    private static string GetOrAddEnumFormatter(
        InlineValueFormatModel valueFormat,
        in InlineValueEmission emission)
    {
        var scope = emission.Scope;
        var key = (valueFormat.TypeName, valueFormat.Format);
        if (scope.Formatters.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var helperName = scope.UniqueNames.New(EnumFormatterBaseName);
        scope.Formatters.Add(key, helperName);
        AppendEnumFormatterSource(valueFormat, helperName, emission.MemberSource);
        return helperName;
    }

    /// <summary>Appends the source of one generated enum formatting helper.</summary>
    /// <param name="valueFormat">The enum rendering strategy.</param>
    /// <param name="helperName">The unique helper method name.</param>
    /// <param name="memberSb">The builder receiving emitted helper members.</param>
    private static void AppendEnumFormatterSource(
        InlineValueFormatModel valueFormat,
        string helperName,
        PooledStringBuilder memberSb)
    {
        var memberIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);
        var caseIndent = bodyIndent + "    ";
        var format = valueFormat.Format;

        _ = memberSb.AppendLine()
            .Append(memberIndent).Append("/// <summary>Formats ").Append(ToXmlDocumentationText(valueFormat.TypeName))
            .AppendLine(" values for generated requests without reflection.</summary>")
            .Append(memberIndent).Append("private static string ").Append(helperName)
            .Append('(').Append(valueFormat.TypeName).AppendLine(" value)")
            .Append(memberIndent).AppendLine("{")
            .Append(bodyIndent).AppendLine("switch (value)")
            .Append(bodyIndent).AppendLine("{");

        foreach (var member in valueFormat.EnumMembers!)
        {
            // With a compile-time format only [EnumMember] overrides beat the formatted numeric rendering,
            // matching string.Format's precedence in the default URL parameter formatter.
            var resolved = format is null
                ? member.EnumMemberValue ?? member.MemberName
                : member.EnumMemberValue;
            if (resolved is null)
            {
                continue;
            }

            _ = memberSb.Append(caseIndent).Append("case ").Append(valueFormat.TypeName).Append(".@").Append(member.MemberName).AppendLine(":")
                .Append(caseIndent).Append("    return ").Append(ToCSharpStringLiteral(resolved)).AppendLine(";");
        }

        var defaultExpression = format is null
            ? "value.ToString()"
            : $"value.ToString({ToCSharpStringLiteral(format)})";
        _ = memberSb.Append(caseIndent).AppendLine("default:")
            .Append(caseIndent).Append("    return ").Append(defaultExpression).AppendLine(";")
            .Append(bodyIndent).AppendLine("}")
            .Append(memberIndent).AppendLine("}");
    }

    /// <summary>Gets the cached converter field for a converter type, emitting the field on first use.</summary>
    /// <param name="converterTypeName">The fully-qualified converter type.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The cached field name.</returns>
    private static string GetOrAddConverterField(string converterTypeName, in InlineValueEmission emission)
    {
        var scope = emission.Scope;
        if (scope.Converters.TryGetValue(converterTypeName, out var existing))
        {
            return existing;
        }

        var fieldName = scope.UniqueNames.New(ConverterFieldBaseName);
        scope.Converters.Add(converterTypeName, fieldName);
        AppendConverterFieldSource(converterTypeName, fieldName, emission.MemberSource);
        return fieldName;
    }

    /// <summary>Appends the source of one cached converter field.</summary>
    /// <param name="converterTypeName">The fully-qualified converter type.</param>
    /// <param name="fieldName">The unique field name.</param>
    /// <param name="memberSb">The builder receiving emitted helper members.</param>
    private static void AppendConverterFieldSource(
        string converterTypeName,
        string fieldName,
        PooledStringBuilder memberSb)
    {
        var memberIndent = Indent(MethodMemberIndentation);
        _ = memberSb.AppendLine()
            .Append(memberIndent).Append("/// <summary>Cached query converter of type ")
            .Append(ToXmlDocumentationText(converterTypeName)).AppendLine(".</summary>")
            .Append(memberIndent).Append("private static readonly ").Append(converterTypeName).Append(' ').Append(fieldName)
            .Append(" = new ").Append(converterTypeName).AppendLine("();");
    }

    /// <summary>Bundles the locals and helper state shared by inline value-formatting emission.</summary>
    /// <param name="QueryBuilderLocal">The generated query builder local name.</param>
    /// <param name="QueryValueLocal">The generated foreach element local name.</param>
    /// <param name="SettingsLocal">The generated settings local name.</param>
    /// <param name="UseDefaultFormattingLocal">The generated default-formatting branch local name.</param>
    /// <param name="UseDefaultFormFormattingLocal">The generated default-form-formatting branch local name,
    /// guarding the fast path for a flattened property's <c>[Query(Format)]</c>.</param>
    /// <param name="Scope">The enum formatter scope for the interface.</param>
    /// <param name="MemberSource">The builder receiving emitted helper members.</param>
    private readonly record struct InlineValueEmission(
        string QueryBuilderLocal,
        string QueryValueLocal,
        string SettingsLocal,
        string UseDefaultFormattingLocal,
        string UseDefaultFormFormattingLocal,
        EnumFormatterScope Scope,
        PooledStringBuilder MemberSource);

    /// <summary>The generated locals and indentation used to emit one flattened query-object property.</summary>
    /// <param name="ValueLocal">The local holding the property value.</param>
    /// <param name="KeyExpression">The query key expression, constant or key-formatter call.</param>
    /// <param name="PreEncoded">The rendered <c>preEncoded</c> boolean literal.</param>
    /// <param name="Indentation">The indentation of the statements emitting this property.</param>
    private readonly record struct QueryPropertySite(
        string ValueLocal,
        string KeyExpression,
        string PreEncoded,
        string Indentation);

    /// <summary>The enclosing-parameter context shared by every flattened property of one query object.</summary>
    /// <param name="Parameter">The enclosing query-object parameter.</param>
    /// <param name="ProviderField">The cached attribute-provider field name for the parameter.</param>
    /// <param name="ParameterCollectionFormat">The parameter's <c>[Query(CollectionFormat)]</c>, or null.</param>
    /// <param name="PreEncoded">The rendered <c>preEncoded</c> boolean literal for the parameter.</param>
    private readonly record struct QueryObjectContext(
        RequestParameterModel Parameter,
        string ProviderField,
        int? ParameterCollectionFormat,
        string PreEncoded);

    /// <summary>The per-nesting-level state threaded through recursive query-object flattening.</summary>
    /// <param name="AccessExpr">The C# expression accessing the current object (e.g. <c>@filter</c> or a nested local).</param>
    /// <param name="ParentKeyExpr">The runtime key expression (a local) of the enclosing object, or null at the top level.</param>
    /// <param name="Delimiter">The delimiter joining nested keys.</param>
    /// <param name="LocalSuffix">The suffix appended to generated local names to keep nested locals unique.</param>
    /// <param name="Indentation">The indentation of the statements emitted at this level.</param>
    private readonly record struct ObjectFlattenScope(
        string AccessExpr,
        string? ParentKeyExpr,
        string Delimiter,
        string LocalSuffix,
        string Indentation);

    /// <summary>The generated locals and indentation used to emit one dictionary entry.</summary>
    /// <param name="EntryLocal">The local holding the current key/value pair.</param>
    /// <param name="KeyLocal">The local receiving the formatted key.</param>
    /// <param name="ValueLocal">The local holding the entry value.</param>
    /// <param name="Indentation">The indentation of the statements emitting this entry.</param>
    private readonly record struct DictionaryEntrySite(
        string EntryLocal,
        string KeyLocal,
        string ValueLocal,
        string Indentation);

    /// <summary>Tracks the enum formatting helpers emitted for one generated interface implementation.</summary>
    /// <param name="uniqueNames">The unique member name builder for the interface scope.</param>
    private sealed class EnumFormatterScope(UniqueNameBuilder uniqueNames)
    {
        /// <summary>Gets the emitted helper names keyed by enum type and compile-time format.</summary>
        public Dictionary<(string TypeName, string? Format), string> Formatters { get; } = new();

        /// <summary>Gets the emitted cached converter field names keyed by converter type.</summary>
        public Dictionary<string, string> Converters { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the unique member name builder for the interface scope.</summary>
        public UniqueNameBuilder UniqueNames { get; } = uniqueNames;
    }
}
