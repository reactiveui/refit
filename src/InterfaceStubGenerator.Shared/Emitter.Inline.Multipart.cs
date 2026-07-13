// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits inline request-construction source for generated Refit method implementations.</summary>
/// <content>
/// Multipart form content emission for the inline path. Builds a <c>MultipartFormDataContent</c> and adds one part per
/// part parameter (or one per element for a reference-typed enumerable), reproducing the reflection request builder's
/// <c>AddMultipartItem</c> dispatch and its part name/file-name selection exactly for statically-resolvable part types.
/// </content>
internal static partial class Emitter
{
    /// <summary>The fully-qualified <c>MultipartFormDataContent</c> constructor prefix.</summary>
    private const string MultipartContentNew = "new global::System.Net.Http.MultipartFormDataContent(";

    /// <summary>The fully-qualified <c>StreamContent</c> constructor prefix.</summary>
    private const string StreamContentNew = "new global::System.Net.Http.StreamContent(";

    /// <summary>The fully-qualified <c>StringContent</c> constructor prefix.</summary>
    private const string StringContentNew = "new global::System.Net.Http.StringContent(";

    /// <summary>The fully-qualified <c>ByteArrayContent</c> constructor prefix.</summary>
    private const string ByteArrayContentNew = "new global::System.Net.Http.ByteArrayContent(";

    /// <summary>Builds the multipart form content assignment for an inline generated method.</summary>
    /// <param name="request">The parsed request model.</param>
    /// <param name="requestLocal">The generated request message local name.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="locals">The method-scope unique local name builder.</param>
    /// <returns>The generated multipart content statements, ending with the request-content assignment.</returns>
    private static string BuildInlineMultipartContent(
        RequestModel request,
        string requestLocal,
        string settingsLocal,
        UniqueNameBuilder locals)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var contentLocal = locals.New("refitMultipart");

        var sb = new PooledStringBuilder();
        _ = sb.Append(bodyIndent).Append("var ").Append(contentLocal).Append(" = ").Append(MultipartContentNew)
            .Append(ToCSharpStringLiteral(request.MultipartBoundary)).AppendLine(");");

        foreach (var parameter in request.Parameters)
        {
            if (parameter is { Kind: RequestParameterKind.MultipartPart, MultipartPart: { } part })
            {
                AppendMultipartPart(sb, parameter, part, settingsLocal, contentLocal, locals);
            }
        }

