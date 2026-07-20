// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

/// <summary>Emits the generated source code for Refit interface implementations.</summary>
internal static partial class Emitter
{
    /// <summary>The generated literal for <see langword="false"/>.</summary>
    private const string FalseLiteral = "false";

    /// <summary>The C# null keyword literal.</summary>
    private const string NullLiteral = "null";

    /// <summary>The number of quote characters wrapping a C# string literal.</summary>
    private const int StringLiteralQuoteLength = 2;

    /// <summary>The rendered length of the <c>", "</c> separator emitted between joined items.</summary>
    private const int ListSeparatorLength = 2;

    /// <summary>The radix used when rendering decimal integers.</summary>
    private const int DecimalRadix = 10;

    /// <summary>The generated literal for <see langword="true"/>.</summary>
    private const string TrueLiteral = "true";

    /// <summary>The C# global namespace alias prefix.</summary>
    private const string GlobalPrefix = "global::";

    /// <summary>The variable name used for the cached type parameter array field.</summary>
    private const string TypeParameterVariableName = "______typeParameters";

    /// <summary>The exception message emitted for interface methods without usable Refit metadata.</summary>
    private const string NonRefitMethodExceptionMessage =
        "Either this method has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument.";

    /// <summary>The generated prefix for type expressions.</summary>
    private const string TypeOfPrefix = "typeof(";

    /// <summary>The fully qualified namespace prefix that precedes the assembly-scoped generated implementation container type.</summary>
    private const string GeneratedNamespacePrefix = "global::Refit.Implementation.";

    /// <summary>The number of spaces in one generated indentation level.</summary>
    private const int CharsPerIndentation = 4;

    /// <summary>The highest indentation level kept in the shared <see cref="IndentCache"/>; deeper levels allocate.</summary>
    private const int MaxCachedIndentLevel = 8;

    /// <summary>Indentation level for generated nested implementation classes.</summary>
    private const int ClassMemberIndentation = 2;

    /// <summary>Indentation level for generated method members.</summary>
    private const int MethodMemberIndentation = 3;

    /// <summary>Indentation level for generated method constraints and statements.</summary>
    private const int MethodBodyIndentation = 4;

    /// <summary>The generated attribute that identifies source produced by this generator.</summary>
    private static readonly string GeneratedCodeAttribute = BuildGeneratedCodeAttribute();

    /// <summary>The XML metacharacters that force <see cref="ToXmlDocumentationText"/> onto its escaping path.</summary>
    private static readonly char[] XmlEscapeChars = ['&', '<', '>'];

