// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Assembles an inline generated Refit method body from its computed request-construction fragments.</summary>
internal static class MethodSignatureEmission
{
    /// <summary>Computes the per-method source fragments shared by every inline generated Refit method shape.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isExplicit">Whether the method is emitted as an explicit interface implementation.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="plan">The method-scope locals and pre-built request fragments.</param>
    /// <returns>The computed request-construction fragments.</returns>
    internal static InlineMethodFragments BuildInlineMethodFragments(
        in MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        UniqueNameBuilder uniqueNames,
        in InlineRequestEmission.InlineMethodPlan plan)
    {
        var request = methodModel.Request;
        var settingsLocal = plan.SettingsLocal;
        var requestLocal = plan.RequestLocal;
        var emission = plan.Emission;
        var innerBodyIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation + 1);

        var requestPrologueSource = InlineRequestEmission.BuildInlineRequestPrologue(request, plan, innerBodyIndent, out var requestPathExpression);
        var (httpMethodFieldSource, httpMethodExpression) = FormFieldEmission.BuildHttpMethodField(request, uniqueNames);

        // A [Url] method dispatches to an absolute URI (the validated [Url] value with any query appended), bypassing
        // the base-address merge; every other method builds a relative URI merged onto the base address.
        var urlKind = InlineRequestEmission.FindRequestParameter(request, RequestParameterKind.Url)
            ?.AllowRelativeUrl is true
            ? "RelativeOrAbsolute"
            : "Absolute";
        var requestUriExpression = InlineRequestEmission.HasUrlParameter(request)
            ? $"new global::System.Uri({requestPathExpression}, global::System.UriKind.{urlKind})"
            : InlineRequestEmission.BuildRelativeUriExpression(request, requestPathExpression, settingsLocal, 1);
        var options = interfaceModel.Options;
        var (formFieldsSource, formFieldsFieldName) = FormFieldEmission.BuildFormFieldsField(plan.BodyParameter, uniqueNames, options.SupportsNullable, options.SupportsStaticLambdas);

        // A multipart method builds a MultipartFormDataContent from its parts; every other method has at most one body
        // parameter (a multipart method never carries one), so the two paths never both apply.
        var contentSource = plan.BodyParameter is null
            ? string.Empty
            : BodyContentEmission.BuildInlineContent(plan, formFieldsFieldName, interfaceModel.Options.SupportsNullable, emission, 1);
        if (request.Body.IsMultipart)
        {
            contentSource = MultipartContentEmission.BuildInlineMultipartContent(request, requestLocal, settingsLocal, plan.Locals, 1);
        }

        var headerSource = InlineRequestEmission.BuildInlineHeaders(request, requestLocal, settingsLocal, 1);
        var requestPropertySource = RequestPropertyEmission.BuildInlineRequestProperties(request, interfaceModel, methodModel, requestLocal, settingsLocal, 1);

