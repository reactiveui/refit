// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Assembles an inline generated Refit method body from its computed request-construction fragments.</summary>
internal static partial class Emitter
{
    /// <summary>Computes the per-method source fragments shared by every inline generated Refit method shape.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isExplicit">Whether the method is emitted as an explicit interface implementation.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="plan">The method-scope locals and pre-built request fragments.</param>
    /// <returns>The computed request-construction fragments.</returns>
    internal static InlineMethodFragments BuildInlineMethodFragments(
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        UniqueNameBuilder uniqueNames,
        in InlineMethodPlan plan)
    {
        var request = methodModel.Request;
        var settingsLocal = plan.SettingsLocal;
        var requestLocal = plan.RequestLocal;
        var emission = plan.Emission;
        var bodyIndent = Indent(MethodBodyIndentation);

        var requestPrologueSource = BuildInlineRequestPrologue(request, plan, bodyIndent, out var requestPathExpression);
        var (httpMethodFieldSource, httpMethodExpression) = BuildHttpMethodField(request, uniqueNames);

        // A [Url] method dispatches to an absolute URI (the validated [Url] value with any query appended), bypassing
        // the base-address merge; every other method builds a relative URI merged onto the base address.
        var requestUriExpression = HasUrlParameter(request)
            ? $"new global::System.Uri({requestPathExpression}, global::System.UriKind.Absolute)"
            : BuildRelativeUriExpression(request, requestPathExpression, settingsLocal);
        var (formFieldsSource, formFieldsFieldName) = BuildFormFieldsField(
            plan.BodyParameter,
            uniqueNames,
            interfaceModel.SupportsNullable,
            interfaceModel.SupportsStaticLambdas);

        // A multipart method builds a MultipartFormDataContent from its parts; every other method has at most one body
        // parameter (a multipart method never carries one), so the two paths never both apply.
        var contentSource = plan.BodyParameter is null
            ? string.Empty
            : BuildInlineContent(plan.BodyParameter, requestLocal, settingsLocal, formFieldsFieldName, interfaceModel.SupportsNullable, emission, plan.Locals);
        if (request.IsMultipart)
        {
            contentSource = BuildInlineMultipartContent(request, requestLocal, settingsLocal, plan.Locals);
        }

        var headerSource = BuildInlineHeaders(request, requestLocal, settingsLocal);
        var requestPropertySource = BuildInlineRequestProperties(request, interfaceModel, methodModel, requestLocal, settingsLocal);

        // A method that declares a positive [Timeout] stashes it on the request so the send helpers apply it; every
        // other method emits nothing here and pays no per-call timeout cost.
        var timeoutSource = request.TimeoutMilliseconds > 0
            ? $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetRequestTimeout({requestLocal}, {request.TimeoutMilliseconds});\n"
            : string.Empty;
        var opening = BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable);

        return new InlineMethodFragments(
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
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isExplicit,
        string settingsFieldName,
        UniqueNameBuilder uniqueNames,
        in InlineMethodPlan plan)
    {
        var request = methodModel.Request;
        var settingsLocal = plan.SettingsLocal;
        var requestLocal = plan.RequestLocal;
        var bodyIndent = Indent(MethodBodyIndentation);
        var methodIndent = Indent(MethodMemberIndentation);

        var prologue = new PooledStringBuilder();
        var requestPathExpression = AppendInlineRequestPrologue(prologue, request, plan, bodyIndent);
        var (httpMethodFieldSource, httpMethodExpression) = BuildHttpMethodField(request, uniqueNames);

        // A [Url] method dispatches to an absolute URI (the validated [Url] value with any query appended), bypassing
        // the base-address merge; every other method builds a relative URI merged onto the base address.
        var requestUriExpression = HasUrlParameter(request)
            ? $"new global::System.Uri({requestPathExpression}, global::System.UriKind.Absolute)"
            : BuildRelativeUriExpression(request, requestPathExpression, settingsLocal);
        var (formFieldsSource, formFieldsFieldName) = BuildFormFieldsField(
            plan.BodyParameter,
            uniqueNames,
            interfaceModel.SupportsNullable,
            interfaceModel.SupportsStaticLambdas);

        // A multipart method builds a MultipartFormDataContent from its parts; every other method has at most one body
        // parameter (a multipart method never carries one), so the two paths never both apply.
        var contentSource = plan.BodyParameter is null
            ? string.Empty
            : BuildInlineContent(plan.BodyParameter, requestLocal, settingsLocal, formFieldsFieldName, interfaceModel.SupportsNullable, plan.Emission, plan.Locals);
        if (request.IsMultipart)
        {
            contentSource = BuildInlineMultipartContent(request, requestLocal, settingsLocal, plan.Locals);
        }

        var headerSource = BuildInlineHeaders(request, requestLocal, settingsLocal);

        // A method that declares a positive [Timeout] stashes it on the request so the send helpers apply it; every
        // other method emits nothing here and pays no per-call timeout cost.
        var timeoutSource = request.TimeoutMilliseconds > 0
            ? $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetRequestTimeout({requestLocal}, {request.TimeoutMilliseconds});\n"
            : string.Empty;
        var opening = BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable);

