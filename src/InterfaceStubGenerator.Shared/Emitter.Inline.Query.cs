// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

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

    /// <summary>The suffix appended to a generated local holding a dictionary entry or nested property value.</summary>
    private const string ValueLocalSuffix = "_value";

    /// <summary>The member access unwrapping a nullable value type to its underlying value after a null guard.</summary>
    private const string NullableValueAccess = ".Value";

    /// <summary>The generated call appending one collection element to the query-string builder.</summary>
    private const string AddCollectionValueCall = ".AddCollectionValue(";

    /// <summary>The start of a generated <c>foreach (var …)</c> loop over a collection or dictionary.</summary>
    private const string ForeachVarKeyword = "foreach (var ";

    /// <summary>The generated <c>ToString()</c> call appended to a value expression.</summary>
    private const string ToStringCall = ".ToString()";

    /// <summary>The tail of a generated <c>if (value == null)</c> guard.</summary>
    private const string NullEqualityCheckSuffix = " == null)";

    /// <summary>The generated argument list fragment that adds an empty serialized value.</summary>
    private const string EmptyValueArgument = ", string.Empty, ";

    /// <summary>The <c>RefitSettings</c> property gating serializer-aware query key naming.</summary>
    private const string HonorSerializerNamesFlag = "HonorContentSerializerPropertyNamesInQuery";

    /// <summary>Determines whether any parameter binds to the query string.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when query emission is required.</returns>
    internal static bool HasQueryBindings(RequestModel request)
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
    internal static bool NeedsFormattingLocal(RequestModel request)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Path && PathParameterNeedsFormattingLocal(parameter))
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
    internal static bool ObjectPropertiesNeedFormattingLocal(ImmutableEquatableArray<QueryObjectPropertyModel> properties)
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

    /// <summary>Determines whether a path parameter's value or any of its object bindings uses the fast-format branch.</summary>
    /// <param name="parameter">The path parameter model.</param>
    /// <returns><see langword="true"/> when the default-formatting local is referenced when formatting the value.</returns>
    internal static bool PathParameterNeedsFormattingLocal(RequestParameterModel parameter)
    {
        if (parameter.ValueFormat is { Kind: not InlineFormatKind.FormatterOnly })
        {
            return true;
        }

        if (parameter.PathObjectBindings is not { } bindings)
        {
            return false;
        }

        foreach (var binding in bindings)
        {
            if (binding.ValueFormat.Kind != InlineFormatKind.FormatterOnly)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether a parameter needs a generated attribute-provider field.</summary>
    /// <param name="parameter">The parameter model.</param>
    /// <returns><see langword="true"/> when formatting may consult the parameter's attributes at runtime.</returns>
    internal static bool NeedsAttributeProvider(RequestParameterModel parameter) =>
        parameter.Kind == RequestParameterKind.Path
        || parameter.Query is { Shape: not QueryParameterShape.Converter };

    /// <summary>Determines whether the generated method needs the default-form-formatting branch local.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when a flattened query-object property carries a compile-time format.</returns>
    internal static bool NeedsFormFormattingLocal(RequestModel request)
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
    internal static bool ObjectPropertiesHaveFormat(ImmutableEquatableArray<QueryObjectPropertyModel> properties)
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

    /// <summary>Appends the query-appending statements for an inline generated method straight into the prologue buffer.</summary>
    /// <param name="sb">The buffer accumulating the request-prologue source.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="parameterInfoNames">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendInlineQueryStatements(
        PooledStringBuilder sb,
        RequestModel request,
        Dictionary<string, string> parameterInfoNames,
        in InlineValueEmission emission)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Query is not { } query)
            {
                continue;
            }

            // A converter parameter needs no attribute provider (it formats its own values), so it may be absent.
            _ = parameterInfoNames.TryGetValue(parameter.Name, out var providerField);
            AppendInlineQueryStatement(sb, parameter, query, providerField, emission);
        }
    }

    /// <summary>Appends the query-building statements for one parameter, dispatched on its query shape.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parsed request parameter.</param>
    /// <param name="query">The parameter's query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <remarks>The <see cref="QueryParameterShape"/> arms are exhaustive over the shapes the parser produces; the
    /// compiler-required collection default arm cannot be reached for every shape value by tests.</remarks>
    [ExcludeFromCodeCoverage]
    internal static void AppendInlineQueryStatement(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
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

    /// <summary>Appends the statement emitting one scalar query value or flag.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendScalarQueryStatement(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);

        if (parameter.CanBeNull)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
            AppendScalarAddCall(sb, parameter, query, providerField, bodyIndent + "    ", emission);
            _ = sb.Append(bodyIndent).AppendLine("}");
            return;
        }

        AppendScalarAddCall(sb, parameter, query, providerField, bodyIndent, emission);
    }

    /// <summary>Appends the query-builder add/addflag call for a scalar value, writing each piece straight into the
    /// builder instead of composing an intermediate interpolated statement string.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="indent">The indentation of the emitted statement.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendScalarAddCall(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        string indent,
        in InlineValueEmission emission)
    {
        var preEncoded = ToLowerInvariantString(query.PreEncoded);
        if (query.Shape == QueryParameterShape.Flag)
        {
            // Scalar values are guarded by the outer null check, so the value is never null when formatted.
            var flagValue = BuildFormattedValueExpression("@" + parameter.Name, false, parameter.Type, query, providerField, emission);
            _ = sb.Append(indent).Append(emission.QueryBuilderLocal).Append(".AddFlag(").Append(flagValue).Append(", ")
                .Append(preEncoded).AppendLine(");");
            return;
        }

        // The key is a compile-time constant, so it is escaped once here rather than on every call; the value keeps its
        // per-call escaping, and the pre-escaped-key builder overloads append the key verbatim.
        var key = BuildPreEscapedQueryKeyLiteral(query.Key, query.PreEncoded);

        // On the default-formatting branch a span-formattable value is rendered straight into the builder, skipping the
        // per-value intermediate string; a customized formatter keeps the string-formatted Add.
        if (IsSpanFormattableFast(query, out var format))
        {
            // A nullable value type writes the unwrapped .Value (span-formattable) on the fast path - the outer null
            // guard already ran - while the customized-formatter branch keeps the original value and declared type so
            // it matches the reflection builder's UrlParameterFormatter.Format call exactly.
            var accessor = "@" + parameter.Name;
            var fastAccessor = query.ValueFormat.IsNullableValueType ? accessor + NullableValueAccess : accessor;
            var customExpression = BuildUrlFormatterCall(accessor, parameter.Type, providerField, emission);
            var innerIndent = indent + "    ";
            _ = sb.Append(indent).Append("if (").Append(emission.UseDefaultFormattingLocal).AppendLine(")")
                .Append(indent).AppendLine("{")
                .Append(innerIndent).Append(emission.QueryBuilderLocal).Append(".AddFormattedPreEscapedKey(").Append(key).Append(", ")
                    .Append(fastAccessor).Append(", ").Append(ToNullableCSharpStringLiteral(format)).Append(", ").Append(preEncoded).AppendLine(");")
                .Append(indent).AppendLine("}")
                .Append(indent).AppendLine("else")
                .Append(indent).AppendLine("{")
                .Append(innerIndent).Append(emission.QueryBuilderLocal).Append(".AddPreEscapedKey(").Append(key).Append(", ")
                    .Append(customExpression).Append(", ").Append(preEncoded).AppendLine(");")
                .Append(indent).AppendLine("}");
            return;
        }

        var valueExpression = BuildFormattedValueExpression("@" + parameter.Name, false, parameter.Type, query, providerField, emission);
        _ = sb.Append(indent).Append(emission.QueryBuilderLocal).Append(".AddPreEscapedKey(").Append(key).Append(", ").Append(valueExpression)
            .Append(", ").Append(preEncoded).AppendLine(");");
    }

    /// <summary>Builds the C# literal for a compile-time-constant query key, escaping it at generation time.</summary>
    /// <param name="key">The constant query key.</param>
    /// <param name="preEncoded">Whether the key is caller-encoded and must be emitted verbatim.</param>
    /// <returns>The C# string literal, URI-data-escaped unless <paramref name="preEncoded"/>.</returns>
    /// <remarks>Escaping here rather than on every request matches the reflection builder's output because
    /// <c>Uri.EscapeDataString</c> follows RFC 3986 consistently across the supported target frameworks.</remarks>
    internal static string BuildPreEscapedQueryKeyLiteral(string key, bool preEncoded) =>
        ToCSharpStringLiteral(preEncoded ? key : System.Uri.EscapeDataString(key));

    /// <summary>Appends the statements emitting one collection-valued query parameter or flag set.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendCollectionQueryStatement(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        QueryParameterModel query,
        string providerField,
        in InlineValueEmission emission)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var isFlag = query.Shape == QueryParameterShape.FlagCollection;
        var preEncoded = ToLowerInvariantString(query.PreEncoded);

        // An unformatted span-formattable element renders straight into the builder on the default-formatting branch,
        // skipping the per-element intermediate string; a per-element format keeps the string-formatted path because
        // AddCollectionValueFormatted renders with the default format only.
        var fast = !isFlag && IsCollectionSpanFormattableFast(query);
        var guarded = parameter.CanBeNull;
        var loopIndent = guarded ? bodyIndent + "    " : bodyIndent;

        if (guarded)
        {
            _ = sb.Append(bodyIndent).Append(IfParameterPrefix).Append(parameter.Name).AppendLine(NotNullCheckSuffix)
                .Append(bodyIndent).AppendLine("{");
        }

        if (!isFlag)
        {
            AppendBeginCollection(sb, query, emission, preEncoded, loopIndent);
        }

        _ = sb.Append(loopIndent).Append(ForeachVarKeyword).Append(emission.QueryValueLocal).Append(" in @").Append(parameter.Name).AppendLine(")")
            .Append(loopIndent).AppendLine("{");
        var itemIndent = loopIndent + "    ";
        if (fast)
        {
            AppendFastCollectionElement(sb, parameter, providerField, emission, itemIndent);
        }
        else
        {
            var elementExpression = BuildFormattedValueExpression(
                emission.QueryValueLocal,
                query.ElementCanBeNull,
                parameter.Type,
                query,
                providerField,
                emission);
            _ = sb.Append(itemIndent).Append(emission.QueryBuilderLocal);
            if (isFlag)
            {
                _ = sb.Append(".AddFlag(").Append(elementExpression).Append(", ").Append(preEncoded).AppendLine(");");
            }
            else
            {
                _ = sb.Append(AddCollectionValueCall).Append(elementExpression).AppendLine(");");
            }
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

    /// <summary>Appends the <c>BeginCollection</c> call opening a query collection.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="preEncoded">The rendered <c>preEncoded</c> boolean literal.</param>
    /// <param name="loopIndent">The indentation of the emitted call.</param>
    internal static void AppendBeginCollection(
        PooledStringBuilder sb,
        QueryParameterModel query,
        in InlineValueEmission emission,
        string preEncoded,
        string loopIndent)
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

        _ = sb.Append(", ").Append(preEncoded).AppendLine(");");
    }

    /// <summary>Appends the default-formatting-guarded add for one span-formattable collection element.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="providerField">The cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="itemIndent">The indentation of the emitted element statements.</param>
    internal static void AppendFastCollectionElement(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        string providerField,
        in InlineValueEmission emission,
        string itemIndent)
    {
        var customExpression = BuildUrlFormatterCall(emission.QueryValueLocal, parameter.Type, providerField, emission);
        var innerIndent = itemIndent + "    ";
        _ = sb.Append(itemIndent).Append("if (").Append(emission.UseDefaultFormattingLocal).AppendLine(")")
            .Append(itemIndent).AppendLine("{")
            .Append(innerIndent).Append(emission.QueryBuilderLocal).Append(".AddCollectionValueFormatted(").Append(emission.QueryValueLocal).AppendLine(");")
            .Append(itemIndent).AppendLine("}")
            .Append(itemIndent).AppendLine("else")
            .Append(itemIndent).AppendLine("{")
            .Append(innerIndent).Append(emission.QueryBuilderLocal).Append(AddCollectionValueCall).Append(customExpression).AppendLine(");")
            .Append(itemIndent).AppendLine("}");
    }

    /// <summary>Appends the statement delegating one converter-bound parameter to its <c>IQueryConverter&lt;T&gt;</c>.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The parameter model.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    internal static void AppendConverterQueryStatements(
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

    /// <summary>Bundles the locals and helper state shared by inline value-formatting emission.</summary>
    /// <param name="QueryBuilderLocal">The generated query builder local name.</param>
    /// <param name="QueryValueLocal">The generated foreach element local name.</param>
    /// <param name="SettingsLocal">The generated settings local name.</param>
    /// <param name="UseDefaultFormattingLocal">The generated default-formatting branch local name.</param>
    /// <param name="UseDefaultFormFormattingLocal">The generated default-form-formatting branch local name,
    /// guarding the fast path for a flattened property's <c>[Query(Format)]</c>.</param>
    /// <param name="Scope">The enum formatter scope for the interface.</param>
    /// <param name="MemberSource">The builder receiving emitted helper members.</param>
    /// <param name="SupportsCollectionExpressions">Whether the consumer supports C# 12 collection expressions, so path
    /// replacements are emitted as a stack-allocatable <c>[...]</c> span instead of an explicitly-typed array.</param>
    internal readonly record struct InlineValueEmission(
        string QueryBuilderLocal,
        string QueryValueLocal,
        string SettingsLocal,
        string UseDefaultFormattingLocal,
        string UseDefaultFormFormattingLocal,
        EnumFormatterScope Scope,
        PooledStringBuilder MemberSource,
        bool SupportsCollectionExpressions);

    /// <summary>The generated locals and indentation used to emit one flattened query-object property.</summary>
    /// <param name="ValueLocal">The local holding the property value.</param>
    /// <param name="KeyExpression">The query key expression, constant or key-formatter call.</param>
    /// <param name="PreEncoded">The rendered <c>preEncoded</c> boolean literal.</param>
    /// <param name="Indentation">The indentation of the statements emitting this property.</param>
    internal readonly record struct QueryPropertySite(
        string ValueLocal,
        string KeyExpression,
        string PreEncoded,
        string Indentation);

    /// <summary>The enclosing-parameter context shared by every flattened property of one query object.</summary>
    /// <param name="Parameter">The enclosing query-object parameter.</param>
    /// <param name="ProviderField">The cached attribute-provider field name for the parameter.</param>
    /// <param name="ParameterCollectionFormat">The parameter's <c>[Query(CollectionFormat)]</c>, or null.</param>
    /// <param name="PreEncoded">The rendered <c>preEncoded</c> boolean literal for the parameter.</param>
    internal readonly record struct QueryObjectContext(
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
    internal readonly record struct ObjectFlattenScope(
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
    internal readonly record struct DictionaryEntrySite(
        string EntryLocal,
        string KeyLocal,
        string ValueLocal,
        string Indentation);

    /// <summary>Tracks the enum formatting helpers emitted for one generated interface implementation.</summary>
    /// <param name="uniqueNames">The unique member name builder for the interface scope.</param>
    internal sealed class EnumFormatterScope(UniqueNameBuilder uniqueNames)
    {
        /// <summary>Gets the emitted helper names keyed by enum type and compile-time format.</summary>
        public Dictionary<(string TypeName, string? Format), string> Formatters { get; } = new();

        /// <summary>Gets the emitted cached converter field names keyed by converter type.</summary>
        public Dictionary<string, string> Converters { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the unique member name builder for the interface scope.</summary>
        public UniqueNameBuilder UniqueNames { get; } = uniqueNames;
    }
}