    /// <summary>Emits the shared preserve attribute and factory registration code.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    public static void EmitSharedCode(
        ContextGenerationModel model,
        Action<string, SourceText> addSource)
    {
        if (model.Interfaces.Count == 0)
        {
            return;
        }

        var generatedFileHeader = BuildSharedGeneratedFileHeader(model.Interfaces, model.EmitGeneratedCodeMarkers);
        EmitPreserveAttribute(model, generatedFileHeader, addSource);
        EmitGeneratedFactoryModule(model, generatedFileHeader, addSource);
    }

    /// <summary>Emits the generated implementation source for a single interface.</summary>
    /// <param name="model">The interface model to emit.</param>
    /// <returns>The generated source text for the interface implementation.</returns>
    public static SourceText EmitInterface(InterfaceModel model)
    {
        var uniqueNames = new UniqueNameBuilder();
        uniqueNames.Reserve(model.MemberNames);
        var requestBuilderFieldName = uniqueNames.New("_requestBuilder");
        var settingsFieldName = uniqueNames.New("_settings");

        // Assemble the interface source in one pooled buffer: the fixed wrapper (header, type declaration, fields, and
        // constructors up to the settings constructor), then the member blocks appended straight in, then the closing
        // braces. Neither the wrapper nor the member blocks materialize as their own string before the source is built.
        var builder = new PooledStringBuilder();
        AppendInterfacePrefix(builder, model, requestBuilderFieldName, settingsFieldName);
        AppendInterfaceMemberSource(builder, model, uniqueNames, requestBuilderFieldName, settingsFieldName);

        // Close the implementation class, the Generated partial class, and the Refit.Implementation namespace.
        _ = builder.AppendLine("        }").AppendLine("    }").AppendLine("}");
        return ToSourceText(builder.ToString());
    }

    /// <summary>Appends the fixed class wrapper up to the settings constructor straight into the interface buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface source.</param>
    /// <param name="model">The interface model being emitted.</param>
    /// <param name="requestBuilderFieldName">The unique generated field name that stores the request builder.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    internal static void AppendInterfacePrefix(
        PooledStringBuilder builder,
        InterfaceModel model,
        string requestBuilderFieldName,
        string settingsFieldName)
    {
        var typeParameterDocs = BuildTypeParameterDocumentation(model.Constraints, ClassMemberIndentation);
        var requestBuilderFieldType = model.SupportsNullable
            ? "global::Refit.IRequestBuilder?"
            : "global::Refit.IRequestBuilder";
        var settingsConstructorSource = BuildSettingsConstructor(model, settingsFieldName);

        _ = builder
            .Append(BuildGeneratedFileHeader(model.Nullability, model.EmitGeneratedCodeMarkers))
            .Append(BuildExternAliasDirectives(model.ExternAliases)).AppendLine()
            .AppendLine("namespace Refit.Implementation")
            .AppendLine("{")
            .AppendLine("    /// <summary>Contains generated Refit implementation types.</summary>")
            .Append("    internal partial class ").Append(model.GeneratedClassName).AppendLine()
            .AppendLine("    {")
            .Append("        /// <summary>Generated Refit implementation for ").Append(ToXmlDocumentationText(model.InterfaceDisplayName)).AppendLine(".</summary>")
            .Append(typeParameterDocs).Append("        ").Append(GeneratedCodeAttribute).AppendLine()
            .AppendLine("        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]")
            .AppendLine("        [global::System.Diagnostics.DebuggerNonUserCode]")
            .Append("        [").Append(model.PreserveAttributeDisplayName).AppendLine("]")
            .AppendLine("        [global::System.Reflection.Obfuscation(Exclude=true)]")
            .AppendLine("        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]")
            .Append("        private sealed class ").Append(model.Ns).Append(model.ClassDeclaration).AppendLine()
            .Append("            : ").Append(model.InterfaceDisplayName).AppendLine()
            .Append(BuildConstraints(model.Constraints, false, ClassMemberIndentation)).AppendLine("        {")
            .AppendLine("            /// <summary>The request builder used to create Refit method delegates.</summary>")
            .Append("            private readonly ").Append(requestBuilderFieldType).Append(" ").Append(requestBuilderFieldName).AppendLine(";")
            .AppendLine()
            .AppendLine("            /// <summary>The settings used by this generated Refit implementation.</summary>")
            .Append("            private readonly global::Refit.RefitSettings ").Append(settingsFieldName).AppendLine(";")
            .AppendLine()
            .AppendLine("            /// <summary>Gets the HTTP client used by this generated Refit implementation.</summary>")
            .AppendLine("            public global::System.Net.Http.HttpClient Client { get; }")
            .AppendLine()
            .Append("            /// <summary>Initializes a new instance of the ").Append(model.Ns).Append(model.ClassSuffix).AppendLine(" class.</summary>")
            .AppendLine("            /// <param name=\"client\">The HTTP client used by the generated implementation.</param>")
            .AppendLine("            /// <param name=\"requestBuilder\">The request builder used to create Refit method delegates.</param>")
            .Append("            public ").Append(model.Ns).Append(model.ClassSuffix).AppendLine("(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)")
            .AppendLine("            {")
            .AppendLine("                Client = client;")
            .Append("                ").Append(requestBuilderFieldName).AppendLine(" = requestBuilder;")
            .Append("                ").Append(settingsFieldName).AppendLine(" = requestBuilder.Settings;")
            .AppendLine("            }")
            .Append(settingsConstructorSource);
    }

    /// <summary>Appends the generated property, method, and dispose member blocks for an interface into the buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface source.</param>
    /// <param name="model">The interface model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="requestBuilderFieldName">The unique generated field name that stores the request builder.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    internal static void AppendInterfaceMemberSource(
        PooledStringBuilder builder,
        InterfaceModel model,
        UniqueNameBuilder uniqueNames,
        string requestBuilderFieldName,
        string settingsFieldName)
    {
        var enumFormatterScope = new EnumFormatterScope(uniqueNames);
        var fieldNames = new GeneratedFieldNames(requestBuilderFieldName, settingsFieldName);

        // Append the five member blocks (properties, top-level and derived Refit methods, non-Refit stubs, and dispose)
        // straight into the interface buffer so none of the block strings materialize before the source is built.
        AppendInterfaceProperties(builder, model.Properties, model.SupportsNullable);
        AppendRefitMethods(builder, model.RefitMethods, true, model, uniqueNames, fieldNames, enumFormatterScope);
        AppendRefitMethods(builder, model.DerivedRefitMethods, false, model, uniqueNames, fieldNames, enumFormatterScope);
        AppendNonRefitMethods(builder, model.NonRefitMethods, model.SupportsNullable);
        AppendDisposableMethod(builder, model.DisposeMethod);
    }

    /// <summary>Creates source text from generated source.</summary>
    /// <param name="source">The generated source.</param>
    /// <returns>The generated source text.</returns>
    internal static SourceText ToSourceText(string source) => SourceText.From(source, Encoding.UTF8);

    /// <summary>Builds the generated code attribute using this generator assembly identity.</summary>
    /// <returns>The generated attribute source.</returns>
    internal static string BuildGeneratedCodeAttribute()
    {
        // This generator assembly always carries a name and version, so no placeholder fallback is reachable.
        var assemblyName = typeof(Emitter).Assembly.GetName();
        return
            "[global::System.CodeDom.Compiler.GeneratedCodeAttribute("
            + ToCSharpStringLiteral(assemblyName.Name!)
            + ", "
            + ToCSharpStringLiteral(assemblyName.Version!.ToString())
            + ")]";
    }

    /// <summary>Escapes text for generated XML documentation comments.</summary>
    /// <param name="value">The text to escape.</param>
    /// <returns>The escaped XML documentation text.</returns>
    internal static string ToXmlDocumentationText(string value)
    {
        // Most identifiers and type names contain none of the XML metacharacters, so return them untouched
        // instead of allocating a builder and copying character by character.
        if (value.IndexOfAny(XmlEscapeChars) < 0)
        {
            return value;
        }

        var builder = new PooledStringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&':
                    {
                        _ = builder.Append("&amp;");
                        break;
                    }

                case '<':
                    {
                        _ = builder.Append("&lt;");
                        break;
                    }

                case '>':
                    {
                        _ = builder.Append("&gt;");
                        break;
                    }

                default:
                    {
                        _ = builder.Append(c);
                        break;
                    }
            }
        }

        return builder.ToString();
    }

    /// <summary>Builds the <c>extern alias</c> directives an interface's types require, if any.</summary>
    /// <param name="aliases">The extern aliases the interface's types reference.</param>
    /// <returns>The directives, or an empty string when none are needed.</returns>
    internal static string BuildExternAliasDirectives(ImmutableEquatableArray<string> aliases)
    {
        if (aliases.Count == 0)
        {
            return string.Empty;
        }

        var builder = new PooledStringBuilder();
        foreach (var alias in aliases)
        {
            _ = builder.Append("extern alias ").Append(alias).AppendLine(";");
        }

        return builder.ToString();
    }

    /// <summary>Builds the generated file header for an interface implementation.</summary>
    /// <param name="nullability">The nullable context for the generated source.</param>
    /// <param name="emitGeneratedCodeMarkers">Whether generated-code markers should be emitted.</param>
    /// <returns>The generated file header.</returns>
    internal static string BuildGeneratedFileHeader(Nullability nullability, bool emitGeneratedCodeMarkers)
    {
        if (!emitGeneratedCodeMarkers)
        {
            return nullability == Nullability.None
                ? """
                  // Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
                  // ReactiveUI and Contributors licenses this file to you under the MIT license.
                  // See the LICENSE file in the project root for full license information.

                  """
                : """
                  // Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
                  // ReactiveUI and Contributors licenses this file to you under the MIT license.
                  // See the LICENSE file in the project root for full license information.

                  #nullable enable annotations
                  #nullable disable warnings

                  """;
        }

        return nullability == Nullability.None
            ? "// <auto-generated/>\n"
            : """
              // <auto-generated/>

              #nullable enable annotations
              #nullable disable warnings

              """;
    }

    /// <summary>Builds the generated file header for shared generated files.</summary>
    /// <param name="interfaces">The parsed interface models.</param>
    /// <param name="emitGeneratedCodeMarkers">Whether generated-code markers should be emitted.</param>
    /// <returns>The generated file header.</returns>
    internal static string BuildSharedGeneratedFileHeader(
        ImmutableEquatableArray<InterfaceModel> interfaces,
        bool emitGeneratedCodeMarkers)
    {
        for (var i = 0; i < interfaces.Count; i++)
        {
            if (interfaces[i].Nullability != Nullability.None)
            {
                return BuildGeneratedFileHeader(interfaces[i].Nullability, emitGeneratedCodeMarkers);
            }
        }

        return BuildGeneratedFileHeader(Nullability.None, emitGeneratedCodeMarkers);
    }

    /// <summary>Builds the generated factory registrations for non-generic interfaces.</summary>
    /// <param name="interfaces">The parsed interface models.</param>
    /// <returns>The generated factory registrations.</returns>
    internal static string BuildGeneratedFactoryRegistrations(ImmutableEquatableArray<InterfaceModel> interfaces)
    {
        // Append each registration straight into one pooled buffer instead of a per-registration string array joined by
        // ConcatParts, so the array and the trimmed join result never materialize.
        var builder = new PooledStringBuilder();
        for (var i = 0; i < interfaces.Count; i++)
        {
            var interfaceModel = interfaces[i];
            if (interfaceModel.ClassDeclaration.Contains("<"))
            {
                continue;
            }

            // The static modifier on a lambda is C# 9; older consumers must not see it.
            var lambdaModifier = interfaceModel.SupportsStaticLambdas ? "static " : string.Empty;
            _ = builder
                .Append("            global::Refit.RestService.RegisterGeneratedFactory<").Append(interfaceModel.InterfaceDisplayName).AppendLine(">(")
                .Append("                ").Append(lambdaModifier).AppendLine("(client, requestBuilder) =>")
                .Append("                    new ").Append(GeneratedNamespacePrefix).Append(interfaceModel.GeneratedClassName).Append('.')
                .Append(interfaceModel.Ns).Append(interfaceModel.ClassSuffix).AppendLine("(client, requestBuilder));");

            if (CanUseGeneratedSettingsFactory(interfaceModel))
            {
                _ = builder
                    .Append("            global::Refit.RestService.RegisterGeneratedSettingsFactory<").Append(interfaceModel.InterfaceDisplayName).AppendLine(">(")
                    .Append("                ").Append(lambdaModifier).AppendLine("(client, settings) =>")
                    .Append("                    new ").Append(GeneratedNamespacePrefix).Append(interfaceModel.GeneratedClassName).Append('.')
                    .Append(interfaceModel.Ns).Append(interfaceModel.ClassSuffix).AppendLine("(client, settings));");
            }
        }

        return builder.ToString();
    }

    /// <summary>Builds the settings-only constructor when the generated implementation can avoid request-builder reflection.</summary>
    /// <param name="model">The interface model being emitted.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <returns>The generated constructor source, or an empty string.</returns>
    internal static string BuildSettingsConstructor(InterfaceModel model, string settingsFieldName) =>
        !CanUseGeneratedSettingsFactory(model)
            ? string.Empty
            : $$"""

                            /// <summary>Initializes a new instance of the {{model.Ns}}{{model.ClassSuffix}} class for generated-only execution.</summary>
                            /// <param name="client">The HTTP client used by the generated implementation.</param>
                            /// <param name="settings">The settings used by the generated implementation.</param>
                            public {{model.Ns}}{{model.ClassSuffix}}(global::System.Net.Http.HttpClient client, global::Refit.RefitSettings settings)
                            {
                                Client = client;
                                {{settingsFieldName}} = settings;
                            }
                """;

    /// <summary>Determines whether an interface can be constructed without a reflection request builder.</summary>
    /// <param name="model">The interface model being emitted.</param>
    /// <returns><see langword="true"/> when all Refit methods use generated request construction.</returns>
    internal static bool CanUseGeneratedSettingsFactory(InterfaceModel model) =>
        (model.RefitMethods.Count != 0 || model.DerivedRefitMethods.Count != 0)
        && AllRequestsCanGenerateInline(model.RefitMethods)
        && AllRequestsCanGenerateInline(model.DerivedRefitMethods);

    /// <summary>Determines whether all methods in a collection use generated request construction.</summary>
    /// <param name="methods">The methods to inspect.</param>
    /// <returns><see langword="true"/> when each method can be emitted inline.</returns>
    internal static bool AllRequestsCanGenerateInline(ImmutableEquatableArray<MethodModel> methods)
    {
        for (var i = 0; i < methods.Count; i++)
        {
            if (!methods[i].Request.CanGenerateInline)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Builds documentation for generated type parameters.</summary>
    /// <param name="typeParameters">The parsed type parameter constraints.</param>
    /// <param name="indentationLevel">The generated indentation level.</param>
    /// <returns>The generated type parameter documentation.</returns>
    internal static string BuildTypeParameterDocumentation(
        ImmutableEquatableArray<TypeConstraint> typeParameters,
        int indentationLevel)
    {
        if (typeParameters.Count == 0)
        {
            return string.Empty;
        }

        var parts = new string[typeParameters.Count];
        var indentation = Indent(indentationLevel);
        for (var i = 0; i < typeParameters.Count; i++)
        {
            parts[i] = $"{indentation}/// <typeparam name=\"{typeParameters[i].DeclaredName}\">The generated interface type parameter.</typeparam>\n";
        }

        return ConcatParts(parts, parts.Length);
    }

    /// <summary>Appends the generated interface property implementations into the member buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated member source.</param>
    /// <param name="properties">The property models to emit.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    internal static void AppendInterfaceProperties(
        PooledStringBuilder builder,
        ImmutableEquatableArray<InterfacePropertyModel> properties,
        bool supportsNullable)
    {
        for (var i = 0; i < properties.Count; i++)
        {
            // A satisfied member emits an empty string, which appends as a no-op.
            _ = builder.Append(BuildInterfaceProperty(properties[i], supportsNullable));
        }
    }

    /// <summary>Appends the generated Refit method implementations into the member buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated member source.</param>
    /// <param name="methods">The method models to emit.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="fieldNames">The generated request-builder and settings backing-field names.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    internal static void AppendRefitMethods(
        PooledStringBuilder builder,
        ImmutableEquatableArray<MethodModel> methods,
        bool isTopLevel,
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames,
        GeneratedFieldNames fieldNames,
        EnumFormatterScope enumFormatterScope)
    {
        for (var i = 0; i < methods.Count; i++)
        {
            BuildRefitMethod(
                builder,
                methods[i],
                isTopLevel,
                interfaceModel,
                uniqueNames,
                fieldNames,
                enumFormatterScope);
        }
    }

    /// <summary>Appends the generated non-Refit method stubs into the member buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated member source.</param>
    /// <param name="methods">The non-Refit method models to emit.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    internal static void AppendNonRefitMethods(
        PooledStringBuilder builder,
        ImmutableEquatableArray<MethodModel> methods,
        bool supportsNullable)
    {
        for (var i = 0; i < methods.Count; i++)
        {
            _ = builder.Append(BuildNonRefitMethod(methods[i], supportsNullable));
        }
    }

    /// <summary>Converts a bool to a lowercase C# literal.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The lowercase bool literal.</returns>
    internal static string ToLowerInvariantString(bool value) => value ? TrueLiteral : FalseLiteral;

    /// <summary>Converts a string into a C# string literal.</summary>
    /// <param name="value">The value to quote.</param>
    /// <returns>The escaped C# string literal.</returns>
    internal static string ToCSharpStringLiteral(string value)
    {
        // The vast majority of emitted literals (query keys, header names, paths) need no escaping, so wrap them in
        // quotes with a single concat instead of allocating a builder and appending each character.
        if (!NeedsCSharpEscaping(value))
        {
            return "\"" + value + "\"";
        }

        var builder = new PooledStringBuilder(value.Length + StringLiteralQuoteLength);
        _ = builder.Append('"');
        foreach (var c in value)
        {
            AppendEscapedCharacter(builder, c);
        }

        _ = builder.Append('"');
        return builder.ToString();
    }

    /// <summary>Determines whether any character in a value requires C# string-literal escaping.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><see langword="true"/> when at least one character needs an escape sequence.</returns>
    internal static bool NeedsCSharpEscaping(string value)
    {
        foreach (var c in value)
        {
            if (EscapeSequence(c) is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds a stub body for a non-Refit method that throws at runtime.</summary>
    /// <param name="methodModel">The method model being emitted.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <returns>The generated method stub.</returns>
    internal static string BuildNonRefitMethod(in MethodModel methodModel, bool supportsNullable)
    {
        var isExplicit = methodModel.IsExplicitInterface;
        var messageLiteral = ToCSharpStringLiteral(NonRefitMethodExceptionMessage);
        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);

        return $$"""
            {{BuildMethodOpening(methodModel, isExplicit, isExplicit, supportsNullable)}}{{bodyIndent}}throw new global::System.NotImplementedException({{messageLiteral}});
            {{methodIndent}}}

            """;
    }

    /// <summary>Builds an interface property implementation.</summary>
    /// <param name="property">The property model being emitted.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <returns>The generated property implementation, or an empty string.</returns>
    internal static string BuildInterfaceProperty(
        InterfacePropertyModel property,
        bool supportsNullable)
    {
        if (property.IsSatisfiedByGeneratedMember)
        {
            return string.Empty;
        }

        var methodIndent = Indent(MethodMemberIndentation);
        var visibility = property.IsExplicitInterface ? string.Empty : "public ";
        var annotation = supportsNullable && property.Annotation ? "?" : string.Empty;
        var explicitInterface = property.IsExplicitInterface
            ? EnsureGlobalPrefix(property.ContainingType) + "."
            : string.Empty;
        var getter = property.HasGetter ? " get;" : string.Empty;
        var setter = property.HasSetter ? " set;" : string.Empty;

        return $$"""

            {{methodIndent}}/// <inheritdoc />
            {{methodIndent}}{{visibility}}{{property.Type}}{{annotation}} {{explicitInterface}}{{property.Name}} { {{getter}}{{setter}} }

            """;
    }

    /// <summary>Appends the explicit IDisposable.Dispose implementation into the member buffer.</summary>
    /// <param name="builder">The buffer accumulating the interface's generated member source.</param>
    /// <param name="shouldEmit">True when the dispose method should be emitted.</param>
    internal static void AppendDisposableMethod(PooledStringBuilder builder, bool shouldEmit)
    {
        if (!shouldEmit)
        {
            return;
        }

        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);
        _ = builder.Append($$"""

            {{methodIndent}}/// <inheritdoc />
            {{methodIndent}}void global::System.IDisposable.Dispose()
            {{methodIndent}}{
            {{bodyIndent}}Client?.Dispose();
            {{methodIndent}}}

            """);
    }
}