        // A method that declares a positive [Timeout] stashes it on the request so the send helpers apply it; every
        // other method emits nothing here and pays no per-call timeout cost.
        var timeoutSource = request.TimeoutMilliseconds > 0
            ? $"{innerBodyIndent}global::Refit.GeneratedRequestRunner.SetRequestTimeout({requestLocal}, {request.TimeoutMilliseconds});\n"
            : string.Empty;
        var opening = LiteralEmissionHelpers.BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.Options.SupportsNullable);

        return new(
            requestPrologueSource,
            httpMethodFieldSource,
            httpMethodExpression,
            requestUriExpression,
            formFieldsSource,
            contentSource,
            headerSource,
            requestPropertySource,
            timeoutSource,
            opening);
    }

    /// <summary>Appends a standard inline generated Refit method: prefix, request construction, and return statement.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isExplicit">Whether the method is emitted as an explicit interface implementation.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="plan">The method-scope locals and pre-built request fragments.</param>
    /// <remarks>Fragments are computed in the same order the observable-path fragment builder uses so the sequence of
    /// unique member/local name allocations (and therefore the generated names) stays byte-for-byte identical. Building a
    /// query- or converter-bound fragment appends helper member declarations into <c>plan.ParamInfoBuilder</c>, so every
    /// fragment is computed before that buffer is drained into <paramref name="builder"/>. The request prologue is the
    /// one block accumulated into a scratch buffer and buffer-copied in below rather than materialized as a string.</remarks>
    internal static void AppendInlineStandardRefitMethod(
        PooledStringBuilder builder,
        in MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        string settingsFieldName,
        UniqueNameBuilder uniqueNames,
        in InlineRequestEmission.InlineMethodPlan plan)
    {
        var request = methodModel.Request;
        var settingsLocal = plan.SettingsLocal;
        var requestLocal = plan.RequestLocal;
        var bodyIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation);
        var methodIndent = SpanBufferEmission.Indent(Emitter.MethodMemberIndentation);
        var httpRequestMessageIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation + 1);

        var prologue = new PooledStringBuilder();
        var requestPathExpression = InlineRequestEmission.AppendInlineRequestPrologue(prologue, request, plan, bodyIndent);
        var (httpMethodFieldSource, httpMethodExpression) = FormFieldEmission.BuildHttpMethodField(request, uniqueNames);

        // A [Url] method dispatches to an absolute URI (the validated [Url] value with any query appended), bypassing
        // the base-address merge; every other method builds a relative URI merged onto the base address.
        var urlKind = InlineRequestEmission.FindRequestParameter(
                request,
                RequestParameterKind.Url)
            ?.AllowRelativeUrl is true
            ? "RelativeOrAbsolute"
            : "Absolute";
        var requestUriExpression = InlineRequestEmission.HasUrlParameter(request)
            ? $"new global::System.Uri({requestPathExpression}, global::System.UriKind.{urlKind})"
            : InlineRequestEmission.BuildRelativeUriExpression(request, requestPathExpression, settingsLocal, 0);
        var options = interfaceModel.Options;
        var (formFieldsSource, formFieldsFieldName) = FormFieldEmission.BuildFormFieldsField(plan.BodyParameter, uniqueNames, options.SupportsNullable, options.SupportsStaticLambdas);

        // A multipart method builds a MultipartFormDataContent from its parts; every other method has at most one body
        // parameter (a multipart method never carries one), so the two paths never both apply.
        var contentSource = plan.BodyParameter is null
            ? string.Empty
            : BodyContentEmission.BuildInlineContent(plan, formFieldsFieldName, interfaceModel.Options.SupportsNullable, plan.Emission, 0);
        if (request.Body.IsMultipart)
        {
            contentSource = MultipartContentEmission.BuildInlineMultipartContent(request, requestLocal, settingsLocal, plan.Locals, 0);
        }

        var headerSource = InlineRequestEmission.BuildInlineHeaders(request, requestLocal, settingsLocal, 0);

        // A method that declares a positive [Timeout] stashes it on the request so the send helpers apply it; every
        // other method emits nothing here and pays no per-call timeout cost.
        var timeoutSource = request.TimeoutMilliseconds > 0
            ? $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetRequestTimeout({requestLocal}, {request.TimeoutMilliseconds});\n"
            : string.Empty;

        // The method prefix (fields, opening, settings local), then the request construction shared by every shape
        // (prologue locals, the message, content, headers, request properties, and an optional per-call timeout), then
        // the return statement and closing brace. Appended fragment-by-fragment so no intermediate block string forms.
        // Request properties bind no query/converter helpers, so they are appended straight in after the attribute-
        // provider buffer has already been drained above.
        _ = builder
            .Append(plan.ParamInfoBuilder)
            .Append(formFieldsSource)
            .Append(httpMethodFieldSource);
        LiteralEmissionHelpers.AppendMethodOpening(builder, methodModel, isExplicit, isExplicit, interfaceModel.Options.SupportsNullable);
        _ = builder
            .Append(bodyIndent).Append("var ").Append(settingsLocal).Append(" = ").Append(settingsFieldName).AppendLine(";")
            .Append(prologue)
            .Append(bodyIndent).Append("var ").Append(requestLocal)
            .AppendLine(" = new global::System.Net.Http.HttpRequestMessage(")
            .Append(httpRequestMessageIndent).Append(httpMethodExpression).AppendLine(",")
            .Append(httpRequestMessageIndent).Append(requestUriExpression).AppendLine()
            .Append(bodyIndent).AppendLine(");")
            .Append(contentSource)
            .Append(headerSource);
        RequestPropertyEmission.AppendInlineRequestProperties(builder, request, interfaceModel, methodModel, requestLocal, settingsLocal, 0);

        // The optional per-call timeout, then the send-and-deserialize statement (appended straight into the buffer,
        // dispatching on the return shape), then the method's closing brace.
        _ = builder.Append(timeoutSource);
        InlineRequestEmission.AppendInlineReturn(builder, methodModel, request, plan);
        _ = builder.Append(methodIndent).AppendLine("}");
    }

    /// <summary>Appends a cold-observable inline generated Refit method: a per-subscription request factory and its send.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated method source.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="plan">The method-scope locals and pre-built request fragments.</param>
    /// <param name="fragments">The computed request-construction fragments.</param>
    internal static void AppendInlineObservableRefitMethod(
        PooledStringBuilder builder,
        in MethodModel methodModel,
        string settingsFieldName,
        in InlineRequestEmission.InlineMethodPlan plan,
        in InlineMethodFragments fragments)
    {
        var request = methodModel.Request;
        var settingsLocal = plan.SettingsLocal;
        var requestLocal = plan.RequestLocal;
        var bodyIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation);
        var innerBodyIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation + 1);
        var methodIndent = SpanBufferEmission.Indent(Emitter.MethodMemberIndentation);
        var prologue = fragments.RequestPrologueSource;
        var httpMethod = fragments.HttpMethodExpression;
        var uri = fragments.RequestUriExpression;

        var methodPrefix = $"{plan.ParamInfoBuilder}{fragments.FormFieldsSource}{fragments.HttpMethodFieldSource}{fragments.Opening}{bodyIndent}var {settingsLocal} = {settingsFieldName};\n";
        var requestConstruction = $$"""
            {{prologue}}{{innerBodyIndent}}var {{requestLocal}} = new global::System.Net.Http.HttpRequestMessage(
            {{innerBodyIndent}}    {{httpMethod}},
            {{innerBodyIndent}}    {{uri}}
            {{innerBodyIndent}});
            {{fragments.ContentSource}}{{fragments.HeaderSource}}{{fragments.RequestPropertySource}}{{fragments.TimeoutSource}}
            """;
        var buildRequestLocal = plan.Locals.New("BuildRefitRequest");
        var observableReturn = InlineRequestEmission.BuildInlineObservableReturn(request, plan.BufferBodyExpression, plan.CancellationTokenExpression, buildRequestLocal, settingsLocal);
        _ = builder.Append(InlineRequestEmission.BuildInlineObservableMethodSource(methodPrefix, requestConstruction, buildRequestLocal, requestLocal, observableReturn, bodyIndent, methodIndent));
    }
}
