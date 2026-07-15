// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Builds the argument, type, and return expressions for the reflection-backed request builder call.</summary>
internal static partial class Emitter
{
    /// <summary>Builds the <c>object[]</c> literal that holds the method's argument values.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <returns>The generated argument array literal.</returns>
    private static string BuildArgumentsArrayLiteral(MethodModel methodModel)
    {
        var parameters = methodModel.Parameters.AsArray();
        if (parameters.Length == 0)
        {
            return "global::System.Array.Empty<object>()";
        }

        const string prefix = "new object[] { ";
        const string suffix = " }";
        var length = prefix.Length + suffix.Length + ((parameters.Length - 1) * ListSeparatorLength);
        for (var i = 0; i < parameters.Length; i++)
        {
            length += 1 + parameters[i].MetadataName.Length;
        }

        return CreateGeneratedString(
            length,
            parameters,
            static (destination, values) =>
            {
                var position = 0;
                AppendText(destination, prefix, ref position);
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        AppendText(destination, ", ", ref position);
                    }

                    destination[position] = '@';
                    position++;
                    AppendText(destination, values[i].MetadataName, ref position);
                }

                AppendText(destination, suffix, ref position);
            });
    }

    /// <summary>Builds the <c>new object[] { ... }</c> literal that captures the method's declared argument values in order.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="supportsNullable">Whether the target language version supports nullable reference type annotations.</param>
    /// <returns>The generated argument-capture array literal, including any cancellation token parameter.</returns>
    private static string BuildMethodArgumentsCaptureLiteral(MethodModel methodModel, bool supportsNullable)
    {
        var parameters = methodModel.Parameters.AsArray();
        var prefix = supportsNullable ? "new object?[] { " : "new object[] { ";
        const string suffix = " }";
        if (parameters.Length == 0)
        {
            return prefix + "}";
        }

        var length = prefix.Length + suffix.Length + ((parameters.Length - 1) * ListSeparatorLength);
        for (var i = 0; i < parameters.Length; i++)
        {
            length += 1 + parameters[i].MetadataName.Length;
        }

        return CreateGeneratedString(
            length,
            (Parameters: parameters, Prefix: prefix),
            static (destination, state) =>
            {
                var position = 0;
                AppendText(destination, state.Prefix, ref position);
                var values = state.Parameters;
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        AppendText(destination, ", ", ref position);
                    }

                    destination[position] = '@';
                    position++;
                    AppendText(destination, values[i].MetadataName, ref position);
                }

                AppendText(destination, suffix, ref position);
            });
    }

    /// <summary>Builds the optional generic <c>Type[]</c> argument for the request builder call.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <returns>The generated generic type argument, or an empty string.</returns>
    private static string BuildGenericTypesArgument(MethodModel methodModel)
    {
        var constraints = methodModel.Constraints.AsArray();
        if (constraints.Length == 0)
        {
            return string.Empty;
        }

        const string prefix = ", new global::System.Type[] { ";
        const string suffix = " }";
        var length = prefix.Length + suffix.Length + ((constraints.Length - 1) * ListSeparatorLength);
        for (var i = 0; i < constraints.Length; i++)
        {
            length += TypeOfPrefix.Length + constraints[i].DeclaredName.Length + 1;
        }

        return CreateGeneratedString(
            length,
            constraints,
            static (destination, values) =>
            {
                var position = 0;
                AppendText(destination, prefix, ref position);
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        AppendText(destination, ", ", ref position);
                    }

                    AppendText(destination, TypeOfPrefix, ref position);
                    AppendText(destination, values[i].DeclaredName, ref position);
                    destination[position] = ')';
                    position++;
                }

                AppendText(destination, suffix, ref position);
            });
    }

    /// <summary>Builds a cached field for non-generic method parameter types, when possible.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <returns>The generated field source and field name, if one was generated.</returns>
    private static (string Source, string? FieldName) BuildTypeParameterField(
        MethodModel methodModel,
        UniqueNameBuilder uniqueNames)
    {
        if (methodModel.Parameters.Count == 0 || ContainsGenericParameter(methodModel.Parameters))
        {
            return (string.Empty, null);
        }

        var typeParameterFieldName = uniqueNames.New(TypeParameterVariableName);
        var typeList = BuildParameterTypeList(methodModel.Parameters);
        var memberIndent = Indent(MethodMemberIndentation);
        var source = $$"""


            {{memberIndent}}/// <summary>Cached parameter type array for the generated {{ToXmlDocumentationText(methodModel.DeclaredMethod)}} method.</summary>
            {{memberIndent}}private static readonly global::System.Type[] {{typeParameterFieldName}} = new global::System.Type[] { {{typeList}} };
            """;
        return (source, typeParameterFieldName);
    }

    /// <summary>Determines whether any parameter type depends on a method type parameter.</summary>
    /// <param name="parameters">The parameter models to inspect.</param>
    /// <returns>True when at least one parameter is generic.</returns>
    private static bool ContainsGenericParameter(ImmutableEquatableArray<ParameterModel> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].IsGeneric)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the expression used to pass the method's parameter types to the request builder.</summary>
    /// <param name="parameters">The parameter models to emit.</param>
    /// <param name="cachedTypeParameterFieldName">The cached field name, if one was generated.</param>
    /// <returns>The generated type parameter expression.</returns>
    private static string BuildTypeParameterExpression(
        ImmutableEquatableArray<ParameterModel> parameters,
        string? cachedTypeParameterFieldName) =>
        parameters.Count == 0
            ? "global::System.Array.Empty<global::System.Type>()"
            : cachedTypeParameterFieldName ?? $"new global::System.Type[] {{ {BuildParameterTypeList(parameters)} }}";

    /// <summary>Builds the generated <c>typeof(...)</c> argument list for method parameters.</summary>
    /// <param name="parameters">The parameter models to emit.</param>
    /// <returns>The generated parameter type list.</returns>
    private static string BuildParameterTypeList(ImmutableEquatableArray<ParameterModel> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        var length = (parameters.Count - 1) * ListSeparatorLength;
        for (var i = 0; i < parameters.Count; i++)
        {
            length += TypeOfPrefix.Length + parameters[i].Type.Length + 1;
        }

        return CreateGeneratedString(
            length,
            parameters.AsArray(),
            static (destination, values) =>
            {
                var position = 0;
                for (var i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        AppendText(destination, ", ", ref position);
                    }

                    AppendText(destination, TypeOfPrefix, ref position);
                    AppendText(destination, values[i].Type, ref position);
                    destination[position] = ')';
                    position++;
                }
            });
    }

    /// <summary>Builds the generated return statement for the reflection-backed Refit method path.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="returnPrefix">The return statement prefix.</param>
    /// <param name="returnType">The generated return type.</param>
    /// <param name="configureAwait">The generated configure-await suffix.</param>
    /// <param name="funcLocal">The generated request-func local name.</param>
    /// <param name="argumentsLocal">The generated arguments-array local name.</param>
    /// <returns>The generated return statement.</returns>
    private static string BuildRefitReturnStatement(
        MethodModel methodModel,
        string returnPrefix,
        string returnType,
        string configureAwait,
        string funcLocal,
        string argumentsLocal)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        return methodModel.ReturnTypeMetadata == ReturnTypeInfo.SyncVoid
            ? $"{bodyIndent}{funcLocal}(this.Client, {argumentsLocal});\n"
            : $"{bodyIndent}{returnPrefix}({returnType}){funcLocal}(this.Client, {argumentsLocal}){configureAwait};\n";
    }
}
