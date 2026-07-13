// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Internal emitter helpers that are directly covered by focused tests.</summary>
internal static partial class Emitter
{
    /// <summary>The text length added to a nullable generated parameter around its type and name.</summary>
    private const int NullableParameterExtraLength = 3;

    /// <summary>The text length added to a non-nullable generated parameter around its type and name.</summary>
    private const int ParameterExtraLength = 2;

    /// <summary>The statement prefix that returns a generated expression to the caller.</summary>
    private const string ReturnStatementPrefix = "return ";

    /// <summary>Builds the request-body buffering expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter, if any.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <returns>The buffering expression.</returns>
    internal static string BuildBufferBodyExpression(RequestParameterModel? bodyParameter, string settingsLocal) =>
        bodyParameter is null
            ? FalseLiteral
            : bodyParameter.BodyBufferMode switch
            {
                BodyBufferMode.Settings => $"{settingsLocal}.Buffered",
                BodyBufferMode.Buffered => TrueLiteral,
                _ => FalseLiteral
            };

    /// <summary>Builds the serialized-body streaming expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <returns>The streaming expression.</returns>
    internal static string BuildStreamBodyExpression(RequestParameterModel bodyParameter, string settingsLocal) =>
        bodyParameter.BodySerializationMethod == "UrlEncoded"
            ? FalseLiteral
            : bodyParameter.BodyBufferMode switch
            {
                BodyBufferMode.Settings => $"!{settingsLocal}.Buffered",
                BodyBufferMode.Buffered => FalseLiteral,
                BodyBufferMode.Streaming => TrueLiteral,
                _ => FalseLiteral
            };

    /// <summary>Builds the expression used to read an implemented interface property.</summary>
    /// <param name="property">The property model.</param>
    /// <returns>The generated property access expression.</returns>
    internal static string BuildPropertyAccessExpression(InterfacePropertyModel property)
    {
        if (property.IsSatisfiedByGeneratedMember)
        {
            return "this.Client";
        }

        return property.IsExplicitInterface
            ? $"(({EnsureGlobalPrefix(property.ContainingType)})this).{property.Name}"
            : $"this.{property.Name}";
    }

    /// <summary>Ensures a type display name carries an alias qualifier, adding <c>global::</c> when it has none.</summary>
    /// <param name="typeName">The type display name.</param>
    /// <returns>The alias-qualified type display name.</returns>
    /// <remarks>A name that already carries an alias qualifier (either <c>global::</c> or an extern alias) contains
    /// the <c>::</c> separator, whereas a bare name never does, so only bare names receive the <c>global::</c> prefix.</remarks>
    internal static string EnsureGlobalPrefix(string typeName) =>
        typeName.IndexOf("::", StringComparison.Ordinal) >= 0
            ? typeName
            : GlobalPrefix + typeName;

    /// <summary>Maps a parsed HTTP method name to an expression that creates or returns an <see cref="HttpMethod"/>.</summary>
    /// <param name="httpMethod">The HTTP method text.</param>
    /// <returns>The HTTP method expression.</returns>
    /// <remarks>Known verbs reuse the cached <see cref="HttpMethod"/> singletons; a custom verb (read from a derived
    /// attribute's <c>new HttpMethod("VERB")</c> getter) constructs one, matching what the reflection builder does.</remarks>
    internal static string ToHttpMethodExpression(string httpMethod) =>
        httpMethod switch
        {
            "DELETE" => "global::System.Net.Http.HttpMethod.Delete",
            "GET" => "global::System.Net.Http.HttpMethod.Get",
            "HEAD" => "global::System.Net.Http.HttpMethod.Head",
            "OPTIONS" => "global::System.Net.Http.HttpMethod.Options",
            "POST" => "global::System.Net.Http.HttpMethod.Post",
            "PUT" => "global::System.Net.Http.HttpMethod.Put",
            _ => $"new global::System.Net.Http.HttpMethod({ToCSharpStringLiteral(httpMethod)})"
        };

