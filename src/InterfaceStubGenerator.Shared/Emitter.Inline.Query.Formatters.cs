// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>Generated enum-formatter and query-converter helper emission for the inline path.</content>
internal static partial class Emitter
{
    /// <summary>Gets the generated enum formatting helper for an enum type and format, emitting it on first use.</summary>
    /// <param name="valueFormat">The enum rendering strategy.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The helper method name.</returns>
    private static string GetOrAddEnumFormatter(
        InlineValueFormatModel valueFormat,
        in InlineValueEmission emission)
    {
        var scope = emission.Scope;
        var key = (valueFormat.TypeName, valueFormat.Format);
        if (scope.Formatters.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var helperName = scope.UniqueNames.New(EnumFormatterBaseName);
        scope.Formatters.Add(key, helperName);
        AppendEnumFormatterSource(valueFormat, helperName, emission.MemberSource);
        return helperName;
    }

    /// <summary>Appends the source of one generated enum formatting helper.</summary>
    /// <param name="valueFormat">The enum rendering strategy.</param>
    /// <param name="helperName">The unique helper method name.</param>
    /// <param name="memberSb">The builder receiving emitted helper members.</param>
    private static void AppendEnumFormatterSource(
        InlineValueFormatModel valueFormat,
        string helperName,
        PooledStringBuilder memberSb)
    {
        var memberIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);
        var caseIndent = bodyIndent + "    ";
        var format = valueFormat.Format;

        _ = memberSb.AppendLine()
            .Append(memberIndent).Append("/// <summary>Formats ").Append(ToXmlDocumentationText(valueFormat.TypeName))
            .AppendLine(" values for generated requests without reflection.</summary>")
            .Append(memberIndent).Append("private static string ").Append(helperName)
            .Append('(').Append(valueFormat.TypeName).AppendLine(" value)")
            .Append(memberIndent).AppendLine("{")
            .Append(bodyIndent).AppendLine("switch (value)")
            .Append(bodyIndent).AppendLine("{");

        foreach (var member in valueFormat.EnumMembers!)
        {
            // With a compile-time format only [EnumMember] overrides beat the formatted numeric rendering,
            // matching string.Format's precedence in the default URL parameter formatter.
            var resolved = format is null
                ? member.EnumMemberValue ?? member.MemberName
                : member.EnumMemberValue;
            if (resolved is null)
            {
                continue;
            }

            _ = memberSb.Append(caseIndent).Append("case ").Append(valueFormat.TypeName).Append(".@").Append(member.MemberName).AppendLine(":")
                .Append(caseIndent).Append("    return ").Append(ToCSharpStringLiteral(resolved)).AppendLine(";");
        }

        var defaultExpression = format is null
            ? "value.ToString()"
            : $"value.ToString({ToCSharpStringLiteral(format)})";
        _ = memberSb.Append(caseIndent).AppendLine("default:")
            .Append(caseIndent).Append("    return ").Append(defaultExpression).AppendLine(";")
            .Append(bodyIndent).AppendLine("}")
            .Append(memberIndent).AppendLine("}");
    }

    /// <summary>Gets the cached converter field for a converter type, emitting the field on first use.</summary>
    /// <param name="converterTypeName">The fully-qualified converter type.</param>
    /// <param name="emission">The shared emission locals and helper state.</param>
    /// <returns>The cached field name.</returns>
    private static string GetOrAddConverterField(string converterTypeName, in InlineValueEmission emission)
    {
        var scope = emission.Scope;
        if (scope.Converters.TryGetValue(converterTypeName, out var existing))
        {
            return existing;
        }

        var fieldName = scope.UniqueNames.New(ConverterFieldBaseName);
        scope.Converters.Add(converterTypeName, fieldName);
        AppendConverterFieldSource(converterTypeName, fieldName, emission.MemberSource);
        return fieldName;
    }

    /// <summary>Appends the source of one cached converter field.</summary>
    /// <param name="converterTypeName">The fully-qualified converter type.</param>
    /// <param name="fieldName">The unique field name.</param>
    /// <param name="memberSb">The builder receiving emitted helper members.</param>
    private static void AppendConverterFieldSource(
        string converterTypeName,
        string fieldName,
        PooledStringBuilder memberSb)
    {
        var memberIndent = Indent(MethodMemberIndentation);
        _ = memberSb.AppendLine()
            .Append(memberIndent).Append("/// <summary>Cached query converter of type ")
            .Append(ToXmlDocumentationText(converterTypeName)).AppendLine(".</summary>")
            .Append(memberIndent).Append("private static readonly ").Append(converterTypeName).Append(' ').Append(fieldName)
            .Append(" = new ").Append(converterTypeName).AppendLine("();");
    }
}
