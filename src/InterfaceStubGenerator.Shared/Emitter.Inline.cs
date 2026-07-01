// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
internal static partial class Emitter
{
    /// <summary>The base member name for a cached form field descriptor array.</summary>
    private const string FormFieldsVariableName = "______formFields";

    /// <summary>The opening of a generated <c>FormField</c> construction.</summary>
    private const string FormFieldNew = "new global::Refit.FormField<";

    /// <summary>The getter lambda opening between the body type and the property name.</summary>
    private const string FormFieldGetterOpen = ">(static body => (object?)body.@";

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
        string settingsFieldName)
    {
        if (interfaceModel.GeneratedRequestBuilding && methodModel.Request.CanGenerateInline)
        {
            return BuildInlineRefitMethod(methodModel, interfaceModel, isTopLevel, settingsFieldName, uniqueNames);
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
            + BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable, isAsync)
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
    /// <returns>The generated inline method implementation.</returns>
    private static string BuildInlineRefitMethod(
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isTopLevel,
        string settingsFieldName,
        UniqueNameBuilder uniqueNames)
    {
        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        var request = methodModel.Request;
        var locals = CreateMethodLocalNameBuilder(methodModel.Parameters);
        var settingsLocal = locals.New("refitSettings");
        var requestLocal = locals.New("refitRequest");
        var bodyParameter = FindRequestParameter(request, RequestParameterKind.Body);
        var cancellationTokenExpression = BuildCancellationTokenExpression(request);
        var bufferBodyExpression = BuildBufferBodyExpression(bodyParameter, settingsLocal);

        // Build path
        var parameterInfoNames = GetParameterInfoUniqueNames(request, uniqueNames);
        var parameters = GetParametersArg(request, parameterInfoNames);
        var paramInfoSb = new StringBuilder();

        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind is not RequestParameterKind.Path)
            {
                continue;
            }

            var parameterName = parameterInfoNames[parameter.Name];
            BuildParameterInfoField(parameter, methodModel.DeclaredMethod, parameterName, paramInfoSb);
        }

        var pathExpression = parameters.Length > 0
            ? $"global::Refit.GeneratedRequestRunner.BuildRequestPath({ToCSharpStringLiteral(request.Path)}, {settingsLocal}.AllowUnmatchedRouteParameters{parameters})"
            : ToCSharpStringLiteral(request.Path);
        var requestUriExpression =
            $"global::Refit.GeneratedRequestRunner.BuildRelativeUri(this.Client, {pathExpression}, {settingsLocal}.UrlResolution)";
        var (formFieldsSource, formFieldsFieldName) = BuildFormFieldsField(bodyParameter, uniqueNames);
        var contentSource = bodyParameter is null ? string.Empty : BuildInlineContent(bodyParameter, requestLocal, settingsLocal, formFieldsFieldName);
        var headerSource = BuildInlineHeaders(request, requestLocal);
        var requestPropertySource = BuildInlineRequestProperties(request, interfaceModel, requestLocal, settingsLocal);
        var returnSource = BuildInlineReturn(methodModel, request, bufferBodyExpression, cancellationTokenExpression, requestLocal, settingsLocal);
        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);

        return $$"""
            {{paramInfoSb}}{{formFieldsSource}}{{BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable)}}{{bodyIndent}}var {{settingsLocal}} = {{settingsFieldName}};
            {{bodyIndent}}var {{requestLocal}} = new global::System.Net.Http.HttpRequestMessage({{ToHttpMethodExpression(request.HttpMethod)}}, {{requestUriExpression}});
            {{bodyIndent}}#if NET6_0_OR_GREATER
            {{bodyIndent}}{{requestLocal}}.Version = {{settingsLocal}}.Version;
            {{bodyIndent}}{{requestLocal}}.VersionPolicy = {{settingsLocal}}.VersionPolicy;
            {{bodyIndent}}#endif
            {{contentSource}}{{headerSource}}{{requestPropertySource}}{{returnSource}}{{methodIndent}}}

            """;

        static string GetParameterInfoFieldName(string parameterName, UniqueNameBuilder uniqueNames) =>
            uniqueNames.New($"______{parameterName}AttributeProvider");

        static StringBuilder AppendSeparator(int i, StringBuilder sb, string separator = ", ")
        {
            return i <= 0 ? sb : sb.Append(separator);
        }
        static StringBuilder AppendJoining(string value, int i, StringBuilder sb, string separator = ", ")
        {
            return AppendSeparator(i, sb, separator).Append(value);
        }

        static void AppendAttributeValue(ParameterAttributeModel attribute, StringBuilder sb0)
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
        static void BuildParameterInfoField(RequestParameterModel parameter, string method, string paramInfoFieldName, StringBuilder sb)
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
                    foreach (var arg in kv.Value)
                    {
                        AppendAttributeValue(arg, sb);
                    }

                    _ = sb.Append("} }");
                }

                _ = sb.Append('}');
            }

            _ = sb.AppendLine(");");
        }

        static Dictionary<string, string> GetParameterInfoUniqueNames(
            RequestModel request,
            UniqueNameBuilder uniqueNames)
        {
            var dict = new Dictionary<string, string>();
            foreach (var parameter in request.Parameters)
            {
                if (parameter.Kind is not RequestParameterKind.Path)
                {
                    continue;
                }

                var parameterInfoFieldName = GetParameterInfoFieldName(parameter.Name, uniqueNames);
                dict.Add(parameter.Name, parameterInfoFieldName);
            }

            return dict;
        }
        static string GetParametersArg(RequestModel request, Dictionary<string, string> uniqueNameLookup)
        {
            var parametersSb = new StringBuilder();
            var pathLength = request.Path.Length;
            foreach (var parameter in request.Parameters)
            {
                if (parameter.Kind is not RequestParameterKind.Path || parameter.Locations is null)
                {
                    continue;
                }

                foreach (var location in parameter.Locations)
                {
                    var start = location.Start.GetOffset(pathLength);
                    var end = location.End.GetOffset(pathLength);
                    _ = parametersSb.Append(", ").Append("((").Append(start).Append(", ").Append(end).Append("), ");
                    var parameterInfoFieldName = uniqueNameLookup[parameter.Name];
                    _ = parametersSb.Append("_settings.UrlParameterFormatter.Format(")
                        .Append(parameter.Name)
                        .Append(", ")
                        .Append(parameterInfoFieldName)
                        .Append(", ")
                        .Append("typeof(").Append(parameter.Type).Append(')')
                        .Append(')');
                    _ = parametersSb.Append(')');
                }
            }

            return parametersSb.ToString();
        }
    }

    /// <summary>Builds request content assignment for an inline generated method.</summary>
    /// <param name="bodyParameter">The body parameter model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="formFieldsFieldName">The cached form field descriptor array name, or null to use the reflection path.</param>
    /// <returns>The generated content assignment.</returns>
    private static string BuildInlineContent(RequestParameterModel bodyParameter, string requestLocal, string settingsLocal, string? formFieldsFieldName)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        if (bodyParameter.BodySerializationMethod == "UrlEncoded")
        {
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

    /// <summary>Builds the cached form field descriptor array declaration for a URL-encoded body, if eligible.</summary>
    /// <param name="bodyParameter">The body parameter model, or null when the method has no body.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <returns>The generated field declaration and its name, or empty values when the reflection path is used.</returns>
    private static (string Source, string? FieldName) BuildFormFieldsField(
        RequestParameterModel? bodyParameter,
        UniqueNameBuilder uniqueNames)
    {
        if (bodyParameter?.FormFields is not { Count: > 0 } formFields)
        {
            return (string.Empty, null);
        }

        var fields = formFields.AsArray();
        var bodyType = bodyParameter.Type;
        var fieldName = uniqueNames.New(FormFieldsVariableName);
        var elementIndent = Indent(MethodBodyIndentation);

        var elementsLength = 0;
        for (var i = 0; i < fields.Length; i++)
        {
            elementsLength += MeasureFormFieldElement(fields[i], bodyType, elementIndent.Length);
        }

        var elements = CreateGeneratedString(
            elementsLength,
            (fields, bodyType, elementIndent),
            static (destination, state) =>
            {
                var position = 0;
                var (elementFields, type, indent) = state;
                for (var i = 0; i < elementFields.Length; i++)
                {
                    var field = elementFields[i];
                    AppendText(destination, indent, ref position);
                    AppendText(destination, FormFieldNew, ref position);
                    AppendText(destination, type, ref position);
                    AppendText(destination, FormFieldGetterOpen, ref position);
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
    /// <param name="indentLength">The element indentation length.</param>
    /// <returns>The number of characters the rendered element occupies.</returns>
    private static int MeasureFormFieldElement(FormFieldModel field, string bodyType, int indentLength) =>
        indentLength
        + FormFieldNew.Length
        + bodyType.Length
        + FormFieldGetterOpen.Length
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
    /// <returns>The generated return statement.</returns>
    private static string BuildInlineReturn(
        MethodModel methodModel,
        RequestModel request,
        string bufferBodyExpression,
        string cancellationTokenExpression,
        string requestLocal,
        string settingsLocal)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
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
                        parts[count++] =
                            $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetHeader({requestLocal}, {ToCSharpStringLiteral(parameter.HeaderName)}, {BuildHeaderValueExpression(parameter)});\n";
                        continue;
                    }

                case RequestParameterKind.HeaderCollection:
                    {
                        parts[count++] =
                            $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddHeaderCollection({requestLocal}, @{parameter.Name});\n";
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
        var names = new List<string>(parameters.Count);
        foreach (var parameter in parameters)
        {
            names.Add(parameter.MetadataName);
        }

        builder.Reserve(names);
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
}