    /// <summary>Gets the invocation text used for a generated method return type.</summary>
    /// <param name="returnTypeInfo">The method return type shape.</param>
    /// <returns>The async flag, return prefix, and configure-await suffix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="returnTypeInfo"/> is unsupported.</exception>
    internal static (bool IsAsync, string ReturnPrefix, string ConfigureAwaitSuffix) GetReturnInvocationParts(
        ReturnTypeInfo returnTypeInfo) =>
        returnTypeInfo switch
        {
            ReturnTypeInfo.AsyncVoid => (true, "await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.AsyncResult => (true, "return await (", ").ConfigureAwait(false)"),
            ReturnTypeInfo.AsyncEnumerable => (false, ReturnStatementPrefix, string.Empty),
            ReturnTypeInfo.Observable => (false, ReturnStatementPrefix, string.Empty),
            ReturnTypeInfo.Return => (false, ReturnStatementPrefix, string.Empty),
            ReturnTypeInfo.SyncVoid => (false, string.Empty, string.Empty),
            _ => throw new ArgumentOutOfRangeException(
                nameof(returnTypeInfo),
                returnTypeInfo,
                "Unsupported value.")
        };

    /// <summary>Converts a nullable string into a C# string literal or null literal.</summary>
    /// <param name="value">The value to quote.</param>
    /// <returns>The generated expression.</returns>
    internal static string ToNullableCSharpStringLiteral(string? value) =>
        value is null ? "null" : ToCSharpStringLiteral(value);

    /// <summary>Appends one escaped C# string-literal character.</summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="character">The character to append.</param>
    [SuppressMessage(
        "CodeQuality",
        "S1541:Methods and properties should not be too complex",
        Justification = "A compact switch avoids a dictionary or repeated helper calls on the generator hot path.")]
    internal static void AppendEscapedCharacter(PooledStringBuilder builder, char character) =>
        _ = character switch
        {
            '\\' => builder.Append(@"\\"),
            '"' => builder.Append("\\\""),
            '\0' => builder.Append(@"\0"),
            '\a' => builder.Append(@"\a"),
            '\b' => builder.Append(@"\b"),
            '\f' => builder.Append(@"\f"),
            '\n' => builder.Append(@"\n"),
            '\r' => builder.Append(@"\r"),
            '\t' => builder.Append(@"\t"),
            '\v' => builder.Append(@"\v"),

            // Line terminators that would break out of a regular C# string literal (CS1010).
            '\u0085' => builder.Append(@"\u0085"),
            '\u2028' => builder.Append(@"\u2028"),
            '\u2029' => builder.Append(@"\u2029"),
            _ => builder.Append(character)
        };

    /// <summary>Gets the escape sequence for a C# string-literal character, or null when none is needed.</summary>
    /// <param name="character">The character to escape.</param>
    /// <returns>The escape sequence, or <see langword="null"/> when the character is emitted verbatim.</returns>
    internal static string? EscapeSequence(char character) =>
        character switch
        {
            '\\' => @"\\",
            '"' => "\\\"",
            '\0' => @"\0",
            '\a' => @"\a",
            '\b' => @"\b",
            '\f' => @"\f",
            '\n' => @"\n",
            '\r' => @"\r",
            '\t' => @"\t",
            '\v' => @"\v",

            // Characters C# treats as line terminators inside a regular string literal (CS1010).
            '\u0085' => @"\u0085",
            '\u2028' => @"\u2028",
            '\u2029' => @"\u2029",
            _ => null
        };

    /// <summary>Computes the rendered length of a C# string literal or the <c>null</c> keyword.</summary>
    /// <param name="value">The value to quote, or <see langword="null"/>.</param>
    /// <returns>The number of characters the rendered expression occupies.</returns>
    internal static int LiteralOrNullLength(string? value)
    {
        if (value is null)
        {
            return NullLiteral.Length;
        }

        var length = StringLiteralQuoteLength;
        foreach (var character in value)
        {
            length += EscapeSequence(character) is { Length: var escapeLength } ? escapeLength : 1;
        }

        return length;
    }

    /// <summary>Computes the rendered decimal length of a non-negative 32-bit integer.</summary>
    /// <param name="value">The non-negative value to render (callers only pass <c>CollectionFormat</c> values).</param>
    /// <returns>The number of characters the decimal rendering occupies.</returns>
    internal static int Int32Length(int value)
    {
        var length = 0;
        do
        {
            length++;
            value /= DecimalRadix;
        }
        while (value > 0);

        return length;
    }

    /// <summary>Strips an explicit interface prefix from a method name (e.g. <c>IFoo.Bar</c> becomes <c>Bar</c>).</summary>
    /// <param name="name">The method name to normalize.</param>
    /// <returns>The method name without any explicit interface prefix.</returns>
    internal static string StripExplicitInterfacePrefix(string name)
    {
        var lastDotIndex = name.LastIndexOf('.');
        return lastDotIndex >= 0 && lastDotIndex < name.Length - 1
            ? name[(lastDotIndex + 1)..]
            : name;
    }

    /// <summary>Builds the method signature, constraints, and opening brace.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isDerivedExplicitImpl">True if the method is a derived explicit implementation.</param>
    /// <param name="isExplicitInterface">True if the method is an explicit interface implementation.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <param name="isAsync">True if the method should be emitted as async.</param>
    /// <param name="methodAttributes">Attribute lines emitted between the documentation and the signature.</param>
    /// <returns>The generated method opening.</returns>
    internal static string BuildMethodOpening(
        MethodModel methodModel,
        bool isDerivedExplicitImpl,
        bool isExplicitInterface,
        bool supportsNullable,
        bool isAsync = false,
        string methodAttributes = "")
    {
        var visibility = !isExplicitInterface ? "public " : string.Empty;
        var asyncKeyword = isAsync ? "async " : string.Empty;
        var explicitInterface = BuildExplicitInterfacePrefix(methodModel, isExplicitInterface);
        var parameters = BuildParameterList(methodModel.Parameters, supportsNullable);
        var methodIndent = Indent(MethodMemberIndentation);
        var constraints = BuildConstraints(methodModel.Constraints, isDerivedExplicitImpl || isExplicitInterface, MethodBodyIndentation);

        return $$"""

            {{methodIndent}}/// <inheritdoc />
            {{methodAttributes}}{{methodIndent}}{{visibility}}{{asyncKeyword}}{{methodModel.ReturnType}} {{explicitInterface}}{{methodModel.DeclaredMethod}}({{parameters}})

            """
            + constraints
            + methodIndent
            + "{\n";
    }

    /// <summary>Builds the trim/AOT annotations emitted onto methods that use the reflection request builder.</summary>
    /// <param name="requiresUnreferencedCode">Whether the interface method declares <c>[RequiresUnreferencedCode]</c>.</param>
    /// <param name="requiresDynamicCode">Whether the interface method declares <c>[RequiresDynamicCode]</c>.</param>
    /// <remarks>
    /// The reflection fallback intentionally trades trim safety for coverage of method shapes the inline emitter does
    /// not support; RF006/RF007 report those shapes at compile time. When the interface member is unannotated the
    /// generated call site suppresses the per-interface IL2026/IL3050 noise consumers cannot act on
    /// (see reactiveui/refit#2200). When the interface member declares the matching <c>[RequiresUnreferencedCode]</c> or
    /// <c>[RequiresDynamicCode]</c>, the implementation mirrors it instead: that both honours the caller-visible contract
    /// and satisfies the trim/AOT annotation-matching rule (IL2046/IL3051) between the interface and its implementation.
    /// </remarks>
    /// <returns>The generated attribute lines, terminated by a newline.</returns>
    internal static string BuildReflectionFallbackSuppressions(
        bool requiresUnreferencedCode,
        bool requiresDynamicCode)
    {
        const string justification =
            "\"This method's shape is not supported by generated request building and intentionally uses the "
            + "reflection request builder; trimmed and Native AOT applications must use inline-eligible method "
            + "shapes instead (Refit reports this at compile time).\"";
        const string requiresMessage =
            "\"This generated Refit method's shape is not supported by generated request building and uses the "
            + "reflection request builder, which requires unreferenced code and runtime code generation.\"";
        var methodIndent = Indent(MethodMemberIndentation);

        // A [RequiresDynamicCode] attribute only exists on net7.0+, but it is only emitted when the interface member
        // declares it, which itself can only compile on net7.0+ — so it is always inside the net5.0 guard safely.
        var unreferencedCodeAttribute = requiresUnreferencedCode
            ? $"[global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode({requiresMessage})]"
            : $"[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\", Justification = {justification})]";
        var dynamicCodeAttribute = requiresDynamicCode
            ? $"[global::System.Diagnostics.CodeAnalysis.RequiresDynamicCode({requiresMessage})]"
            : $"[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"AOT\", \"IL3050\", Justification = {justification})]";

        return $$"""
            {{methodIndent}}#if NET5_0_OR_GREATER
            {{methodIndent}}{{unreferencedCodeAttribute}}
            {{methodIndent}}{{dynamicCodeAttribute}}
            {{methodIndent}}#endif

            """;
    }

    /// <summary>Builds the explicit interface qualifier for a method signature.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isExplicitInterface">Whether the method is emitted explicitly.</param>
    /// <returns>The explicit interface prefix, or an empty string.</returns>
    private static string BuildExplicitInterfacePrefix(MethodModel methodModel, bool isExplicitInterface)
    {
        if (!isExplicitInterface)
        {
            return string.Empty;
        }

        var containingType = methodModel.ContainingType;
        return containingType.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? containingType + "."
            : GlobalPrefix + containingType + ".";
    }

    /// <summary>Builds the generated method parameter list.</summary>
    /// <param name="parameters">The parameter models.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <returns>The generated method parameter list.</returns>
    private static string BuildParameterList(
        ImmutableEquatableArray<ParameterModel> parameters,
        bool supportsNullable)
    {
        var parameterModels = parameters.AsArray();
        if (parameterModels.Length == 0)
        {
            return string.Empty;
        }

        var length = (parameterModels.Length - 1) * ListSeparatorLength;
        for (var i = 0; i < parameterModels.Length; i++)
        {
            var (metadataName, type, annotation, _) = parameterModels[i];
            length += type.Length
                + (supportsNullable && annotation ? NullableParameterExtraLength : ParameterExtraLength)
                + metadataName.Length;
        }

        return CreateGeneratedString(
            length,
            (parameterModels, supportsNullable),
            static (destination, values) =>
            {
                var position = 0;
                var (models, emitNullableAnnotations) = values;
                for (var i = 0; i < models.Length; i++)
                {
                    if (i > 0)
                    {
                        AppendText(destination, ", ", ref position);
                    }

                    var (metadataName, type, annotation, _) = models[i];
                    AppendText(destination, type, ref position);
                    if (emitNullableAnnotations && annotation)
                    {
                        destination[position] = '?';
                        position++;
                    }

                    AppendText(destination, " @", ref position);
                    AppendText(destination, metadataName, ref position);
                }
            });
    }
}
