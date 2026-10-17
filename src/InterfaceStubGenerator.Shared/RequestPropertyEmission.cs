// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Emits the request-property/option application statements for an inline generated method.</content>
internal static class RequestPropertyEmission
{
    /// <summary>The opening of a generated <c>GeneratedRequestRunner.AddRequestProperty</c> call up to the type argument.</summary>
    internal const string RunnerAddRequestProperty = "global::Refit.GeneratedRequestRunner.AddRequestProperty<";

    /// <summary>The request-property key for the reflection-parity method name.</summary>
    internal const string MethodNameOption = "global::Refit.HttpRequestMessageOptions.MethodName";

    /// <summary>The request-property key for the raw relative route template.</summary>
    internal const string RelativePathTemplateOption = "global::Refit.HttpRequestMessageOptions.RelativePathTemplate";

    /// <summary>The request-property key for the captured method arguments array.</summary>
    internal const string MethodArgumentsOption = "global::Refit.HttpRequestMessageOptions.MethodArguments";

    /// <summary>Builds request-option/property application for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="methodAdditionalIndent">The additional indentation for the method body.</param>
    /// <returns>The generated request option/property statements.</returns>
    /// <remarks>The cold-observable shape reuses this block inside a string-interpolated construction block, so it
    /// materializes it here; the standard shape appends it straight into the interface buffer.</remarks>
    internal static string BuildInlineRequestProperties(
        in RequestModel request,
        InterfaceModel interfaceModel,
        in MethodModel methodModel,
        string requestLocal,
        string settingsLocal,
        int methodAdditionalIndent)
    {
        var sb = new PooledStringBuilder();
        AppendInlineRequestProperties(sb, request, interfaceModel, methodModel, requestLocal, settingsLocal, methodAdditionalIndent);
        return sb.ToString();
    }

    /// <summary>Appends request-option/property application for an inline generated method straight into the buffer.</summary>
    /// <param name="sb">The buffer accumulating the interface's generated method source.</param>
    /// <param name="request">The parsed request model.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="methodAdditionalIndent">The additional indentation for the method body.</param>
    internal static void AppendInlineRequestProperties(
        PooledStringBuilder sb,
        in RequestModel request,
        InterfaceModel interfaceModel,
        in MethodModel methodModel,
        string requestLocal,
        string settingsLocal,
        int methodAdditionalIndent)
    {
        var bodyIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation + methodAdditionalIndent);
        _ = sb.Append(bodyIndent)
            .Append("global::Refit.GeneratedRequestRunner.AddConfiguredRequestOptions(")
            .Append(requestLocal).Append(InlineRequestEmission.ArgumentSeparator).Append(settingsLocal)
            .Append(", typeof(").Append(interfaceModel.Names.InterfaceDisplayName).AppendLine("));");

        // The method name (stripped of any explicit-interface prefix, matching reflection's MethodInfo.Name) and the
        // raw route template are compile-time literals a source-gen handler reads without any runtime reflection.
        var methodName = Emitter.ToCSharpStringLiteral(LiteralEmissionHelpers.StripExplicitInterfacePrefix(methodModel.Name));
        AppendRequestProperty(sb, bodyIndent, requestLocal, "string", MethodNameOption, methodName);
        var pathLiteral = Emitter.ToCSharpStringLiteral(request.Path);
        AppendRequestProperty(sb, bodyIndent, requestLocal, "string", RelativePathTemplateOption, pathLiteral);

        // Capture the declared-order argument values only when RefitSettings.CaptureMethodArguments opts in, so the
        // object[] allocation is paid per call solely when a handler needs them. The annotation is gated on the target
        // language version so the C# 7.3 baseline still compiles.
        var argumentsArrayType = interfaceModel.Options.SupportsNullable ? "object?[]" : "object[]";
        _ = sb.Append(bodyIndent).Append("if (").Append(settingsLocal)
            .Append(".CaptureMethodArguments) { ").Append(RunnerAddRequestProperty)
            .Append(argumentsArrayType).Append(">(").Append(requestLocal).Append(", ").Append(MethodArgumentsOption)
            .Append(InlineRequestEmission.ArgumentSeparator)
            .Append(ReflectionArgumentEmission.BuildMethodArgumentsCaptureLiteral(methodModel, interfaceModel.Options.SupportsNullable))
            .AppendLine("); }");

        foreach (var property in interfaceModel.Members.Properties)
        {
            if (property.RequestPropertyKey.Length == 0 || !property.HasGetter)
            {
                continue;
            }

            var key = Emitter.ToCSharpStringLiteral(property.RequestPropertyKey);
            AppendRequestProperty(sb, bodyIndent, requestLocal, property.Type, key, LiteralEmissionHelpers.BuildPropertyAccessExpression(property));
        }

        foreach (var parameter in request.Bindings.Parameters)
        {
            if (parameter.Kind != RequestParameterKind.Property)
            {
                continue;
            }

            var key = Emitter.ToCSharpStringLiteral(parameter.PropertyKey);
            AppendRequestProperty(sb, bodyIndent, requestLocal, parameter.Identity.Type, key, $"@{parameter.Identity.Name}");
        }

        // A JsonTypeInfo<T> parameter that describes the reply rides on the request so the runner reads the reply with it.
        foreach (var parameter in request.Bindings.Parameters)
        {
            if (parameter.Kind != RequestParameterKind.JsonTypeInfo || parameter.JsonTypeInfoTarget != request.Return.DeserializedResultType)
            {
                continue;
            }

            _ = sb.Append(bodyIndent).Append("global::Refit.GeneratedRequestRunner.SetRequestJsonTypeInfo<")
                .Append(parameter.JsonTypeInfoTarget).Append(">(").Append(requestLocal).Append(InlineRequestEmission.ArgumentSeparator)
                .Append('@').Append(parameter.Identity.Name).AppendLine(");");
        }
    }

    /// <summary>Appends one <c>AddRequestProperty&lt;T&gt;</c> statement directly into the request-property buffer.</summary>
    /// <param name="sb">The pooled statement buffer.</param>
    /// <param name="bodyIndent">The method-body indentation.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="typeArgument">The request-property value type argument.</param>
    /// <param name="keyExpression">The property-key expression.</param>
    /// <param name="valueExpression">The property-value expression.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void AppendRequestProperty(
        PooledStringBuilder sb,
        string bodyIndent,
        string requestLocal,
        string typeArgument,
        string keyExpression,
        string valueExpression) =>
        sb.Append(bodyIndent).Append(RunnerAddRequestProperty).Append(typeArgument)
            .Append(">(").Append(requestLocal).Append(InlineRequestEmission.ArgumentSeparator)
            .Append(keyExpression).Append(InlineRequestEmission.ArgumentSeparator).Append(valueExpression)
            .AppendLine(");");
}
