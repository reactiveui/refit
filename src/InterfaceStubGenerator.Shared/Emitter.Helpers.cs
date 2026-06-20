// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Refit.Generator;

/// <summary>Internal emitter helpers that are directly covered by focused tests.</summary>
internal static partial class Emitter
{
    /// <summary>Builds the request-body buffering expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter, if any.</param>
    /// <returns>The buffering expression.</returns>
    internal static string BuildBufferBodyExpression(RequestParameterModel? bodyParameter) =>
        bodyParameter is null
            ? FalseLiteral
            : bodyParameter.BodyBufferMode switch
            {
                BodyBufferMode.Settings => "______settings.Buffered",
                BodyBufferMode.Buffered => TrueLiteral,
                _ => FalseLiteral
            };

    /// <summary>Builds the serialized-body streaming expression for an inline generated method.</summary>
    /// <param name="bodyParameter">The parsed body parameter.</param>
    /// <returns>The streaming expression.</returns>
    internal static string BuildStreamBodyExpression(RequestParameterModel bodyParameter) =>
        bodyParameter.BodySerializationMethod == "UrlEncoded"
            ? FalseLiteral
            : bodyParameter.BodyBufferMode switch
            {
                BodyBufferMode.Settings => "!______settings.Buffered",
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

    /// <summary>Ensures a type display name is prefixed with <c>global::</c>.</summary>
    /// <param name="typeName">The type display name.</param>
    /// <returns>The globally qualified type display name.</returns>
    internal static string EnsureGlobalPrefix(string typeName) =>
        typeName.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? typeName
            : GlobalPrefix + typeName;

    /// <summary>Maps a parsed HTTP method name to an expression that creates or returns an <see cref="HttpMethod"/>.</summary>
    /// <param name="httpMethod">The HTTP method text.</param>
    /// <returns>The HTTP method expression.</returns>
    [ExcludeFromCodeCoverage]
    internal static string ToHttpMethodExpression(string httpMethod) =>
        httpMethod switch
        {
            "DELETE" => "global::System.Net.Http.HttpMethod.Delete",
            "GET" => "global::System.Net.Http.HttpMethod.Get",
            "HEAD" => "global::System.Net.Http.HttpMethod.Head",
            "OPTIONS" => "global::System.Net.Http.HttpMethod.Options",
            "POST" => "global::System.Net.Http.HttpMethod.Post",
            "PUT" => "global::System.Net.Http.HttpMethod.Put",
            "PATCH" => "new global::System.Net.Http.HttpMethod(\"PATCH\")",
            _ => throw new ArgumentOutOfRangeException(nameof(httpMethod), httpMethod, "Unsupported HTTP method.")
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
            ReturnTypeInfo.Return => (false, "return ", string.Empty),
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
    internal static void AppendEscapedCharacter(StringBuilder builder, char character) =>
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
            _ => builder.Append(character)
        };

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

    /// <summary>Emits the method signature, constraints, and opening brace.</summary>
    /// <param name="source">The source writer to emit to.</param>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="isDerivedExplicitImpl">True if the method is a derived explicit implementation.</param>
    /// <param name="isExplicitInterface">True if the method is an explicit interface implementation.</param>
    /// <param name="isAsync">True if the method should be emitted as async.</param>
    internal static void WriteMethodOpening(
        SourceWriter source,
        MethodModel methodModel,
        bool isDerivedExplicitImpl,
        bool isExplicitInterface,
        bool isAsync = false)
    {
        var visibility = !isExplicitInterface ? "public " : string.Empty;
        var asyncKeyword = isAsync ? "async " : string.Empty;

        source.WriteLine();
        source.WriteLine("/// <inheritdoc />");
        source.WriteIndentation();
        source.Append(visibility);
        source.Append(asyncKeyword);
        source.Append(methodModel.ReturnType);
        source.Append(' ');

        if (isExplicitInterface)
        {
            var ct = methodModel.ContainingType;
            if (!ct.StartsWith(GlobalPrefix, StringComparison.Ordinal))
            {
                source.Append(GlobalPrefix);
            }

            source.Append(ct);
            source.Append('.');
        }

        source.Append(methodModel.DeclaredMethod);
        source.Append('(');

        var parameters = methodModel.Parameters.AsArray();
        if (parameters.Length > 0)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    source.Append(", ");
                }

                var (metadataName, type, annotation, _) = parameters[i];
                source.Append(type);
                if (annotation)
                {
                    source.Append('?');
                }

                source.Append(" @");
                source.Append(metadataName);
            }
        }

        source.Append(')');
        source.WriteLine();
        source.Indentation++;
        GenerateConstraints(source, methodModel.Constraints, isDerivedExplicitImpl || isExplicitInterface);
        source.Indentation--;
        source.WriteLine("{");
        source.Indentation++;
    }
}
