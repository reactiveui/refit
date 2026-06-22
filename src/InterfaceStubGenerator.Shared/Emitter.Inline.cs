// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
internal static partial class Emitter
{
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
            return BuildInlineRefitMethod(methodModel, interfaceModel, isTopLevel, settingsFieldName);
        }

        var (typeParameterFieldSource, cachedTypeParameterFieldName) = BuildTypeParameterField(
            methodModel,
            uniqueNames);
        var returnType = methodModel.ReturnType;
        var (isAsync, @return, configureAwait) = GetReturnInvocationParts(methodModel.ReturnTypeMetadata);
        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        var lookupName = StripExplicitInterfacePrefix(methodModel.Name);
        var typeParameterExpression = BuildTypeParameterExpression(methodModel.Parameters, cachedTypeParameterFieldName);
        var genericTypesArgument = BuildGenericTypesArgument(methodModel);
        var returnStatement = BuildRefitReturnStatement(methodModel, @return, returnType, configureAwait);
        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);

        return typeParameterFieldSource
            + BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable, isAsync)
            + $$"""
                {{bodyIndent}}var refitArguments = {{BuildArgumentsArrayLiteral(methodModel)}};
                {{bodyIndent}}var refitRequestBuilder = {{requestBuilderFieldName}} ?? throw new global::System.InvalidOperationException("This generated Refit method requires a request builder.");
                {{bodyIndent}}var refitFunc = refitRequestBuilder.BuildRestResultFuncForMethod("{{lookupName}}", {{typeParameterExpression}}{{genericTypesArgument}} );

                {{returnStatement}}{{methodIndent}}}

                """;
    }

    /// <summary>Builds a Refit method that constructs the request directly in generated code.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <returns>The generated inline method implementation.</returns>
    private static string BuildInlineRefitMethod(
        MethodModel methodModel,
        InterfaceModel interfaceModel,
        bool isTopLevel,
        string settingsFieldName)
    {
        var isExplicit = methodModel.IsExplicitInterface || !isTopLevel;
        var request = methodModel.Request;
        var bodyParameter = FindRequestParameter(request, RequestParameterKind.Body);
        var cancellationTokenExpression = BuildCancellationTokenExpression(request);
        var bufferBodyExpression = BuildBufferBodyExpression(bodyParameter);
        var requestUriExpression =
            $"new global::System.Uri(refitBasePath + {ToCSharpStringLiteral(request.Path)}, global::System.UriKind.Relative)";
        var contentSource = bodyParameter is null ? string.Empty : BuildInlineContent(bodyParameter);
        var headerSource = BuildInlineHeaders(request);
        var requestPropertySource = BuildInlineRequestProperties(request, interfaceModel);
        var returnSource = BuildInlineReturn(methodModel, request, bufferBodyExpression, cancellationTokenExpression);
        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);

        return $$"""
            {{BuildMethodOpening(methodModel, isExplicit, isExplicit, interfaceModel.SupportsNullable)}}{{bodyIndent}}var refitSettings = {{settingsFieldName}};
            {{bodyIndent}}var refitBasePath = this.Client.BaseAddress?.AbsolutePath ?? throw new global::System.InvalidOperationException("BaseAddress must be set on the HttpClient instance");
            {{bodyIndent}}refitBasePath = refitBasePath == "/" ? string.Empty : refitBasePath.TrimEnd('/');
            {{bodyIndent}}var refitRequest = new global::System.Net.Http.HttpRequestMessage({{ToHttpMethodExpression(request.HttpMethod)}}, {{requestUriExpression}});
            {{bodyIndent}}#if NET6_0_OR_GREATER
            {{bodyIndent}}refitRequest.Version = refitSettings.Version;
            {{bodyIndent}}refitRequest.VersionPolicy = refitSettings.VersionPolicy;
            {{bodyIndent}}#endif
            {{contentSource}}{{headerSource}}{{requestPropertySource}}{{returnSource}}{{methodIndent}}}

            """;
    }

    /// <summary>Builds request content assignment for an inline generated method.</summary>
    /// <param name="bodyParameter">The body parameter model.</param>
    /// <returns>The generated content assignment.</returns>
    private static string BuildInlineContent(RequestParameterModel bodyParameter)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        if (bodyParameter.BodySerializationMethod == "UrlEncoded")
        {
            return $$"""
                {{bodyIndent}}refitRequest.Content = global::Refit.GeneratedRequestRunner.CreateUrlEncodedBodyContent<{{bodyParameter.Type}}>(
                {{bodyIndent}}    refitSettings,
                {{bodyIndent}}    @{{bodyParameter.Name}});

                """;
        }

        if (bodyParameter.BodySerializationMethod == "JsonLines")
        {
            return $$"""
                {{bodyIndent}}refitRequest.Content = global::Refit.GeneratedRequestRunner.CreateJsonLinesBodyContent<{{bodyParameter.Type}}>(
                {{bodyIndent}}    refitSettings,
                {{bodyIndent}}    @{{bodyParameter.Name}});

                """;
        }

        var streamBodyExpression = BuildStreamBodyExpression(bodyParameter);
        var serializationMethodExpression = BuildBodySerializationMethodExpression(bodyParameter);

        return $$"""
            {{bodyIndent}}refitRequest.Content = global::Refit.GeneratedRequestRunner.CreateBodyContent<{{bodyParameter.Type}}>(
            {{bodyIndent}}    refitSettings,
            {{bodyIndent}}    @{{bodyParameter.Name}},
            {{bodyIndent}}    {{serializationMethodExpression}},
            {{bodyIndent}}    {{streamBodyExpression}});

            """;
    }

    /// <summary>Builds the return statement for an inline generated Refit method.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="bufferBodyExpression">The expression indicating whether request content should be buffered.</param>
    /// <param name="cancellationTokenExpression">The cancellation token expression.</param>
    /// <returns>The generated return statement.</returns>
    private static string BuildInlineReturn(
        MethodModel methodModel,
        RequestModel request,
        string bufferBodyExpression,
        string cancellationTokenExpression)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        if (methodModel.ReturnTypeMetadata == ReturnTypeInfo.AsyncVoid)
        {
            return $$"""
                {{bodyIndent}}return global::Refit.GeneratedRequestRunner.SendVoidAsync(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    refitRequest,
                {{bodyIndent}}    refitSettings,
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{cancellationTokenExpression}});

                """;
        }

        return methodModel.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal)
            ? $$"""
                {{bodyIndent}}return new {{methodModel.ReturnType}}(global::Refit.GeneratedRequestRunner.SendAsync<{{request.ResultType}}, {{request.DeserializedResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    refitRequest,
                {{bodyIndent}}    refitSettings,
                {{bodyIndent}}    {{ToLowerInvariantString(request.IsApiResponse)}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.ShouldDisposeResponse)}},
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{cancellationTokenExpression}}));

                """
            : $$"""
                {{bodyIndent}}return global::Refit.GeneratedRequestRunner.SendAsync<{{request.ResultType}}, {{request.DeserializedResultType}}>(
                {{bodyIndent}}    this.Client,
                {{bodyIndent}}    refitRequest,
                {{bodyIndent}}    refitSettings,
                {{bodyIndent}}    {{ToLowerInvariantString(request.IsApiResponse)}},
                {{bodyIndent}}    {{ToLowerInvariantString(request.ShouldDisposeResponse)}},
                {{bodyIndent}}    {{bufferBodyExpression}},
                {{bodyIndent}}    {{cancellationTokenExpression}});

                """;
    }

    /// <summary>Builds static and dynamic header application for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <returns>The generated header statements.</returns>
    private static string BuildInlineHeaders(RequestModel request)
    {
        var parts = new string[request.StaticHeaders.Count + request.Parameters.Count];
        var count = 0;
        var bodyIndent = Indent(MethodBodyIndentation);
        foreach (var header in request.StaticHeaders)
        {
            parts[count++] =
                $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetHeader(refitRequest, {ToCSharpStringLiteral(header.Name)}, {ToNullableCSharpStringLiteral(header.Value)});\n";
        }

        foreach (var parameter in request.Parameters)
        {
            switch (parameter.Kind)
            {
                case RequestParameterKind.Header:
                    {
                        parts[count++] =
                            $"{bodyIndent}global::Refit.GeneratedRequestRunner.SetHeader(refitRequest, {ToCSharpStringLiteral(parameter.HeaderName)}, {BuildHeaderValueExpression(parameter)});\n";
                        continue;
                    }

                case RequestParameterKind.HeaderCollection:
                    {
                        parts[count++] =
                            $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddHeaderCollection(refitRequest, @{parameter.Name});\n";
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
    /// <returns>The generated request option/property statements.</returns>
    private static string BuildInlineRequestProperties(
        RequestModel request,
        InterfaceModel interfaceModel)
    {
        var parts = new string[1 + interfaceModel.Properties.Count + request.Parameters.Count];
        var count = 0;
        var bodyIndent = Indent(MethodBodyIndentation);
        parts[count++] =
            $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(refitRequest, refitSettings, typeof({interfaceModel.InterfaceDisplayName}));\n";

        foreach (var property in interfaceModel.Properties)
        {
            if (property.RequestPropertyKey.Length == 0 || !property.HasGetter)
            {
                continue;
            }

            parts[count++] =
                $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddRequestProperty<{property.Type}>"
                + $"(refitRequest, {ToCSharpStringLiteral(property.RequestPropertyKey)}, "
                + $"{BuildPropertyAccessExpression(property)});\n";
        }

        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Property)
            {
                parts[count++] =
                    $"{bodyIndent}global::Refit.GeneratedRequestRunner.AddRequestProperty<{parameter.Type}>(refitRequest, {ToCSharpStringLiteral(parameter.PropertyKey)}, @{parameter.Name});\n";
            }
        }

        return ConcatParts(parts, count);
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
