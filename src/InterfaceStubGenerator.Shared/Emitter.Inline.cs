// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
internal static partial class Emitter
{
    /// <summary>The base member name for a cached form field descriptor array.</summary>
    private const string FormFieldsVariableName = "______formFields";

    /// <summary>The opening of a generated <c>FormField</c> construction.</summary>
    private const string FormFieldNew = "new global::Refit.FormField<";

    /// <summary>The separator before the quoted CLR property name argument.</summary>
    private const string FormFieldNameOpen = ", \"";

    /// <summary>The separator after the quoted CLR property name argument.</summary>
    private const string FormFieldNameClose = "\", ";

    /// <summary>The separator between generated arguments.</summary>
    private const string ArgumentSeparator = ", ";

    /// <summary>The closing of a generated <c>FormField</c> construction including the trailing element separator.</summary>
    private const string FormFieldClose = "),\n";

    /// <summary>The cast prefix for an explicit collection format value.</summary>
    private const string CollectionFormatCast = "(global::Refit.CollectionFormat)";

    /// <summary>Builds the body of the Refit method.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="requestBuilderFieldName">The unique generated field name that stores the request builder.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    /// <returns>The generated method implementation.</returns>
    [SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification =
            "The ArgumentOutOfRangeException intentionally reports the offending model property (ReturnTypeMetadata) rather than a method parameter.")]
    private static string BuildRefitMethod(
        MethodModel methodModel,
        bool isTopLevel,
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames,
        string requestBuilderFieldName,
        string settingsFieldName,
        EnumFormatterScope enumFormatterScope)
    {
        if (interfaceModel.GeneratedRequestBuilding && methodModel.Request.CanGenerateInline)
        {
            return BuildInlineRefitMethod(methodModel, interfaceModel, isTopLevel, settingsFieldName, uniqueNames, enumFormatterScope);
        }

        var locals = CreateMethodLocalNameBuilder(methodModel.Parameters);
        var argumentsLocal = locals.New("refitArguments");
        var requestBuilderLocal = locals.New("refitRequestBuilder");
        var funcLocal = locals.New("refitFunc");

        var (typeParameterFieldSource, cachedTypeParameterFieldName) = BuildTypeParameterField(
            methodModel,
            uniqueNames);
        var returnType = methodModel.ReturnType;
        var (isAsync, @return, configureAwait) = GetReturnInvocationParts(methodModel.ReturnTypeMetadata);
        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        var lookupName = StripExplicitInterfacePrefix(methodModel.Name);
        var typeParameterExpression = BuildTypeParameterExpression(methodModel.Parameters, cachedTypeParameterFieldName);
        var genericTypesArgument = BuildGenericTypesArgument(methodModel);
        var returnStatement = BuildRefitReturnStatement(methodModel, @return, returnType, configureAwait, funcLocal, argumentsLocal);
        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);
        var requestBuilderInit =
            $"{requestBuilderFieldName} ?? throw new global::System.InvalidOperationException(\"This generated Refit method requires a request builder.\")";

        return typeParameterFieldSource
            + BuildMethodOpening(
                methodModel,
                isExplicit,
                isExplicit,
                interfaceModel.SupportsNullable,
                isAsync,
                BuildReflectionFallbackSuppressions(
                    methodModel.RequiresUnreferencedCode,
                    methodModel.RequiresDynamicCode))
            + $$"""
                {{bodyIndent}}var {{argumentsLocal}} = {{BuildArgumentsArrayLiteral(methodModel)}};
                {{bodyIndent}}var {{requestBuilderLocal}} = {{requestBuilderInit}};
                {{bodyIndent}}var {{funcLocal}} = {{requestBuilderLocal}}.BuildRestResultFuncForMethod("{{lookupName}}", {{typeParameterExpression}}{{genericTypesArgument}} );

                {{returnStatement}}{{methodIndent}}}

                """;
    }

    /// <summary>Builds a Refit method that constructs the request directly in generated code.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    /// <returns>The generated inline method implementation.</returns>
    private static string BuildInlineRefitMethod(
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isTopLevel,
        string settingsFieldName,
        UniqueNameBuilder uniqueNames,
        EnumFormatterScope enumFormatterScope)
    {
        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        var request = methodModel.Request;
        var locals = CreateMethodLocalNameBuilder(methodModel.Parameters);
        var settingsLocal = locals.New("refitSettings");
        var requestLocal = locals.New("refitRequest");
        var adapterTokenLocal = locals.New("refitAdapterToken");
        var bodyParameter = FindRequestParameter(request, RequestParameterKind.Body);
        var cancellationTokenExpression = BuildCancellationTokenExpression(request);
        var bufferBodyExpression = BuildBufferBodyExpression(bodyParameter, settingsLocal);

        // Build path. Assigning the unique field names and emitting the attribute-provider fields is a single pass
        // over the parameters (the two were previously separate loops sharing the same NeedsAttributeProvider filter).
        var paramInfoSb = new PooledStringBuilder();
        var parameterInfoNames = BuildParameterInfoFields(request, uniqueNames, methodModel.DeclaredMethod, paramInfoSb);

        var emission = new InlineValueEmission(
            locals.New("refitQueryBuilder"),
            locals.New("refitQueryValue"),
            settingsLocal,
            locals.New("refitUseDefaultFormatting"),
            locals.New("refitUseDefaultFormFormatting"),
            enumFormatterScope,
            paramInfoSb);
        var parameters = GetParametersArg(request, parameterInfoNames, emission);

        var pathExpression = BuildInlinePathExpression(request, parameterInfoNames, emission, settingsLocal, parameters);

        var bodyIndent = Indent(MethodBodyIndentation);
        var requestPathExpression = pathExpression;

        // Accumulate the request prologue through a pooled buffer rather than reallocating the whole string on each
        // '+=' branch below.
        var prologue = new PooledStringBuilder();
        if (NeedsFormattingLocal(request))
        {
            _ = prologue.Append(bodyIndent).Append("var ").Append(emission.UseDefaultFormattingLocal)
                .Append(" = global::Refit.GeneratedRequestRunner.UsesDefaultUrlParameterFormatting(")
                .Append(settingsLocal).AppendLine(");");
        }

        if (NeedsFormFormattingLocal(request))
        {
            _ = prologue.Append(bodyIndent).Append("var ").Append(emission.UseDefaultFormFormattingLocal)
                .Append(" = global::Refit.GeneratedRequestRunner.UsesDefaultFormUrlEncodedParameterFormatting(")
                .Append(settingsLocal).AppendLine(");");
        }

        if (HasQueryBindings(request))
        {
            _ = prologue.Append(bodyIndent).Append("var ").Append(emission.QueryBuilderLocal)
                .Append(" = new global::Refit.GeneratedQueryStringBuilder(").Append(pathExpression).AppendLine(");")
                .Append(BuildInlineQueryStatements(request, parameterInfoNames, emission));
            requestPathExpression = emission.QueryBuilderLocal + ".Build()";
        }

        var requestPrologueSource = prologue.ToString();

        var httpMethodExpression = ToHttpMethodExpression(request.HttpMethod);
        var requestUriExpression =
            $"global::Refit.GeneratedRequestRunner.BuildRelativeUri(this.Client, {requestPathExpression}, {settingsLocal}.UrlResolution)";
        var (formFieldsSource, formFieldsFieldName) = BuildFormFieldsField(
            bodyParameter,
            uniqueNames,
            interfaceModel.SupportsNullable,
            interfaceModel.SupportsStaticLambdas);
        var contentSource = bodyParameter is null
            ? string.Empty
            : BuildInlineContent(bodyParameter, requestLocal, settingsLocal, formFieldsFieldName, interfaceModel.SupportsNullable, emission, locals);
        var headerSource = BuildInlineHeaders(request, requestLocal);
        var requestPropertySource = BuildInlineRequestProperties(request, interfaceModel, requestLocal, settingsLocal);
        var returnSource = BuildInlineReturn(methodModel, request, bufferBodyExpression, cancellationTokenExpression, requestLocal, settingsLocal, adapterTokenLocal);
        var methodIndent = Indent(MethodMemberIndentation);

        return $$"""
            {{paramInfoSb}}{{formFieldsSource}}{{BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable)}}{{bodyIndent}}var {{settingsLocal}} = {{settingsFieldName}};
            {{requestPrologueSource}}{{bodyIndent}}var {{requestLocal}} = new global::System.Net.Http.HttpRequestMessage({{httpMethodExpression}}, {{requestUriExpression}});
            {{bodyIndent}}#if NET6_0_OR_GREATER
            {{bodyIndent}}{{requestLocal}}.Version = {{settingsLocal}}.Version;
            {{bodyIndent}}{{requestLocal}}.VersionPolicy = {{settingsLocal}}.VersionPolicy;
            {{bodyIndent}}#endif
            {{contentSource}}{{headerSource}}{{requestPropertySource}}{{returnSource}}{{methodIndent}}}

            """;
    }

    /// <summary>Builds the unique cached attribute-provider field name for a path parameter.</summary>
    /// <param name="parameterName">The source parameter name.</param>
    /// <param name="uniqueNames">The unique member name builder for the interface scope.</param>
    /// <returns>The unique generated field name.</returns>
    private static string GetParameterInfoFieldName(string parameterName, UniqueNameBuilder uniqueNames) =>
        uniqueNames.New($"______{parameterName}AttributeProvider");

    /// <summary>Appends a separator before all but the first element.</summary>
    /// <param name="i">The zero-based element index.</param>
    /// <param name="sb">The target builder.</param>
    /// <param name="separator">The separator to append.</param>
    /// <returns>The same builder for chaining.</returns>
    private static PooledStringBuilder AppendSeparator(int i, PooledStringBuilder sb, string separator = ", ")
    {
        return i <= 0 ? sb : sb.Append(separator);
    }

    /// <summary>Appends a value, prefixed by a separator for all but the first element.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="i">The zero-based element index.</param>
    /// <param name="sb">The target builder.</param>
    /// <param name="separator">The separator to append before the value.</param>
    /// <returns>The same builder for chaining.</returns>
    private static PooledStringBuilder AppendJoining(string value, int i, PooledStringBuilder sb, string separator = ", ")
    {
        return AppendSeparator(i, sb, separator).Append(value);
    }

    /// <summary>Appends a C# attribute construction expression to the builder.</summary>
    /// <param name="attribute">The attribute model to render.</param>
    /// <param name="sb0">The target builder.</param>
    private static void AppendAttributeValue(ParameterAttributeModel attribute, PooledStringBuilder sb0)
    {
        _ = sb0.Append("new ").Append(attribute.TypeExpression).Append('(');
        var i = 0;

        foreach (var argument in attribute.ConstructorArguments)
        {
            _ = AppendJoining(argument, i++, sb0);
        }

        _ = sb0.Append(')');
        if (attribute.NamedArguments.Count < 1)
        {
            return;
        }

        i = 0;
        _ = sb0.Append("{ ");
        foreach (var named in attribute.NamedArguments)
        {
            _ = AppendSeparator(i++, sb0);
            _ = sb0.Append(named.Name).Append(" = ").Append(named.ValueExpression);
        }

        _ = sb0.Append(" }");
    }

    /// <summary>Emits the cached attribute-provider field for a single path parameter.</summary>
    /// <param name="parameter">The path parameter model.</param>
    /// <param name="method">The declaring method name, used for the generated documentation.</param>
    /// <param name="paramInfoFieldName">The unique generated field name.</param>
    /// <param name="sb">The target builder.</param>
    private static void BuildParameterInfoField(RequestParameterModel parameter, string method, string paramInfoFieldName, PooledStringBuilder sb)
    {
        // Build the initializer.
        var memberIndent = Indent(MethodMemberIndentation);
        Dictionary<string, List<ParameterAttributeModel>> grouped = new();

        foreach (var attribute in parameter.Attributes)
        {
            var key = $"typeof({attribute.TypeExpression})";
            if (grouped.TryGetValue(key, out var groupedAttributes))
            {
                groupedAttributes.Add(attribute);
            }
            else
            {
                grouped.Add(key, [attribute]);
            }
        }

        const string dictType = "global::System.Collections.Generic.Dictionary<global::System.Type, object[]>";
        _ = sb.AppendLine().Append(memberIndent).Append("/// <summary>Cached attribute provider for the generated ")
            .Append(ToXmlDocumentationText(method)).Append(" method's ").Append(ToXmlDocumentationText(parameter.Name)).AppendLine(" parameter.</summary>")
            .Append(memberIndent).Append("private static readonly global::Refit.GeneratedParameterAttributeProvider ").Append(paramInfoFieldName).Append(" = ")
            .Append("new global::Refit.GeneratedParameterAttributeProvider(new ").Append(dictType).Append("()");
        var i = 0;
        if (grouped.Count > 0)
        {
            _ = sb.Append(" {");
            foreach (var kv in grouped)
            {
                _ = AppendJoining("{ ", i++, sb).Append(kv.Key).Append(", new object[] { ");
                var argIndex = 0;
                foreach (var arg in kv.Value)
                {
                    // Multiple attributes of the same type must be comma-separated inside the array.
                    _ = AppendSeparator(argIndex++, sb);
                    AppendAttributeValue(arg, sb);
                }

                _ = sb.Append("} }");
            }

            _ = sb.Append('}');
        }

        _ = sb.AppendLine(");");
    }

    /// <summary>Assigns the unique cached field name for each attribute-provider parameter and emits its field.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="uniqueNames">The unique member name builder for the interface scope.</param>
    /// <param name="declaredMethod">The declared method name the emitted fields are scoped to.</param>
    /// <param name="paramInfoSb">The builder receiving the emitted attribute-provider fields.</param>
    /// <returns>A map of parameter name to its cached attribute-provider field name.</returns>
    private static Dictionary<string, string> BuildParameterInfoFields(
        RequestModel request,
        UniqueNameBuilder uniqueNames,
        string declaredMethod,
        PooledStringBuilder paramInfoSb)
    {
        var dict = new Dictionary<string, string>();
        foreach (var parameter in request.Parameters)
        {
            if (!NeedsAttributeProvider(parameter))
            {
                continue;
            }

            var parameterInfoFieldName = GetParameterInfoFieldName(parameter.Name, uniqueNames);
            dict.Add(parameter.Name, parameterInfoFieldName);
            BuildParameterInfoField(parameter, declaredMethod, parameterInfoFieldName, paramInfoSb);
        }

        return dict;
    }

    /// <summary>Builds the additional arguments passed to the generated request path builder.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="uniqueNameLookup">The map of parameter name to cached attribute-provider field name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The generated argument list fragment.</returns>
    private static string GetParametersArg(
        RequestModel request,
        Dictionary<string, string> uniqueNameLookup,
        in InlineValueEmission emission)
    {
        // A single pre-encoded path parameter switches every replacement to the overload carrying the
        // per-value encoding flag, because a params call cannot mix tuple arities.
        var anyPreEncoded = HasPreEncodedPathParameter(request);

        var pathLength = request.Path.Length;
        var replacements = new List<PathReplacement>();
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind is not RequestParameterKind.Path)
            {
                continue;
            }

            var providerField = uniqueNameLookup[parameter.Name];

            // A dotted {param.Prop} object parameter fills each placeholder with a formatted property value.
            if (parameter.PathObjectBindings is { } bindings)
            {
                foreach (var binding in bindings)
                {
                    var bindingValue = BuildPathValueExpressionCore(
                        "@" + parameter.Name + "." + binding.PropertyClrName,
                        binding.PropertyType,
                        binding.ValueFormat,
                        binding.PropertyCanBeNull,
                        providerField,
                        emission);
                    replacements.Add(new(
                        binding.Location.Start.GetOffset(pathLength),
                        binding.Location.End.GetOffset(pathLength),
                        bindingValue,
                        PreEncoded: false));
                }

                continue;
            }

            if (parameter.Locations is null)
            {
                continue;
            }

            var valueExpression = BuildPathValueExpression(parameter, providerField, emission);
            foreach (var location in parameter.Locations)
            {
                replacements.Add(new(
                    location.Start.GetOffset(pathLength),
                    location.End.GetOffset(pathLength),
                    valueExpression,
                    parameter.PreEncoded));
            }
        }

        // BuildRequestPath fills the template left-to-right and slices between consecutive replacements, so they must
        // be ordered by template position. Parameter order does not match template order when an object binding (or a
        // later parameter) fills an earlier placeholder, so sort here rather than relying on declaration order.
        replacements.Sort(static (left, right) => left.Start.CompareTo(right.Start));

        var parametersSb = new PooledStringBuilder();
        foreach (var replacement in replacements)
        {
            AppendPathTuple(
                parametersSb,
                replacement.Start,
                replacement.End,
                replacement.Value,
                anyPreEncoded,
                replacement.PreEncoded);
        }

        return parametersSb.ToString();
    }

    /// <summary>Determines whether any path parameter passes its value through pre-encoded.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns><see langword="true"/> when a path parameter carries <c>[Encoded]</c>.</returns>
    private static bool HasPreEncodedPathParameter(RequestModel request)
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
    private static void AppendPathTuple(
        PooledStringBuilder sb,
        int start,
        int end,
        string valueExpression,
        bool includePreEncoded,
        bool preEncoded)
    {
        _ = sb.Append(", ").Append("((").Append(start).Append(", ").Append(end).Append("), ").Append(valueExpression);
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
    private static string BuildInlinePathExpression(
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
    private static string? TryBuildInlinePathFastExpression(
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
            || (!valueFormat.IsUrlSafeSpanFormattable && !valueFormat.IsSpanFormattableEscapable))
        {
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
        var customExpression =
            $"{runner}({template}, {allowUnmatched}, (({start}, {end}), {settingsLocal}.UrlParameterFormatter.Format({valueExpression}, {providerField}, typeof({pathParameter.Type}))))";

        return $"({emission.UseDefaultFormattingLocal} ? {fastExpression} : {customExpression})";
    }

    /// <summary>Builds request content assignment for an inline generated method.</summary>
    /// <param name="bodyParameter">The body parameter model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="formFieldsFieldName">The cached form field descriptor array name, or null to use the reflection path.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <param name="locals">The method-scope unique local name builder.</param>
    /// <returns>The generated content assignment.</returns>
    private static string BuildInlineContent(
        RequestParameterModel bodyParameter,
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
    private static string BuildInlineFormUnroll(
        RequestParameterModel bodyParameter,
        string requestLocal,
        bool supportsNullable,
        in InlineValueEmission emission,
        UniqueNameBuilder locals)
    {
        var settingsLocal = emission.SettingsLocal;
        var bodyIndent = Indent(MethodBodyIndentation);
        var inner = bodyIndent + "    ";
        var fields = bodyParameter.FormFields!.AsArray();
        var bodyExpr = "@" + bodyParameter.Name;
        var entriesLocal = locals.New("______formEntries");

        // Nullable reference annotations are a C# 8 feature; older consumers get the unannotated types, which also match
        // the .NET Framework/netstandard FormUrlEncodedContent constructor signature. The generated code stays
        // compilable down to the C# 7.3 floor (explicit KeyValuePair construction, != null guards - no C# 9 syntax).
        var nullable = supportsNullable ? "?" : string.Empty;
        var kvpType = "global::System.Collections.Generic.KeyValuePair<string" + nullable + ", string" + nullable + ">";
        var site = new FormUnrollSite(bodyExpr, entriesLocal, inner, "new " + kvpType, locals);

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
    private static void AppendFormFieldUnroll(
        PooledStringBuilder sb,
        FormFieldModel field,
        in FormUnrollSite site,
        in InlineValueEmission emission)
    {
        var indent = site.Indentation;
        var valueLocal = site.Locals.New("______formValue");
        var keyExpr = "global::Refit.GeneratedRequestRunner.BuildQueryKey("
            + emission.SettingsLocal + ", "
            + ToCSharpStringLiteral(field.PropertyName) + ", "
            + ToNullableCSharpStringLiteral(field.ExplicitName) + ", "
            + ToNullableCSharpStringLiteral(field.PrefixSegment) + ")";

        _ = sb.Append(indent).Append("var ").Append(valueLocal).Append(" = ").Append(site.BodyExpr).Append(".@").Append(field.PropertyName).AppendLine(";");

        var valueExpr = BuildFormFieldValueExpression(field, valueLocal, emission);

        // A non-nullable value type is always present, so it is added unconditionally.
        if (!field.CanBeNull)
        {
            AppendFormEntryAdd(sb, in site, indent, keyExpr, valueExpr);
            return;
        }

        // "!= null" (not the C# 9 "is not null" pattern) keeps the emitted null guard compilable down to C# 7.3.
        var childIndent = indent + "    ";
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
    private static void AppendFormEntryAdd(PooledStringBuilder sb, in FormUnrollSite site, string indent, string keyExpr, string valueExpr) =>
        _ = sb.Append(indent).Append(site.EntriesLocal).Append(".Add(").Append(site.KvpNew)
            .Append('(').Append(keyExpr).Append(", ").Append(valueExpr).AppendLine("));");

    /// <summary>Builds the value expression for one scalar form field, matching the configured form formatter.</summary>
    /// <param name="field">The form field descriptor.</param>
    /// <param name="valueLocal">The non-null value local name.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The rendering expression, branching to the formatter when it is customized.</returns>
    private static string BuildFormFieldValueExpression(
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
    private static bool IsUnrollableFormBody(RequestParameterModel? bodyParameter)
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
    private static bool FormBodyHasFastPath(RequestParameterModel? bodyParameter)
    {
        if (!IsUnrollableFormBody(bodyParameter))
        {
            return false;
        }

        foreach (var field in bodyParameter!.FormFields!)
        {
            if (field.ValueFormat!.Kind != InlineFormatKind.FormatterOnly)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the cached form field descriptor array declaration for a URL-encoded body, if eligible.</summary>
    /// <param name="bodyParameter">The body parameter model, or null when the method has no body.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <param name="supportsStaticLambdas">Whether the consumer compilation supports static lambda syntax.</param>
    /// <returns>The generated field declaration and its name, or empty values when the reflection path is used.</returns>
    private static (string Source, string? FieldName) BuildFormFieldsField(
        RequestParameterModel? bodyParameter,
        UniqueNameBuilder uniqueNames,
        bool supportsNullable,
        bool supportsStaticLambdas)
    {
        // An all-scalar body is serialized straight-line by BuildInlineFormUnroll and needs no descriptor array.
        if (IsUnrollableFormBody(bodyParameter))
        {
            return (string.Empty, null);
        }

        if (bodyParameter?.FormFields is not { Count: > 0 } formFields)
        {
            return (string.Empty, null);
        }

        var fields = formFields.AsArray();
        var bodyType = bodyParameter.Type;
        var fieldName = uniqueNames.New(FormFieldsVariableName);
        var elementIndent = Indent(MethodBodyIndentation);

        // The getter lambda degrades to the consumer's language version: 'static' is C# 9 and the 'object?' cast
        // annotation is C# 8, so both are omitted below those versions to keep generation compilable at the C# 7.3 floor.
        var getterOpen = ">(" + (supportsStaticLambdas ? "static " : string.Empty)
            + "body => (" + (supportsNullable ? "object?" : "object") + ")body.@";

        var elementsLength = 0;
        for (var i = 0; i < fields.Length; i++)
        {
            elementsLength += MeasureFormFieldElement(fields[i], bodyType, getterOpen.Length, elementIndent.Length);
        }

        var elements = CreateGeneratedString(
            elementsLength,
            (fields, bodyType, elementIndent, getterOpen),
            static (destination, state) =>
            {
                var position = 0;
                var (elementFields, type, indent, getter) = state;
                for (var i = 0; i < elementFields.Length; i++)
                {
                    var field = elementFields[i];
                    AppendText(destination, indent, ref position);
                    AppendText(destination, FormFieldNew, ref position);
                    AppendText(destination, type, ref position);
                    AppendText(destination, getter, ref position);
                    AppendText(destination, field.PropertyName, ref position);
                    AppendText(destination, FormFieldNameOpen, ref position);
                    AppendText(destination, field.PropertyName, ref position);
                    AppendText(destination, FormFieldNameClose, ref position);
                    AppendLiteralOrNull(destination, field.ExplicitName, ref position);
                    AppendText(destination, ArgumentSeparator, ref position);
                    AppendLiteralOrNull(destination, field.PrefixSegment, ref position);
                    AppendText(destination, ArgumentSeparator, ref position);
                    AppendLiteralOrNull(destination, field.Format, ref position);
                    AppendText(destination, ArgumentSeparator, ref position);
                    if (field.CollectionFormatValue is { } collectionFormatValue)
                    {
                        AppendText(destination, CollectionFormatCast, ref position);
                        AppendInt32(destination, collectionFormatValue, ref position);
                    }
                    else
                    {
                        AppendText(destination, NullLiteral, ref position);
                    }

                    AppendText(destination, ArgumentSeparator, ref position);
                    AppendText(destination, field.SerializeNull ? TrueLiteral : FalseLiteral, ref position);
                    AppendText(destination, FormFieldClose, ref position);
                }
            });

        var memberIndent = Indent(MethodMemberIndentation);
        var source = $$"""


            {{memberIndent}}/// <summary>Cached form field descriptors used to serialize the URL-encoded request body without reflection.</summary>
            {{memberIndent}}private static readonly global::Refit.FormField<{{bodyType}}>[] {{fieldName}} = new global::Refit.FormField<{{bodyType}}>[]
            {{memberIndent}}{
            {{elements}}{{memberIndent}}};
            """;
        return (source, fieldName);
    }

    /// <summary>Measures the rendered length of one generated form field element line.</summary>
    /// <param name="field">The form field descriptor.</param>
    /// <param name="bodyType">The fully-qualified body type.</param>
    /// <param name="getterOpenLength">The length of the language-version-specific getter lambda opening.</param>
    /// <param name="indentLength">The element indentation length.</param>
    /// <returns>The number of characters the rendered element occupies.</returns>
    private static int MeasureFormFieldElement(FormFieldModel field, string bodyType, int getterOpenLength, int indentLength) =>
        indentLength
        + FormFieldNew.Length
        + bodyType.Length
        + getterOpenLength
        + field.PropertyName.Length
        + FormFieldNameOpen.Length
        + field.PropertyName.Length
        + FormFieldNameClose.Length
        + LiteralOrNullLength(field.ExplicitName)
        + ArgumentSeparator.Length
        + LiteralOrNullLength(field.PrefixSegment)
        + ArgumentSeparator.Length
        + LiteralOrNullLength(field.Format)
        + ArgumentSeparator.Length
        + (field.CollectionFormatValue is { } collectionFormatValue
            ? CollectionFormatCast.Length + Int32Length(collectionFormatValue)
            : NullLiteral.Length)
        + ArgumentSeparator.Length
        + (field.SerializeNull ? TrueLiteral.Length : FalseLiteral.Length)
        + FormFieldClose.Length;

    /// <summary>Builds the return statement for an inline generated Refit method.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="bufferBodyExpression">The expression indicating whether request content should be buffered.</param>
    /// <param name="cancellationTokenExpression">The cancellation token expression.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="adapterTokenLocal">The lambda parameter name for the return-type adapter's cancellation token.</param>
    /// <returns>The generated return statement.</returns>
    private static string BuildInlineReturn(
        MethodModel methodModel,
        RequestModel request,
        string bufferBodyExpression,
        string cancellationTokenExpression,
        string requestLocal,
        string settingsLocal,
        string adapterTokenLocal)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        if (request.AdapterTypeExpression is { } adapterType)
        {
            // The adapter surfaces the call synchronously and receives a deferred send. The request is built eagerly
            // and captured, so the deferred call is single-use (a second invocation sends a disposed request).
            return $$"""
                {{bodyIndent}}return new {{adapterType}}().Adapt(({{adapterTokenLocal}}) => global::Refit.GeneratedRequestRunner.SendAsync<{{request.ResultType}}, {{request.DeserializedResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    {{requestLocal}},
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.IsApiResponse)}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.ShouldDisposeResponse)}},
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{adapterTokenLocal}}));

                """;
        }

        if (methodModel.ReturnTypeMetadata == ReturnTypeInfo.AsyncVoid)
        {
            return $$"""
                {{bodyIndent}}return global::Refit.GeneratedRequestRunner.SendVoidAsync(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    {{requestLocal}},
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{cancellationTokenExpression}});

                """;
        }

        if (methodModel.ReturnTypeMetadata == ReturnTypeInfo.AsyncEnumerable)
        {
            return $$"""
                {{bodyIndent}}return global::Refit.GeneratedRequestRunner.StreamAsync<{{request.ResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    {{requestLocal}},
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    {{cancellationTokenExpression}});

                """;
        }

        return methodModel.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal)
            ? $$"""
                {{bodyIndent}}return new {{methodModel.ReturnType}}(global::Refit.GeneratedRequestRunner.SendAsync<{{request.ResultType}}, {{request.DeserializedResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    {{requestLocal}},
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.IsApiResponse)}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.ShouldDisposeResponse)}},
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{cancellationTokenExpression}}));

                """
            : $$"""
                {{bodyIndent}}return global::Refit.GeneratedRequestRunner.SendAsync<{{request.ResultType}}, {{request.DeserializedResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    {{requestLocal}},
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.IsApiResponse)}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.ShouldDisposeResponse)}},
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{cancellationTokenExpression}});

                """;
    }

    /// <summary>Builds static and dynamic header application for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <returns>The generated header statements.</returns>
    private static string BuildInlineHeaders(RequestModel request, string requestLocal)
    {
        var parts = new string[request.StaticHeaders.Count + request.Parameters.Count];
        var count = 0;
        var bodyIndent = Indent(MethodBodyIndentation);
        foreach (var header in request.StaticHeaders)
        {
            parts[count++] =
                $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetHeader({requestLocal}, {ToCSharpStringLiteral(header.Name)}, {ToNullableCSharpStringLiteral(header.Value)});\n";
        }

        foreach (var parameter in request.Parameters)
        {
            switch (parameter.Kind)
            {
                case RequestParameterKind.Header:
                    {
                        // An [Authorize] parameter carries a "{scheme} " prefix; a plain [Header] has none.
                        var headerValueExpression = parameter.HeaderValuePrefix is { } valuePrefix
                            ? $"{ToCSharpStringLiteral(valuePrefix)} + {BuildHeaderValueExpression(parameter)}"
                            : BuildHeaderValueExpression(parameter);
                        parts[count++] =
                            $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetHeader({requestLocal}, {ToCSharpStringLiteral(parameter.HeaderName)}, {headerValueExpression});\n";
                        continue;
                    }

                case RequestParameterKind.HeaderCollection:
                    {
                        parts[count++] =
                            $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddHeaderCollection({requestLocal}, @{parameter.Name});\n";
                        break;
                    }

                default:
                    {
                        // All other parameter kinds (Path, Query, Body, Property, CancellationToken, ...)
                        // are not headers and contribute nothing to header application here.
                        break;
                    }
            }
        }

        return count == 0 ? string.Empty : ConcatParts(parts, count);
    }

    /// <summary>Builds a header value expression without null-conditionals on non-nullable value types.</summary>
    /// <param name="parameter">The header parameter to format.</param>
    /// <returns>The generated header value expression.</returns>
    private static string BuildHeaderValueExpression(RequestParameterModel parameter)
    {
        var parameterExpression = $"@{parameter.Name}";
        return parameter.CanBeNull
            ? $"{parameterExpression}?.ToString()"
            : $"{parameterExpression}.ToString()";
    }

    /// <summary>Builds request-option/property application for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <returns>The generated request option/property statements.</returns>
    private static string BuildInlineRequestProperties(
        RequestModel request,
        InterfaceModel interfaceModel,
        string requestLocal,
        string settingsLocal)
    {
        var parts = new string[1 + interfaceModel.Properties.Count + request.Parameters.Count];
        var count = 0;
        var bodyIndent = Indent(MethodBodyIndentation);
        parts[count++] =
            $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions({requestLocal}, {settingsLocal}, typeof({interfaceModel.InterfaceDisplayName}));\n";

        foreach (var property in interfaceModel.Properties)
        {
            if (property.RequestPropertyKey.Length == 0 || !property.HasGetter)
            {
                continue;
            }

            parts[count++] =
                $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddRequestProperty<{property.Type}>"
                + $"({requestLocal}, {ToCSharpStringLiteral(property.RequestPropertyKey)}, "
                + $"{BuildPropertyAccessExpression(property)});\n";
        }

        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Property)
            {
                parts[count++] =
                    $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddRequestProperty<{parameter.Type}>({requestLocal}, {ToCSharpStringLiteral(parameter.PropertyKey)}, @{parameter.Name});\n";
            }
        }

        return ConcatParts(parts, count);
    }

    /// <summary>Creates a name builder reserved with a method's parameter names so generated locals never collide with them.</summary>
    /// <param name="parameters">The method parameters whose names must be avoided.</param>
    /// <returns>A <see cref="UniqueNameBuilder"/> seeded with the parameter names.</returns>
    private static UniqueNameBuilder CreateMethodLocalNameBuilder(ImmutableEquatableArray<ParameterModel> parameters)
    {
        var builder = new UniqueNameBuilder();
        foreach (var parameter in parameters)
        {
            builder.Reserve(parameter.MetadataName);
        }

        return builder;
    }

    /// <summary>Finds the first request parameter of the given kind.</summary>
    /// <param name="request">The request model to inspect.</param>
    /// <param name="kind">The parameter kind to find.</param>
    /// <returns>The parameter model, if present.</returns>
    private static RequestParameterModel? FindRequestParameter(RequestModel request, RequestParameterKind kind)
    {
        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == kind)
            {
                return parameter;
            }
        }

        return null;
    }

    /// <summary>Builds the cancellation token expression for an inline generated method.</summary>
    /// <param name="request">The request model to inspect.</param>
    /// <returns>The cancellation token expression.</returns>
    private static string BuildCancellationTokenExpression(RequestModel request)
    {
        var cancellationToken = FindRequestParameter(request, RequestParameterKind.CancellationToken);
        if (cancellationToken is null)
        {
            return "global::System.Threading.CancellationToken.None";
        }

        return cancellationToken.CanBeNull
            ? $"@{cancellationToken.Name}.GetValueOrDefault()"
            : $"@{cancellationToken.Name}";
    }

    /// <summary>Builds the body serialization enum expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter.</param>
    /// <returns>The serialization method expression.</returns>
    private static string BuildBodySerializationMethodExpression(RequestParameterModel bodyParameter)
    {
        var serializationMethod = bodyParameter.BodySerializationMethod == "Json"
            ? "Serialized"
            : bodyParameter.BodySerializationMethod;
        return $"global::Refit.BodySerializationMethod.{serializationMethod}";
    }

    /// <summary>The shared locals and rendered fragments used to emit one unrolled form body.</summary>
    /// <param name="BodyExpr">The body value expression.</param>
    /// <param name="EntriesLocal">The form entry list local name.</param>
    /// <param name="Indentation">The statement indentation.</param>
    /// <param name="KvpNew">The <c>new KeyValuePair&lt;...&gt;</c> constructor prefix, nullable-annotated per language version.</param>
    /// <param name="Locals">The method-scope unique local name builder.</param>
    private readonly record struct FormUnrollSite(
        string BodyExpr,
        string EntriesLocal,
        string Indentation,
        string KvpNew,
        UniqueNameBuilder Locals);

    /// <summary>One placeholder replacement in the request path template, ordered by its start offset.</summary>
    /// <param name="Start">The placeholder start offset in the template.</param>
    /// <param name="End">The placeholder end offset in the template.</param>
    /// <param name="Value">The generated replacement value expression.</param>
    /// <param name="PreEncoded">Whether the value passes through pre-encoded.</param>
    private readonly record struct PathReplacement(int Start, int End, string Value, bool PreEncoded);
}
