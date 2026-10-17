// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits the cached HTTP method field and form-field descriptor array for the inline path.</summary>
internal static class FormFieldEmission
{
    /// <summary>Resolves the HTTP method expression, caching a custom verb in a static field.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="uniqueNames">The interface-scope unique name builder.</param>
    /// <returns>The static field source (empty for a known verb) and the method expression to use.</returns>
    /// <remarks>A known verb resolves to a framework-cached <see cref="System.Net.Http.HttpMethod"/> singleton. A custom
    /// verb otherwise constructs a new instance on every call; caching it in a static field matches the reflection
    /// builder, which reads the verb from the attribute once per method.</remarks>
    internal static (string Source, string Expression) BuildHttpMethodField(
        in RequestModel request,
        UniqueNameBuilder uniqueNames)
    {
        var expression = LiteralEmissionHelpers.ToHttpMethodExpression(request.HttpMethod);
        if (!expression.StartsWith("new ", StringComparison.Ordinal))
        {
            return (string.Empty, expression);
        }

        var fieldName = uniqueNames.New("______httpMethod");
        var memberIndent = SpanBufferEmission.Indent(Emitter.MethodMemberIndentation);
        var source = $$"""


            {{memberIndent}}/// <summary>Cached custom HTTP method, allocated once instead of per request.</summary>
            {{memberIndent}}private static readonly global::System.Net.Http.HttpMethod {{fieldName}} = {{expression}};
            """;
        return (source, fieldName);
    }

    /// <summary>Builds the cached form field descriptor array declaration for a URL-encoded body, if eligible.</summary>
    /// <param name="bodyParameter">The body parameter model, or null when the method has no body.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <param name="supportsStaticLambdas">Whether the consumer compilation supports static lambda syntax.</param>
    /// <returns>The generated field declaration and its name, or empty values when the reflection path is used.</returns>
    internal static (string Source, string? FieldName) BuildFormFieldsField(
        RequestParameterModel? bodyParameter,
        UniqueNameBuilder uniqueNames,
        bool supportsNullable,
        bool supportsStaticLambdas)
    {
        // An all-scalar body is serialized straight-line by BuildInlineFormUnroll and needs no descriptor array.
        if (BodyContentEmission.IsUnrollableFormBody(bodyParameter))
        {
            return (string.Empty, null);
        }

        if (bodyParameter?.Body.FormFields is not { Length: > 0 } formFields)
        {
            return (string.Empty, null);
        }

        var fields = formFields;
        var bodyType = bodyParameter.Value.Identity.Type;
        var fieldName = uniqueNames.New(InlineRequestEmission.FormFieldsVariableName);
        var elementIndent = SpanBufferEmission.Indent(Emitter.MethodBodyIndentation);

        // The getter lambda degrades to the consumer's language version: 'static' is C# 9 and the 'object?' cast
        // annotation is C# 8, so both are omitted below those versions to keep generation compilable at the C# 7.3 floor.
        var getterOpen = $">({(supportsStaticLambdas ? "static " : string.Empty)}body => ({(supportsNullable ? "object?" : "object")})body.@";

        var elements = BuildFormFieldElements(fields, bodyType, elementIndent, getterOpen);

        var memberIndent = SpanBufferEmission.Indent(Emitter.MethodMemberIndentation);
        var source = $$"""


            {{memberIndent}}/// <summary>Cached form field descriptors used to serialize the URL-encoded request body without reflection.</summary>
            {{memberIndent}}private static readonly global::Refit.FormField<{{bodyType}}>[] {{fieldName}} = new global::Refit.FormField<{{bodyType}}>[]
            {{memberIndent}}{
            {{elements}}{{memberIndent}}};
            """;
        return (source, fieldName);
    }

