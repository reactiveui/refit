// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

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

    /// <summary>The opening of a generated <c>GeneratedRequestRunner.SetHeader</c> call.</summary>
    private const string RunnerSetHeader = "global::Refit.GeneratedRequestRunner.SetHeader(";

    /// <summary>The settings member read for the header validation flag, plus the call terminator that follows it.</summary>
    private const string ValidateHeadersMember = ".ValidateHeaders";

    /// <summary>Builds the body of the Refit method, appending it to the interface's method buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="fieldNames">The generated request-builder and settings backing-field names.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    internal static void BuildRefitMethod(
        PooledStringBuilder builder,
        MethodModel methodModel,
        bool isTopLevel,
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames,
        GeneratedFieldNames fieldNames,
        EnumFormatterScope enumFormatterScope)
    {
        if (interfaceModel.GeneratedRequestBuilding && methodModel.Request.CanGenerateInline)
        {
            BuildInlineRefitMethod(builder, methodModel, interfaceModel, isTopLevel, fieldNames.Settings, uniqueNames, enumFormatterScope);
            return;
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
            $"{fieldNames.RequestBuilder} ?? throw new global::System.InvalidOperationException(\"This generated Refit method requires a request builder.\")";

        _ = builder
            .Append(typeParameterFieldSource)
            .Append(BuildMethodOpening(
                methodModel,
                isExplicit,
                isExplicit,
                interfaceModel.SupportsNullable,
                isAsync,
                BuildReflectionFallbackSuppressions(
                    methodModel.RequiresUnreferencedCode,
                    methodModel.RequiresDynamicCode)))
            .Append($$"""
                {{bodyIndent}}var {{argumentsLocal}} = {{BuildArgumentsArrayLiteral(methodModel)}};
                {{bodyIndent}}var {{requestBuilderLocal}} = {{requestBuilderInit}};
                {{bodyIndent}}var {{funcLocal}} = {{requestBuilderLocal}}.BuildRestResultFuncForMethod("{{lookupName}}", {{typeParameterExpression}}{{genericTypesArgument}} );

                {{returnStatement}}{{methodIndent}}}

                """);
    }

    /// <summary>Builds a Refit method that constructs the request directly in generated code.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    internal static void BuildInlineRefitMethod(
        PooledStringBuilder builder,
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
            paramInfoSb,
            interfaceModel.SupportsCollectionExpressions);
        var parameters = GetParametersArg(request, parameterInfoNames, emission);
        var pathExpression = BuildInlinePathExpression(request, parameterInfoNames, emission, settingsLocal, parameters);

        var plan = new InlineMethodPlan(
            bodyParameter,
            settingsLocal,
            requestLocal,
            adapterTokenLocal,
            BuildCancellationTokenExpression(request),
            BuildBufferBodyExpression(bodyParameter, settingsLocal),
            pathExpression,
            parameterInfoNames,
            paramInfoSb,
            emission,
            locals);
        BuildInlineRefitMethodBody(builder, methodModel, interfaceModel, isExplicit, settingsFieldName, uniqueNames, plan);
    }

    /// <summary>Assembles the generated inline method from its request-message construction and return statement,
    /// appending it to the interface's method buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isExplicit">Whether the method is emitted as an explicit interface implementation.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="plan">The method-scope locals and pre-built request fragments.</param>
    internal static void BuildInlineRefitMethodBody(
        PooledStringBuilder builder,
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        string settingsFieldName,
        UniqueNameBuilder uniqueNames,
        in InlineMethodPlan plan)
    {
        var fragments = BuildInlineMethodFragments(methodModel, interfaceModel, isExplicit, uniqueNames, plan);

        // A cold IObservable wraps the shared request construction in a per-subscription local function so a second
        // subscription rebuilds and re-sends instead of reusing a disposed request. That shape reuses both the method
        // prefix and the construction block, so it materializes them as strings; every other shape appends its fragments
        // straight into the buffer, so the prefix and construction strings never allocate.
        if (methodModel.ReturnTypeMetadata == ReturnTypeInfo.Observable)
        {
            AppendInlineObservableRefitMethod(builder, methodModel, settingsFieldName, plan, fragments);
            return;
        }

        AppendInlineStandardRefitMethod(builder, methodModel, settingsFieldName, plan, fragments);
    }

    /// <summary>Emits the request-prologue formatting locals and resolves the request-path expression to use.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="plan">The method-scope locals and pre-built request fragments.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <param name="requestPathExpression">The request-path expression: the query builder's <c>Build()</c> call, or the path when no query binds.</param>
    /// <returns>The generated request-prologue source.</returns>
    internal static string BuildInlineRequestPrologue(
        RequestModel request,
        in InlineMethodPlan plan,
        string bodyIndent,
        out string requestPathExpression)
    {
        var emission = plan.Emission;
        var settingsLocal = plan.SettingsLocal;

        // Accumulate the request prologue through a pooled buffer rather than reallocating the whole string on each
        // '+=' branch below.
        var prologue = new PooledStringBuilder();

        // A [Url] method dispatches to the absolute URI its [Url] parameter supplies, bypassing the base address:
        // validate the value and use it as the base the query string is appended to, instead of the (empty) template.
        var basePathExpression = plan.PathExpression;
        var urlParameter = FindRequestParameter(request, RequestParameterKind.Url);
        if (urlParameter is not null)
        {
            var urlLocal = plan.Locals.New("refitUrl");
            _ = prologue.Append(bodyIndent).Append("var ").Append(urlLocal)
                .Append(" = global::Refit.GeneratedRequestRunner.RequireAbsoluteUrl(@")
                .Append(urlParameter.Name).AppendLine(");");
            basePathExpression = urlLocal;
        }

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

        requestPathExpression = AppendInlineQueryPrologue(prologue, request, plan.ParameterInfoNames, emission, basePathExpression, bodyIndent);
        return prologue.ToString();
    }

    /// <summary>Appends the query-string-builder prologue and returns the request-path expression to use.</summary>
    /// <param name="prologue">The request-prologue buffer to append to.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="parameterInfoNames">The per-parameter attribute-provider field names.</param>
    /// <param name="emission">The inline value-emission context.</param>
    /// <param name="pathExpression">The generated relative-path expression.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <returns>The request-path expression: the query builder's <c>Build()</c> call, or the path when no query binds.</returns>
    internal static string AppendInlineQueryPrologue(
        PooledStringBuilder prologue,
        RequestModel request,
        Dictionary<string, string> parameterInfoNames,
        in InlineValueEmission emission,
        string pathExpression,
        string bodyIndent)
    {
        if (!HasQueryBindings(request))
        {
            return pathExpression;
        }

        _ = prologue.Append(bodyIndent).Append("var ").Append(emission.QueryBuilderLocal)
            .Append(" = new global::Refit.GeneratedQueryStringBuilder(").Append(pathExpression);

        // The query separator (? vs &) depends on whether the built path already carries a query. Path values are
        // escaped, so only a pre-encoded ([Encoded]) segment could inject a ? the template lacks; without one the
        // answer is the compile-time presence of ? in the template, passed in so the path is not rescanned per call.
        // A [Url] method's base is a runtime absolute URL whose query cannot be known at compile time, so it scans too.
        if (!HasPreEncodedPathParameter(request) && !HasUrlParameter(request))
        {
            _ = prologue.Append(", ").Append(ToLowerInvariantString(request.Path.IndexOf('?') >= 0));
        }

        _ = prologue.AppendLine(");")
            .Append(BuildInlineQueryStatements(request, parameterInfoNames, emission));
        return emission.QueryBuilderLocal + ".Build()";
    }

    /// <summary>Assembles a cold-observable inline method: a per-subscription request factory and its send.</summary>
    /// <param name="methodPrefix">The method signature, settings assignment, and any field prologue.</param>
    /// <param name="requestConstruction">The shared request-construction block.</param>
    /// <param name="buildRequestLocal">The per-subscription request-building local function name.</param>
    /// <param name="requestLocal">The request-message local name.</param>
    /// <param name="observableReturn">The generated <c>return SendObservable(...)</c> statement.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <param name="methodIndent">The method-member indentation.</param>
    /// <returns>The generated method source.</returns>
    internal static string BuildInlineObservableMethodSource(
        string methodPrefix,
        string requestConstruction,
        string buildRequestLocal,
        string requestLocal,
        string observableReturn,
        string bodyIndent,
        string methodIndent) =>
        $$"""
        {{methodPrefix}}{{bodyIndent}}global::System.Net.Http.HttpRequestMessage {{buildRequestLocal}}()
        {{bodyIndent}}{
        {{requestConstruction}}{{bodyIndent}}    return {{requestLocal}};
        {{bodyIndent}}}
        {{observableReturn}}{{methodIndent}}}

        """;

    /// <summary>Builds the cold-observable return statement: a per-subscription send over the request factory.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="bufferBodyExpression">The generated buffer-body expression.</param>
    /// <param name="cancellationTokenExpression">The generated method cancellation-token expression.</param>
    /// <param name="buildRequestLocal">The per-subscription request-building local function name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <returns>The generated <c>return SendObservable(...)</c> statement.</returns>
    internal static string BuildInlineObservableReturn(
        RequestModel request,
        string bufferBodyExpression,
        string cancellationTokenExpression,
        string buildRequestLocal,
        string settingsLocal)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        return $$"""
            {{bodyIndent}}return global::Refit.GeneratedRequestRunner.SendObservable<{{request.ResultType}}, {{request.DeserializedResultType}}>(
            {{bodyIndent}}    this.Client,
            {{bodyIndent}}    {{buildRequestLocal}},
            {{bodyIndent}}    {{settingsLocal}},
            {{bodyIndent}}    {{ToLowerInvariantString(request.IsApiResponse)}},
            {{bodyIndent}}    {{ToLowerInvariantString(request.ShouldDisposeResponse)}},
            {{bodyIndent}}    {{bufferBodyExpression}},
            {{bodyIndent}}    {{cancellationTokenExpression}});

            """;
    }

    /// <summary>Builds the return statement for a <c>Task&lt;HttpRequestMessage&gt;</c> method that returns the built request.</summary>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <returns>The generated return statement handing the built request back without sending it.</returns>
    internal static string BuildInlineRequestMessageReturn(string requestLocal)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        return $$"""
            {{bodyIndent}}return global::System.Threading.Tasks.Task.FromResult({{requestLocal}});

            """;
    }

    /// <summary>Builds the return statement for an inline generated Refit method.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="bufferBodyExpression">The expression indicating whether request content should be buffered.</param>
    /// <param name="cancellationTokenExpression">The cancellation token expression.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="adapterTokenLocal">The lambda parameter name for the return-type adapter's cancellation token.</param>
    /// <returns>The generated return statement.</returns>
    internal static string BuildInlineReturn(
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

        return methodModel.ReturnTypeMetadata == ReturnTypeInfo.AsyncEnumerable
            ? $$"""
                {{bodyIndent}}return global::Refit.GeneratedRequestRunner.StreamAsync<{{request.ResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    {{requestLocal}},
                {{bodyIndent}}    {{settingsLocal}},
                {{bodyIndent}}    {{cancellationTokenExpression}});

                """
            : BuildInlineSendAsyncReturn(methodModel, request, bufferBodyExpression, cancellationTokenExpression, requestLocal, settingsLocal, bodyIndent);
    }

    /// <summary>Builds the awaited <c>SendAsync</c> return statement for the standard async-result inline path.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="bufferBodyExpression">The request-body buffering expression.</param>
    /// <param name="cancellationTokenExpression">The cancellation token expression.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <returns>The generated return statement.</returns>
    internal static string BuildInlineSendAsyncReturn(
        MethodModel methodModel,
        RequestModel request,
        string bufferBodyExpression,
        string cancellationTokenExpression,
        string requestLocal,
        string settingsLocal,
        string bodyIndent) =>
        methodModel.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal)
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

    /// <summary>Builds static and dynamic header application for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name, read for the header validation flag.</param>
    /// <returns>The generated header statements.</returns>
    internal static string BuildInlineHeaders(RequestModel request, string requestLocal, string settingsLocal)
    {
        // Append directly into a pooled buffer allocated only once a header is actually emitted; the previous
        // string[] + interpolated-fragment + ConcatParts shape allocated the array (and a validate-flag string) even
        // for the common header-less method. The emitted text is identical.
        var bodyIndent = Indent(MethodBodyIndentation);
        PooledStringBuilder? sb = null;
        foreach (var header in request.StaticHeaders)
        {
            sb ??= new PooledStringBuilder();
            var name = ToCSharpStringLiteral(header.Name);
            var value = ToNullableCSharpStringLiteral(header.Value);
            AppendSetHeader(sb, bodyIndent, requestLocal, name, value, settingsLocal);
        }

        foreach (var parameter in request.Parameters)
        {
            switch (parameter.Kind)
            {
                case RequestParameterKind.Header:
                    {
                        // An [Authorize] parameter carries a "{scheme} " prefix; a plain [Header] has none.
                        var headerValueExpression = parameter.HeaderValuePrefix is { } valuePrefix
                            ? ToCSharpStringLiteral(valuePrefix) + " + " + BuildHeaderValueExpression(parameter)
                            : BuildHeaderValueExpression(parameter);
                        sb ??= new PooledStringBuilder();
                        var headerName = ToCSharpStringLiteral(parameter.HeaderName);
                        AppendSetHeader(sb, bodyIndent, requestLocal, headerName, headerValueExpression, settingsLocal);
                        break;
                    }

                case RequestParameterKind.HeaderCollection:
                    {
                        sb ??= new PooledStringBuilder();
                        _ = sb.Append(bodyIndent).Append("global::Refit.GeneratedRequestRunner.AddHeaderCollection(")
                            .Append(requestLocal).Append(ArgumentSeparator).Append("@").Append(parameter.Name)
                            .Append(ArgumentSeparator).Append(settingsLocal).Append(ValidateHeadersMember).Append(");\n");
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

        return sb?.ToString() ?? string.Empty;
    }

    /// <summary>Appends one <c>SetHeader</c> statement directly into the header buffer.</summary>
    /// <param name="sb">The pooled statement buffer.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="nameExpression">The header-name expression.</param>
    /// <param name="valueExpression">The header-value expression.</param>
    /// <param name="settingsLocal">The generated settings local name, read for the header validation flag.</param>
    internal static void AppendSetHeader(
        PooledStringBuilder sb,
        string bodyIndent,
        string requestLocal,
        string nameExpression,
        string valueExpression,
        string settingsLocal) =>
        sb.Append(bodyIndent).Append(RunnerSetHeader).Append(requestLocal).Append(ArgumentSeparator)
            .Append(nameExpression).Append(ArgumentSeparator).Append(valueExpression).Append(ArgumentSeparator)
            .Append(settingsLocal).Append(ValidateHeadersMember).Append(");\n");

    /// <summary>Builds a header value expression without null-conditionals on non-nullable value types.</summary>
    /// <param name="parameter">The header parameter to format.</param>
    /// <returns>The generated header value expression.</returns>
    internal static string BuildHeaderValueExpression(RequestParameterModel parameter)
    {
        var parameterExpression = $"@{parameter.Name}";
        return parameter.CanBeNull
            ? $"{parameterExpression}?.ToString()"
            : $"{parameterExpression}.ToString()";
    }

    /// <summary>Creates a name builder reserved with a method's parameter names so generated locals never collide with them.</summary>
    /// <param name="parameters">The method parameters whose names must be avoided.</param>
    /// <returns>A <see cref="UniqueNameBuilder"/> seeded with the parameter names.</returns>
    internal static UniqueNameBuilder CreateMethodLocalNameBuilder(ImmutableEquatableArray<ParameterModel> parameters)
    {
        var builder = new UniqueNameBuilder();
        foreach (var parameter in parameters)
        {
            builder.Reserve(parameter.MetadataName);
        }

        return builder;
    }

    /// <summary>Determines whether the request dispatches to an absolute URI supplied by a <c>[Url]</c> parameter.</summary>
    /// <param name="request">The request model to inspect.</param>
    /// <returns><see langword="true"/> when a <c>[Url]</c> parameter is present.</returns>
    internal static bool HasUrlParameter(RequestModel request) =>
        FindRequestParameter(request, RequestParameterKind.Url) is not null;

    /// <summary>Builds the relative-URI expression that merges the built path and query onto the client base address.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="requestPathExpression">The built path-and-query expression.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <returns>The generated <c>BuildRelativeUri</c> call.</returns>
    /// <remarks>A <c>[QueryUriFormat]</c> method re-encodes the whole path and query with the attribute's UriFormat,
    /// matching the reflection builder's final GetComponents pass; every other method uses the direct relative URI.</remarks>
    internal static string BuildRelativeUriExpression(RequestModel request, string requestPathExpression, string settingsLocal) =>
        request.QueryUriFormat is { } queryUriFormat
            ? $"global::Refit.GeneratedRequestRunner.BuildRelativeUri(this.Client, {requestPathExpression}, {settingsLocal}.UrlResolution, (global::System.UriFormat){queryUriFormat})"
            : $"global::Refit.GeneratedRequestRunner.BuildRelativeUri(this.Client, {requestPathExpression}, {settingsLocal}.UrlResolution)";

    /// <summary>Finds the first request parameter of the given kind.</summary>
    /// <param name="request">The request model to inspect.</param>
    /// <param name="kind">The parameter kind to find.</param>
    /// <returns>The parameter model, if present.</returns>
    internal static RequestParameterModel? FindRequestParameter(RequestModel request, RequestParameterKind kind)
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
    internal static string BuildCancellationTokenExpression(RequestModel request)
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
    internal static string BuildBodySerializationMethodExpression(RequestParameterModel bodyParameter)
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
    internal readonly record struct FormUnrollSite(
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
    internal readonly record struct PathReplacement(int Start, int End, string Value, bool PreEncoded);

    /// <summary>The method-scope locals and pre-built request fragments threaded through inline method assembly.</summary>
    /// <param name="BodyParameter">The request body parameter, or null.</param>
    /// <param name="SettingsLocal">The generated settings local name.</param>
    /// <param name="RequestLocal">The generated request message local name.</param>
    /// <param name="AdapterTokenLocal">The lambda parameter name for the return-type adapter's cancellation token.</param>
    /// <param name="CancellationTokenExpression">The cancellation token expression.</param>
    /// <param name="BufferBodyExpression">The request-body buffering expression.</param>
    /// <param name="PathExpression">The generated relative-path expression.</param>
    /// <param name="ParameterInfoNames">The per-parameter attribute-provider field names.</param>
    /// <param name="ParamInfoBuilder">The builder holding the emitted attribute-provider fields.</param>
    /// <param name="Emission">The inline value-emission context.</param>
    /// <param name="Locals">The method-scope unique local name builder.</param>
    internal readonly record struct InlineMethodPlan(
        RequestParameterModel? BodyParameter,
        string SettingsLocal,
        string RequestLocal,
        string AdapterTokenLocal,
        string CancellationTokenExpression,
        string BufferBodyExpression,
        string PathExpression,
        Dictionary<string, string> ParameterInfoNames,
        PooledStringBuilder ParamInfoBuilder,
        InlineValueEmission Emission,
        UniqueNameBuilder Locals);
}
