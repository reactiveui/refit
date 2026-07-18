// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Emits the request-property/option application statements for an inline generated method.</content>
internal static partial class Emitter
{
    /// <summary>The opening of a generated <c>GeneratedRequestRunner.AddRequestProperty</c> call up to the type argument.</summary>
    private const string RunnerAddRequestProperty = "global::Refit.GeneratedRequestRunner.AddRequestProperty<";

    /// <summary>The request-property key for the reflection-parity method name.</summary>
    private const string MethodNameOption = "global::Refit.HttpRequestMessageOptions.MethodName";

    /// <summary>The request-property key for the raw relative route template.</summary>
    private const string RelativePathTemplateOption = "global::Refit.HttpRequestMessageOptions.RelativePathTemplate";

    /// <summary>The request-property key for the captured method arguments array.</summary>
    private const string MethodArgumentsOption = "global::Refit.HttpRequestMessageOptions.MethodArguments";

    /// <summary>Builds request-option/property application for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <returns>The generated request option/property statements.</returns>
    internal static string BuildInlineRequestProperties(
        RequestModel request,
        InterfaceModel interfaceModel,
        MethodModel methodModel,
        string requestLocal,
        string settingsLocal)
    {
        // Append every statement into one pooled buffer. The previous string[] + ConcatParts shape allocated a
        // throwaway interpolated string per statement; the emitted text is identical, appended with no intermediates.
        var bodyIndent = Indent(MethodBodyIndentation);
        var sb = new PooledStringBuilder();
        _ = sb.Append(bodyIndent)
            .Append("global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(")
            .Append(requestLocal).Append(ArgumentSeparator).Append(settingsLocal)
            .Append(", typeof(").Append(interfaceModel.InterfaceDisplayName).Append("));\n");

        // The method name (stripped of any explicit-interface prefix, matching reflection's MethodInfo.Name) and the
        // raw route template are compile-time literals a source-gen handler reads without any runtime reflection.
        var methodName = ToCSharpStringLiteral(StripExplicitInterfacePrefix(methodModel.Name));
        AppendRequestProperty(sb, bodyIndent, requestLocal, "string", MethodNameOption, methodName);
        var pathLiteral = ToCSharpStringLiteral(request.Path);
        AppendRequestProperty(sb, bodyIndent, requestLocal, "string", RelativePathTemplateOption, pathLiteral);

        // Capture the declared-order argument values only when RefitSettings.CaptureMethodArguments opts in, so the
        // object[] allocation is paid per call solely when a handler needs them. The annotation is gated on the target
        // language version so the C# 7.3 baseline still compiles.
        var argumentsArrayType = interfaceModel.SupportsNullable ? "object?[]" : "object[]";
        _ = sb.Append(bodyIndent).Append("if (").Append(settingsLocal)
            .Append(".CaptureMethodArguments) { ").Append(RunnerAddRequestProperty)
            .Append(argumentsArrayType).Append(">(").Append(requestLocal).Append(", ").Append(MethodArgumentsOption)
            .Append(ArgumentSeparator)
            .Append(BuildMethodArgumentsCaptureLiteral(methodModel, interfaceModel.SupportsNullable))
            .Append("); }\n");

        foreach (var property in interfaceModel.Properties)
        {
            if (property.RequestPropertyKey.Length != 0 && property.HasGetter)
            {
                var key = ToCSharpStringLiteral(property.RequestPropertyKey);
                AppendRequestProperty(sb, bodyIndent, requestLocal, property.Type, key, BuildPropertyAccessExpression(property));
            }
        }

        foreach (var parameter in request.Parameters)
        {
            if (parameter.Kind == RequestParameterKind.Property)
            {
                var key = ToCSharpStringLiteral(parameter.PropertyKey);
                AppendRequestProperty(sb, bodyIndent, requestLocal, parameter.Type, key, "@" + parameter.Name);
            }
        }

        return sb.ToString();
    }

    /// <summary>Appends one <c>AddRequestProperty&lt;T&gt;</c> statement directly into the request-property buffer.</summary>
    /// <param name="sb">The pooled statement buffer.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="typeArgument">The request-property value type argument.</param>
    /// <param name="keyExpression">The property-key expression.</param>
    /// <param name="valueExpression">The property-value expression.</param>
    internal static void AppendRequestProperty(
        PooledStringBuilder sb,
        string bodyIndent,
        string requestLocal,
        string typeArgument,
        string keyExpression,
        string valueExpression) =>
        sb.Append(bodyIndent).Append(RunnerAddRequestProperty).Append(typeArgument)
            .Append(">(").Append(requestLocal).Append(ArgumentSeparator)
            .Append(keyExpression).Append(ArgumentSeparator).Append(valueExpression)
            .Append(");\n");
}