        // The method prefix (fields, opening, settings local), then the request construction shared by every shape
        // (prologue locals, the message, content, headers, request properties, and an optional per-call timeout), then
        // the return statement and closing brace. Appended fragment-by-fragment so no intermediate block string forms.
        // Request properties bind no query/converter helpers, so they are appended straight in after the attribute-
        // provider buffer has already been drained above.
        _ = builder
            .Append(plan.ParamInfoBuilder)
            .Append(formFieldsSource)
            .Append(httpMethodFieldSource)
            .Append(opening)
            .Append(bodyIndent).Append("var ").Append(settingsLocal).Append(" = ").Append(settingsFieldName).AppendLine(";")
            .Append(prologue)
            .Append(bodyIndent).Append("var ").Append(requestLocal)
            .Append(" = new global::System.Net.Http.HttpRequestMessage(").Append(httpMethodExpression)
            .Append(", ").Append(requestUriExpression).AppendLine(");")
            .Append(contentSource)
            .Append(headerSource);
        AppendInlineRequestProperties(builder, request, interfaceModel, methodModel, requestLocal, settingsLocal);

        // The optional per-call timeout, then the send-and-deserialize statement (appended straight into the buffer,
        // dispatching on the return shape), then the method's closing brace.
        _ = builder.Append(timeoutSource);
        AppendInlineReturn(builder, methodModel, request, plan);
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
        MethodModel methodModel,
        string settingsFieldName,
        in InlineMethodPlan plan,
        in InlineMethodFragments fragments)
    {
        var request = methodModel.Request;
        var settingsLocal = plan.SettingsLocal;
        var requestLocal = plan.RequestLocal;
        var bodyIndent = Indent(MethodBodyIndentation);
        var methodIndent = Indent(MethodMemberIndentation);
        var prologue = fragments.RequestPrologueSource;
        var httpMethod = fragments.HttpMethodExpression;
        var uri = fragments.RequestUriExpression;

        var methodPrefix = $"{plan.ParamInfoBuilder}{fragments.FormFieldsSource}{fragments.HttpMethodFieldSource}{fragments.Opening}{bodyIndent}var {settingsLocal} = {settingsFieldName};\n";
        var requestConstruction = $$"""
            {{prologue}}{{bodyIndent}}var {{requestLocal}} = new global::System.Net.Http.HttpRequestMessage({{httpMethod}}, {{uri}});
            {{fragments.ContentSource}}{{fragments.HeaderSource}}{{fragments.RequestPropertySource}}{{fragments.TimeoutSource}}
            """;
        var buildRequestLocal = plan.Locals.New("BuildRefitRequest");
        var observableReturn = BuildInlineObservableReturn(request, plan.BufferBodyExpression, plan.CancellationTokenExpression, buildRequestLocal, settingsLocal);
        _ = builder.Append(BuildInlineObservableMethodSource(methodPrefix, requestConstruction, buildRequestLocal, requestLocal, observableReturn, bodyIndent, methodIndent));
    }
}
