// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Flattens a complex query object's properties into query-string statements.</content>
internal static partial class Emitter
{
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
    internal static void AppendObjectQueryStatements(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var guarded = parameter.CanBeNull;
        var indent = guarded ? $"{bodyIndent}    " : bodyIndent;

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

        // A Nullable<T> query object holds its underlying struct behind .Value; the != null guard above is the HasValue
        // check, so the deref is always safe here. A reference-type object flattens directly off the parameter.
        var accessExpr = query.ValueFormat.IsNullableValueType
            ? $"@{parameter.Name}{NullableValueAccess}"
            : $"@{parameter.Name}";
        var scope = new ObjectFlattenScope(accessExpr, null, query.NestingDelimiter, string.Empty, indent);
        AppendObjectPropertyList(sb, context, query.ObjectProperties!.Value, scope, emission);

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
    internal static void AppendObjectPropertyList(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        ImmutableEquatableArray<QueryObjectPropertyModel> properties,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        foreach (var property in properties)
        {
            var valueLocal = $"{emission.QueryValueLocal}{scope.LocalSuffix}_{property.ClrName}";
            var keyExpression = scope.ParentKeyExpr is { } parentKey
                ? BuildNestedKeyExpression(property, parentKey, scope.Delimiter, emission, context)
                : BuildQueryObjectKeyExpression(property, emission);

            var pairCall = context.PreEscapedKeys ? ".AddPreEscapedKey(" : AddQueryPairCall;
            var site = new QueryPropertySite(valueLocal, keyExpression, context.PreEncoded, $"{scope.Indentation}    ", pairCall);

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
    internal static void AppendObjectLeafProperty(
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
    internal static void AppendObjectDictionaryProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        QueryDictionaryModel dictionary,
        in QueryPropertySite site,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var entryLocal = $"{site.ValueLocal}_entry";
        var entryValueLocal = site.ValueLocal + ValueLocalSuffix;
        var entryKeyLocal = $"{site.ValueLocal}_entrykey";

        var loopIndent = indent;
        if (property.CanBeNull)
        {
            _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
                .Append(indent).AppendLine("{");
            loopIndent = $"{indent}    ";
        }

        _ = sb.Append(loopIndent).Append(ForeachVarKeyword).Append(entryLocal).Append(" in ").Append(site.ValueLocal).AppendLine(")")
            .Append(loopIndent).AppendLine("{");

        var entryIndent = $"{loopIndent}    ";
        _ = sb.Append(entryIndent).Append("var ").Append(entryValueLocal).Append(" = ").Append(entryLocal).AppendLine(".Value;");

        var valueIndent = entryIndent;
        if (dictionary.ValueCanBeNull)
        {
            _ = sb.Append(entryIndent).Append("if (").Append(entryValueLocal).AppendLine(NotNullCheckSuffix)
                .Append(entryIndent).AppendLine("{");
            valueIndent = $"{entryIndent}    ";
        }

        var (entryKeyExpression, valueExpression) = BuildDictionaryEntryExpressions(entryLocal, entryValueLocal, dictionary, property, context, emission);

        // The entry key composes under this property's key: "propertyKey" + delimiter + entryKey, matching the
        // reflection builder's nested BuildQueryMap. A blank entry key drops the pair, exactly as reflection does.
        _ = sb.Append(valueIndent).Append("var ").Append(entryKeyLocal).Append(" = ").Append(entryKeyExpression).AppendLine(";")
            .Append(valueIndent).Append("if (!string.IsNullOrWhiteSpace(").Append(entryKeyLocal).AppendLine("))")
            .Append(valueIndent).AppendLine("{")
            .Append(valueIndent).Append("    ").Append(emission.QueryBuilderLocal)
            .Append(site.AddPairCall).Append(site.KeyExpression).Append(" + ").Append(ToCSharpStringLiteral(scope.Delimiter))
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

    /// <summary>Builds the formatted entry-key and value expressions for one dictionary property entry.</summary>
    /// <param name="entryLocal">The local holding the current key/value pair.</param>
    /// <param name="entryValueLocal">The local holding the entry value.</param>
    /// <param name="dictionary">The dictionary key metadata.</param>
    /// <param name="property">The dictionary property descriptor.</param>
    /// <param name="context">The enclosing parameter and provider-field context.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The generated entry-key and value formatting expressions.</returns>
    internal static (string EntryKeyExpression, string ValueExpression) BuildDictionaryEntryExpressions(
        string entryLocal,
        string entryValueLocal,
        QueryDictionaryModel dictionary,
        QueryObjectPropertyModel property,
        in QueryObjectContext context,
        in InlineValueEmission emission)
    {
        var keyTypeOf = $"typeof({dictionary.KeyTypeName})";
        var customKey = EmitFormatUrlParameter($"{entryLocal}.Key", keyTypeOf, keyTypeOf, emission);
        var fastKey = BuildFastFormatExpression($"{entryLocal}.Key", dictionary.KeyFormat, emission);
        var entryKeyExpression = fastKey is null
            ? customKey
            : $"{emission.UseDefaultFormattingLocal} ? ({fastKey}) : {customKey}";

        var customValue = BuildUrlFormatterCall(entryValueLocal, property.ValueFormat.TypeName, context.ProviderField, emission);
        var fastValue = BuildFastFormatExpression(entryValueLocal, property.ValueFormat, emission);
        var valueExpression = fastValue is null
            ? customValue
            : $"{emission.UseDefaultFormattingLocal} ? ({fastValue}) : {customValue}";
        return (entryKeyExpression, valueExpression);
    }

    /// <summary>Appends the statements flattening one nested-object property, recursing into its children.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <param name="property">The nested property descriptor.</param>
    /// <param name="children">The nested property's own flattened properties.</param>
    /// <param name="site">The generated value local and composed key expression for this property.</param>
    /// <param name="scope">The access expression, delimiter, local suffix and indentation for this level.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendNestedObjectProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        ImmutableEquatableArray<QueryObjectPropertyModel> children,
        in QueryPropertySite site,
        in ObjectFlattenScope scope,
        in InlineValueEmission emission)
    {
        var indent = scope.Indentation;
        var innerIndent = $"{indent}    ";
        var keyLocal = $"{site.ValueLocal}_key";
        var childSuffix = $"{scope.LocalSuffix}_{property.ClrName}";

        _ = sb.Append(indent).AppendLine("{")
            .Append(innerIndent).Append("var ").Append(site.ValueLocal).Append(" = ").Append(scope.AccessExpr)
            .Append('.').Append(property.ClrName).AppendLine(";")
            .Append(innerIndent).Append("var ").Append(keyLocal).Append(" = ").Append(site.KeyExpression).AppendLine(";");

        // A nullable value-type nested object holds its underlying struct behind .Value; a reference type flattens off
        // the value directly. The null check above still runs against the value itself.
        var childAccess = property.NestedThroughValue
            ? string.Concat(site.ValueLocal, NullableValueAccess)
            : site.ValueLocal;

        if (!property.CanBeNull)
        {
            AppendObjectPropertyList(sb, context, children, new(childAccess, keyLocal, scope.Delimiter, childSuffix, innerIndent), emission);
            _ = sb.Append(indent).AppendLine("}");
            return;
        }

        // A null nested object is omitted, unless [Query(SerializeNull = true)] emits a bare key=.
        if (property.SerializeNull)
        {
            _ = sb.Append(innerIndent).Append("if (").Append(site.ValueLocal).AppendLine(NullEqualityCheckSuffix)
                .Append(innerIndent).AppendLine("{")
                .Append(innerIndent).Append("    ").Append(emission.QueryBuilderLocal)
                .Append(site.AddPairCall).Append(keyLocal).Append(EmptyValueArgument).Append(site.PreEncoded).AppendLine(");")
                .Append(innerIndent).AppendLine("}")
                .Append(innerIndent).AppendLine("else")
                .Append(innerIndent).AppendLine("{");
            AppendObjectPropertyList(sb, context, children, new(childAccess, keyLocal, scope.Delimiter, childSuffix, $"{innerIndent}    "), emission);
            _ = sb.Append(innerIndent).AppendLine("}").Append(indent).AppendLine("}");
            return;
        }

        _ = sb.Append(innerIndent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
            .Append(innerIndent).AppendLine("{");
        AppendObjectPropertyList(sb, context, children, new(childAccess, keyLocal, scope.Delimiter, childSuffix, $"{innerIndent}    "), emission);
        _ = sb.Append(innerIndent).AppendLine("}").Append(indent).AppendLine("}");
    }

    /// <summary>Builds the composed key expression for a nested property under a parent key.</summary>
    /// <param name="property">The nested-level property.</param>
    /// <param name="parentKeyExpr">The runtime key expression (a local) of the enclosing object.</param>
    /// <param name="delimiter">The nesting delimiter.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="context">The enclosing parameter, provider field, and collection-format context.</param>
    /// <returns>The composed key expression: parent key, delimiter, this property's own prefix, then its name.</returns>
    internal static string BuildNestedKeyExpression(
        QueryObjectPropertyModel property,
        string parentKeyExpr,
        string delimiter,
        in InlineValueEmission emission,
        in QueryObjectContext context)
    {
        var prefixExpr = $"{parentKeyExpr} + {ToCSharpStringLiteral(delimiter + (property.PrefixSegment ?? string.Empty))}";

        // An [AliasAs] name always wins and bypasses the key formatter.
        if (property.ExplicitName is { } alias)
        {
            return $"{prefixExpr} + {ToCSharpStringLiteral(alias)}";
        }

        var propertyNameExpr = ToCSharpStringLiteral(property.ClrName);
        var formatterCall = context.PreEscapedKeys
            ? $"{prefixExpr} + {propertyNameExpr}"
            : $"global::Refit.GeneratedRequestRunner.BuildQueryKey({emission.SettingsLocal}, {propertyNameExpr}, null, {prefixExpr})";

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
    internal static void AppendObjectQueryCollectionProperty(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        QueryObjectCollectionModel collection,
        in QueryPropertySite site,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var keyLocal = $"{site.ValueLocal}_key";
        _ = sb.Append(indent).Append("var ").Append(keyLocal).Append(" = ").Append(site.KeyExpression).AppendLine(";");

        var bodySite = site with { KeyExpression = keyLocal };
        if (!property.CanBeNull)
        {
            AppendCollectionPropertyBody(sb, context, property, collection, bodySite, emission);
            return;
        }

        if (property.SerializeNull)
        {
            _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NullEqualityCheckSuffix)
                .Append(indent).AppendLine("{")
                .Append(indent).Append("    ").Append(emission.QueryBuilderLocal)
                .Append(site.AddPairCall).Append(keyLocal).Append(EmptyValueArgument).Append(site.PreEncoded).AppendLine(");")
                .Append(indent).AppendLine("}")
                .Append(indent).AppendLine("else")
                .Append(indent).AppendLine("{");
            AppendCollectionPropertyBody(sb, context, property, collection, bodySite with { Indentation = $"{indent}    " }, emission);
            _ = sb.Append(indent).AppendLine("}");
            return;
        }

        _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NotNullCheckSuffix)
            .Append(indent).AppendLine("{");
        AppendCollectionPropertyBody(sb, context, property, collection, bodySite with { Indentation = $"{indent}    " }, emission);
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
    internal static void AppendCollectionPropertyBody(
        PooledStringBuilder sb,
        in QueryObjectContext context,
        QueryObjectPropertyModel property,
        QueryObjectCollectionModel collection,
        in QueryPropertySite site,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var formatExpression = BuildCollectionFormatExpression(collection.CollectionFormatValue, context.ParameterCollectionFormat, emission);
        var elementLocal = $"{site.ValueLocal}_e";
        var elementExpression = BuildFastCollectionElementExpression(elementLocal, property.ValueFormat, collection, emission);
        var innerIndent = $"{indent}    ";

        _ = sb.Append(indent).Append("if (").Append(emission.UseDefaultFormattingLocal).AppendLine(")")
            .Append(indent).AppendLine("{")
            .Append(innerIndent).Append(emission.QueryBuilderLocal).Append(".BeginCollection(").Append(site.KeyExpression)
            .Append(", ").Append(formatExpression).Append(", ").Append(site.PreEncoded).AppendLine(");")
            .Append(innerIndent).Append(ForeachVarKeyword).Append(elementLocal).Append(" in ").Append(site.ValueLocal).AppendLine(")")
            .Append(innerIndent).AppendLine("{")
            .Append(innerIndent).Append("    ").Append(emission.QueryBuilderLocal).Append(AddCollectionValueCall).Append(elementExpression).AppendLine(");")
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
    internal static string BuildCollectionFormatExpression(
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
    internal static string BuildFastCollectionElementExpression(
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
            var elementTypeOf = $"typeof({collection.PropertyTypeName})";
            return EmitFormatUrlParameter(elementLocal, elementTypeOf, elementTypeOf, emission);
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
    internal static void AppendNullableObjectQueryProperty(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
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
            AppendObjectQueryPropertyValue(sb, parameter, property, site with { Indentation = $"{indent}    " }, providerField, emission);
            _ = sb.Append(indent).AppendLine("}");
            return;
        }

        _ = sb.Append(indent).Append("if (").Append(site.ValueLocal).AppendLine(NullEqualityCheckSuffix)
            .Append(indent).AppendLine("{")
            .Append(indent).Append("    ").Append(emission.QueryBuilderLocal)
            .Append(site.AddPairCall).Append(site.KeyExpression).Append(EmptyValueArgument)
            .Append(site.PreEncoded).AppendLine(");")
            .Append(indent).AppendLine("}")
            .Append(indent).AppendLine("else")
            .Append(indent).AppendLine("{");

        AppendObjectQueryPropertyValue(sb, parameter, property, site with { Indentation = $"{indent}    " }, providerField, emission);

        _ = sb.Append(indent).AppendLine("}");
    }

    /// <summary>Appends the statements rendering one non-null flattened property value.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The enclosing parameter model.</param>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="site">The generated locals and indentation for this property.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendObjectQueryPropertyValue(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
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
    internal static void AppendUnformattedObjectQueryProperty(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
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
            .Append(site.AddPairCall).Append(site.KeyExpression).Append(", ").Append(valueExpression).Append(", ")
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
    internal static void AppendFormattedObjectQueryProperty(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryObjectPropertyModel property,
        in QueryPropertySite site,
        string providerField,
        in InlineValueEmission emission,
        string? fastExpression)
    {
        // The form-url-encoded formatter applies the property format; a null result omits the pair entirely, and only
        // the surviving string reaches the URL parameter formatter.
        var indent = site.Indentation;
        var formattedLocal = $"{site.ValueLocal}_formatted";
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
            .Append(site.AddPairCall).Append(site.KeyExpression).Append(", ")
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
    internal static string BuildUrlFormatterCall(
        string valueExpression,
        string parameterTypeName,
        string providerField,
        in InlineValueEmission emission) =>
        EmitFormatUrlParameter(valueExpression, providerField, $"typeof({parameterTypeName})", emission);

    /// <summary>Emits a call to the shared runtime helper that renders a URL parameter value, consulting the per-type
    /// formatter registry on the settings before the configured <c>IUrlParameterFormatter</c>.</summary>
    /// <param name="valueExpression">The value expression to format.</param>
    /// <param name="providerExpression">The attribute-provider expression passed to the formatter.</param>
    /// <param name="typeExpression">The declared-type expression passed to the formatter.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The formatter call expression, keeping the reflection and generated paths in parity.</returns>
    internal static string EmitFormatUrlParameter(
        string valueExpression,
        string providerExpression,
        string typeExpression,
        in InlineValueEmission emission) =>
        $"global::Refit.GeneratedRequestRunner.FormatUrlParameter({emission.SettingsLocal}, {valueExpression}, {providerExpression}, {typeExpression})";

    /// <summary>Builds the query key expression for one flattened property.</summary>
    /// <param name="property">The flattened property descriptor.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>A constant literal when the key is fully known, otherwise a helper call or a settings-gated choice.</returns>
    internal static string BuildQueryObjectKeyExpression(QueryObjectPropertyModel property, in InlineValueEmission emission)
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
    internal static string BuildQueryKeyHelperCall(QueryObjectPropertyModel property, in InlineValueEmission emission)
    {
        var clrName = ToCSharpStringLiteral(property.ClrName);
        var prefix = ToNullableCSharpStringLiteral(property.PrefixSegment);
        return $"global::Refit.GeneratedRequestRunner.BuildQueryKey({emission.SettingsLocal}, {clrName}, null, {prefix})";
    }

    /// <summary>Appends the statements that iterate a <c>[Query(CollectionFormat.Indexed)]</c> collection,
    /// flattening each element's properties under an indexed key prefix: <c>key[0].Prop=val</c>.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendIndexedCollectionQueryStatements(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var guarded = parameter.CanBeNull;
        var outerIndent = guarded ? bodyIndent + "    " : bodyIndent;

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        // Wrap in a block so the index local is scoped to this parameter and cannot clash with other parameters.
        var idxLocal = emission.QueryValueLocal + "_idx";
        var blockIndent = outerIndent + "    ";
        var foreachIndent = blockIndent + "    ";

        _ = sb.Append(outerIndent).AppendLine("{")
            .Append(blockIndent).Append("var ").Append(idxLocal).AppendLine(" = 0;")
            .Append(blockIndent).Append(ForeachVarKeyword).Append(emission.QueryValueLocal)
                .Append(" in @").Append(parameter.Name).AppendLine(")")
            .Append(blockIndent).AppendLine("{");

        AppendIndexedElement(sb, parameter, query, providerField, idxLocal, foreachIndent, emission);

        _ = sb.Append(foreachIndent).Append(idxLocal).AppendLine("++;")
            .Append(blockIndent).AppendLine("}") // close foreach body
            .Append(outerIndent).AppendLine("}"); // close wrapper block

        if (!guarded)
        {
            return;
        }

        _ = sb.Append(bodyIndent).AppendLine("}");
    }

    /// <summary>Appends one element's optional null guard, indexed key declaration, and flattened property statements.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The enclosing parameter model.</param>
    /// <param name="query">The query-binding metadata whose <c>ObjectProperties</c> describes each element's shape.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="idxLocal">The generated index counter local name.</param>
    /// <param name="foreachIndent">The indentation inside the foreach body.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendIndexedElement(
        PooledStringBuilder sb,
        in RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        string idxLocal,
        string foreachIndent,
        in InlineValueEmission emission)
    {
        var keyLocal = $"{emission.QueryValueLocal}_key";
        var itemIndent = foreachIndent;

        _ = sb.Append(itemIndent).Append("var ").Append(keyLocal).Append(" = ")
            .Append("$").Append('"').Append(query.Key)
            .Append("[{").Append(idxLocal).Append(".ToString(global::System.Globalization.CultureInfo.InvariantCulture)").Append("}]").Append('"').AppendLine(";");

        if (query.ElementCanBeNull)
        {
            _ = sb.Append(foreachIndent).Append("if (").Append(emission.QueryValueLocal).AppendLine(NotNullCheckSuffix)
                .Append(foreachIndent).AppendLine("{");
            itemIndent = foreachIndent + "    ";
        }

        // Flatten the element's properties under the indexed key; pass null as collection format so nested
        // collection properties fall back to settings default instead of inheriting Indexed.
        // PreEscapedKeys tells every leaf property to use AddPreEscapedKey so the brackets in the key are not re-encoded.
        var context = new QueryObjectContext(parameter, providerField, null, ToLowerInvariantString(query.PreEncoded), true);
        var scope = new ObjectFlattenScope(emission.QueryValueLocal, keyLocal, query.NestingDelimiter, "_" + parameter.Name, itemIndent);
        AppendObjectPropertyList(sb, context, query.ObjectProperties!.Value, scope, emission);

        if (!query.ElementCanBeNull)
        {
            return;
        }

        _ = sb.Append(foreachIndent).AppendLine("}");
    }
}