        // A blank line separates the content assignment from the following header statements, matching the body path.
        _ = sb.Append(bodyIndent).Append(requestLocal).Append(".Content = ").Append(contentLocal).AppendLine(";")
            .AppendLine();
        return sb.ToString();
    }

    /// <summary>Appends the statements adding one part parameter (guarding null values as the reflection builder does).</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="parameter">The part parameter model.</param>
    /// <param name="part">The multipart part descriptor.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="contentLocal">The generated multipart content local name.</param>
    /// <param name="locals">The method-scope unique local name builder.</param>
    private static void AppendMultipartPart(
        PooledStringBuilder sb,
        RequestParameterModel parameter,
        MultipartPartModel part,
        string settingsLocal,
        string contentLocal,
        UniqueNameBuilder locals)
    {
        var bodyIndent = Indent(MethodBodyIndentation);
        var valueExpression = "@" + parameter.Name;

        // A reference-typed enumerable adds one part per element; a null collection contributes no parts, matching the
        // reflection builder's skip of a null parameter value.
        if (part.IsEnumerable)
        {
            var elementLocal = locals.New("refitPart");
            _ = sb.Append(bodyIndent).Append("if (").Append(valueExpression).AppendLine(" != null)")
                .Append(bodyIndent).AppendLine("{")
                .Append(bodyIndent).Append("    foreach (var ").Append(elementLocal).Append(" in ").Append(valueExpression).AppendLine(")")
                .Append(bodyIndent).AppendLine("    {");
            AppendMultipartAdd(sb, part, settingsLocal, contentLocal, elementLocal, bodyIndent + "        ");
            _ = sb.Append(bodyIndent).AppendLine("    }")
                .Append(bodyIndent).AppendLine("}");
            return;
        }

        // A null single value contributes no part, matching the reflection builder's null-parameter skip.
        if (parameter.CanBeNull)
        {
            _ = sb.Append(bodyIndent).Append("if (").Append(valueExpression).AppendLine(" != null)")
                .Append(bodyIndent).AppendLine("{");
            AppendMultipartAdd(sb, part, settingsLocal, contentLocal, valueExpression, bodyIndent + "    ");
            _ = sb.Append(bodyIndent).AppendLine("}");
            return;
        }

        AppendMultipartAdd(sb, part, settingsLocal, contentLocal, valueExpression, bodyIndent);
    }

    /// <summary>Appends the single <c>MultipartFormDataContent.Add</c> call for one value, per the part's dispatch arm.</summary>
    /// <param name="sb">The statement builder.</param>
    /// <param name="part">The multipart part descriptor.</param>
    /// <param name="settingsLocal">The generated settings local name.</param>
    /// <param name="contentLocal">The generated multipart content local name.</param>
    /// <param name="value">The value expression (a parameter accessor or a foreach element local).</param>
    /// <param name="indent">The statement indentation.</param>
    private static void AppendMultipartAdd(
        PooledStringBuilder sb,
        MultipartPartModel part,
        string settingsLocal,
        string contentLocal,
        string value,
        string indent)
    {
        var fieldName = ToCSharpStringLiteral(part.FieldName);
        var fileName = ToCSharpStringLiteral(part.FileName);
        _ = sb.Append(indent).Append(contentLocal).Append(".Add(");
        switch (part.Kind)
        {
            case MultipartPartKind.HttpContent:
            {
                _ = sb.Append(value).AppendLine(");");
                break;
            }

            case MultipartPartKind.MultipartItem:
            {
                _ = sb.Append(value).Append(".ToContent(), ").Append(value).Append(".Name ?? ").Append(fieldName)
                    .Append(", string.IsNullOrEmpty(").Append(value).Append(".FileName) ? ").Append(fileName)
                    .Append(" : ").Append(value).AppendLine(".FileName);");
                break;
            }

            case MultipartPartKind.Stream:
            {
                _ = sb.Append(StreamContentNew).Append(value).Append("), ").Append(fieldName).Append(", ")
                    .Append(fileName).AppendLine(");");
                break;
            }

            case MultipartPartKind.String:
            {
                _ = sb.Append(StringContentNew).Append(value).Append("), ").Append(fieldName).AppendLine(");");
                break;
            }

            case MultipartPartKind.FileInfo:
            {
                _ = sb.Append(StreamContentNew).Append(value).Append(".OpenRead()), ").Append(fieldName).Append(", ")
                    .Append(value).AppendLine(".Name);");
                break;
            }

            case MultipartPartKind.ByteArray:
            {
                _ = sb.Append(ByteArrayContentNew).Append(value).Append("), ").Append(fieldName).Append(", ")
                    .Append(fileName).AppendLine(");");
                break;
            }

            case MultipartPartKind.Serialized:
            {
                // A sealed/value part is JSON-serialized under its field name, matching AddSerializedMultipartItem's
                // serializer fallback. The declared type drives ToHttpContent<T>, so the serialized form matches; a
                // serialization failure is wrapped in the same descriptive ArgumentException the reflection builder raises.
                _ = sb.Append("global::Refit.GeneratedRequestRunner.SerializeMultipartPart(").Append(settingsLocal)
                    .Append(", ").Append(value).Append(", ").Append(fieldName).Append("), ")
                    .Append(fieldName).AppendLine(");");
                break;
            }

            default:
            {
                // Formattable: Guid/DateTime/etc. render through the form URL-encoded formatter, exactly as the
                // reflection builder's AddSerializedMultipartItem special case does.
                _ = sb.Append(StringContentNew).Append(settingsLocal)
                    .Append(".FormUrlEncodedParameterFormatter.Format(").Append(value).Append(", null) ?? string.Empty), ")
                    .Append(fieldName).AppendLine(");");
                break;
            }
        }
    }
}