    /// <summary>Builds the generated elements of the cached form field descriptor array.</summary>
    /// <param name="fields">The form field descriptors.</param>
    /// <param name="bodyType">The fully-qualified body type.</param>
    /// <param name="elementIndent">The element indentation.</param>
    /// <param name="getterOpen">The language-version-specific getter lambda opening.</param>
    /// <returns>The rendered descriptor array element source.</returns>
    internal static string BuildFormFieldElements(
        FormFieldModel[] fields,
        string bodyType,
        string elementIndent,
        string getterOpen)
    {
        var elementsLength = 0;
        for (var i = 0; i < fields.Length; i++)
        {
            elementsLength += MeasureFormFieldElement(fields[i], bodyType, getterOpen.Length, elementIndent.Length);
        }

        return SpanBufferEmission.CreateGeneratedString(
            elementsLength,
            (fields, bodyType, elementIndent, getterOpen),
            static (destination, state) =>
            {
                var position = 0;
                var (elementFields, type, indent, getter) = state;
                for (var i = 0; i < elementFields.Length; i++)
                {
                    var field = elementFields[i];
                    SpanBufferEmission.AppendText(destination, indent, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.FormFieldNew, ref position);
                    SpanBufferEmission.AppendText(destination, type, ref position);
                    SpanBufferEmission.AppendText(destination, getter, ref position);
                    SpanBufferEmission.AppendText(destination, field.PropertyName, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.FormFieldNameOpen, ref position);
                    SpanBufferEmission.AppendText(destination, field.PropertyName, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.FormFieldNameClose, ref position);
                    SpanBufferEmission.AppendLiteralOrNull(destination, field.ExplicitName, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.ArgumentSeparator, ref position);
                    SpanBufferEmission.AppendLiteralOrNull(destination, field.PrefixSegment, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.ArgumentSeparator, ref position);
                    SpanBufferEmission.AppendLiteralOrNull(destination, field.Format, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.ArgumentSeparator, ref position);
                    if (field.CollectionFormatValue is { } collectionFormatValue)
                    {
                        SpanBufferEmission.AppendText(destination, InlineRequestEmission.CollectionFormatCast, ref position);
                        SpanBufferEmission.AppendInt32(destination, collectionFormatValue, ref position);
                    }
                    else
                    {
                        SpanBufferEmission.AppendText(destination, Emitter.NullLiteral, ref position);
                    }

                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.ArgumentSeparator, ref position);
                    SpanBufferEmission.AppendText(destination, field.SerializeNull ? Emitter.TrueLiteral : Emitter.FalseLiteral, ref position);
                    SpanBufferEmission.AppendText(destination, InlineRequestEmission.FormFieldClose, ref position);
                }
            });
    }

    /// <summary>Measures the rendered length of one generated form field element line.</summary>
    /// <param name="field">The form field descriptor.</param>
    /// <param name="bodyType">The fully-qualified body type.</param>
    /// <param name="getterOpenLength">The length of the language-version-specific getter lambda opening.</param>
    /// <param name="indentLength">The element indentation length.</param>
    /// <returns>The number of characters the rendered element occupies.</returns>
    internal static int MeasureFormFieldElement(FormFieldModel field, string bodyType, int getterOpenLength, int indentLength) =>
        indentLength
        + InlineRequestEmission.FormFieldNew.Length
        + bodyType.Length
        + getterOpenLength
        + field.PropertyName.Length
        + InlineRequestEmission.FormFieldNameOpen.Length
        + field.PropertyName.Length
        + InlineRequestEmission.FormFieldNameClose.Length
        + LiteralEmissionHelpers.LiteralOrNullLength(field.ExplicitName)
        + InlineRequestEmission.ArgumentSeparator.Length
        + LiteralEmissionHelpers.LiteralOrNullLength(field.PrefixSegment)
        + InlineRequestEmission.ArgumentSeparator.Length
        + LiteralEmissionHelpers.LiteralOrNullLength(field.Format)
        + InlineRequestEmission.ArgumentSeparator.Length
        + (field.CollectionFormatValue is { } collectionFormatValue
            ? InlineRequestEmission.CollectionFormatCast.Length + LiteralEmissionHelpers.Int32Length(collectionFormatValue)
            : Emitter.NullLiteral.Length)
        + InlineRequestEmission.ArgumentSeparator.Length
        + (field.SerializeNull ? Emitter.TrueLiteral.Length : Emitter.FalseLiteral.Length)
        + InlineRequestEmission.FormFieldClose.Length;
}
