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

    /// <summary>The number of generated factory registrations emitted per non-generic interface.</summary>
    private const int RegistrationsPerInterface = 2;

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
        var memberSource = BuildInterfaceMemberSource(model, uniqueNames, requestBuilderFieldName, settingsFieldName);
        var typeParameterDocs = BuildTypeParameterDocumentation(model.Constraints, ClassMemberIndentation);
        var generatedCodeAttribute = GeneratedCodeAttribute;
        var settingsConstructorSource = BuildSettingsConstructor(model, settingsFieldName);
        var requestBuilderFieldType = model.SupportsNullable
            ? "global::Refit.IRequestBuilder?"
            : "global::Refit.IRequestBuilder";
        var source = $$"""
            {{BuildGeneratedFileHeader(model.Nullability, model.EmitGeneratedCodeMarkers)}}{{BuildExternAliasDirectives(model.ExternAliases)}}
            namespace Refit.Implementation
            {
                /// <summary>Contains generated Refit implementation types.</summary>
                internal partial class Generated
                {
                    /// <summary>Generated Refit implementation for {{ToXmlDocumentationText(model.InterfaceDisplayName)}}.</summary>
            {{typeParameterDocs}}        {{generatedCodeAttribute}}
                    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                    [global::System.Diagnostics.DebuggerNonUserCode]
                    [{{model.PreserveAttributeDisplayName}}]
                    [global::System.Reflection.Obfuscation(Exclude=true)]
                    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                    private sealed class {{model.Ns}}{{model.ClassDeclaration}}
                        : {{model.InterfaceDisplayName}}
            {{BuildConstraints(model.Constraints, false, ClassMemberIndentation)}}        {
                        /// <summary>The request builder used to create Refit method delegates.</summary>
                        private readonly {{requestBuilderFieldType}} {{requestBuilderFieldName}};

                        /// <summary>The settings used by this generated Refit implementation.</summary>
                        private readonly global::Refit.RefitSettings {{settingsFieldName}};

                        /// <summary>Gets the HTTP client used by this generated Refit implementation.</summary>
                        public global::System.Net.Http.HttpClient Client { get; }

                        /// <summary>Initializes a new instance of the {{model.Ns}}{{model.ClassSuffix}} class.</summary>
                        /// <param name="client">The HTTP client used by the generated implementation.</param>
                        /// <param name="requestBuilder">The request builder used to create Refit method delegates.</param>
                        public {{model.Ns}}{{model.ClassSuffix}}(global::System.Net.Http.HttpClient client, global::Refit.IRequestBuilder requestBuilder)
                        {
                            Client = client;
                            {{requestBuilderFieldName}} = requestBuilder;
                            {{settingsFieldName}} = requestBuilder.Settings;
                        }
            {{settingsConstructorSource}}{{memberSource}}        }
                }
            }

            """;
        return ToSourceText(source);
    }

    /// <summary>Concatenates the generated property, method, and dispose member blocks for an interface.</summary>
    /// <param name="model">The interface model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="requestBuilderFieldName">The unique generated field name that stores the request builder.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <returns>The concatenated member source.</returns>
    private static string BuildInterfaceMemberSource(
        InterfaceModel model,
        UniqueNameBuilder uniqueNames,
        string requestBuilderFieldName,
        string settingsFieldName)
    {
        var enumFormatterScope = new EnumFormatterScope(uniqueNames);
        var propertySource = BuildInterfaceProperties(model.Properties, model.SupportsNullable);
        var refitMethodSource = BuildRefitMethods(
            model.RefitMethods,
            true,
            model,
            uniqueNames,
            requestBuilderFieldName,
            settingsFieldName,
            enumFormatterScope);
        var derivedRefitMethodSource = BuildRefitMethods(
            model.DerivedRefitMethods,
            false,
            model,
            uniqueNames,
            requestBuilderFieldName,
            settingsFieldName,
            enumFormatterScope);
        var nonRefitMethodSource = BuildNonRefitMethods(model.NonRefitMethods, model.SupportsNullable);
        var disposableSource = BuildDisposableMethod(model.DisposeMethod);

        // Concatenate the five member blocks through a pooled buffer instead of a five-operand '+', which would
        // allocate a params string[] for the String.Concat overload.
        return new PooledStringBuilder(
                propertySource.Length + refitMethodSource.Length + derivedRefitMethodSource.Length
                + nonRefitMethodSource.Length + disposableSource.Length)
            .Append(propertySource)
            .Append(refitMethodSource)
            .Append(derivedRefitMethodSource)
            .Append(nonRefitMethodSource)
            .Append(disposableSource)
            .ToString();
    }

    /// <summary>Creates source text from generated source.</summary>
    /// <param name="source">The generated source.</param>
    /// <returns>The generated source text.</returns>
    private static SourceText ToSourceText(string source) => SourceText.From(source, Encoding.UTF8);

    /// <summary>Builds the generated code attribute using this generator assembly identity.</summary>
    /// <returns>The generated attribute source.</returns>
    private static string BuildGeneratedCodeAttribute()
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
    private static string ToXmlDocumentationText(string value)
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
    private static string BuildExternAliasDirectives(ImmutableEquatableArray<string> aliases)
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
    private static string BuildGeneratedFileHeader(Nullability nullability, bool emitGeneratedCodeMarkers)
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
    private static string BuildSharedGeneratedFileHeader(
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
    private static string BuildGeneratedFactoryRegistrations(ImmutableEquatableArray<InterfaceModel> interfaces)
    {
        var registrations = new string[interfaces.Count * RegistrationsPerInterface];
        var count = 0;
        for (var i = 0; i < interfaces.Count; i++)
        {
            var interfaceModel = interfaces[i];
            if (interfaceModel.ClassDeclaration.Contains("<"))
            {
                continue;
            }

            var generatedType = $"global::Refit.Implementation.Generated.{interfaceModel.Ns}{interfaceModel.ClassSuffix}";

            // The static modifier on a lambda is C# 9; older consumers must not see it.
            var lambdaModifier = interfaceModel.SupportsStaticLambdas ? "static " : string.Empty;
            registrations[count] = $$"""
                                    global::Refit.RestService.RegisterGeneratedFactory<{{interfaceModel.InterfaceDisplayName}}>(
                                        {{lambdaModifier}}(client, requestBuilder) => new {{generatedType}}(client, requestBuilder));

                        """;
            count++;

            if (CanUseGeneratedSettingsFactory(interfaceModel))
            {
                registrations[count] = $$"""
                                        global::Refit.RestService.RegisterGeneratedSettingsFactory<{{interfaceModel.InterfaceDisplayName}}>(
                                            {{lambdaModifier}}(client, settings) => new {{generatedType}}(client, settings));

                            """;
                count++;
            }
        }

        return count == 0 ? string.Empty : ConcatParts(registrations, count);
    }

    /// <summary>Builds the settings-only constructor when the generated implementation can avoid request-builder reflection.</summary>
    /// <param name="model">The interface model being emitted.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <returns>The generated constructor source, or an empty string.</returns>
    private static string BuildSettingsConstructor(InterfaceModel model, string settingsFieldName) =>
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
    private static bool CanUseGeneratedSettingsFactory(InterfaceModel model) =>
        (model.RefitMethods.Count != 0 || model.DerivedRefitMethods.Count != 0)
        && AllRequestsCanGenerateInline(model.RefitMethods)
        && AllRequestsCanGenerateInline(model.DerivedRefitMethods);

    /// <summary>Determines whether all methods in a collection use generated request construction.</summary>
    /// <param name="methods">The methods to inspect.</param>
    /// <returns><see langword="true"/> when each method can be emitted inline.</returns>
    private static bool AllRequestsCanGenerateInline(ImmutableEquatableArray<MethodModel> methods)
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
    private static string BuildTypeParameterDocumentation(
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

    /// <summary>Builds generated interface property implementations.</summary>
    /// <param name="properties">The property models to emit.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <returns>The generated property implementations.</returns>
    private static string BuildInterfaceProperties(
        ImmutableEquatableArray<InterfacePropertyModel> properties,
        bool supportsNullable)
    {
        var parts = new string[properties.Count];
        var count = 0;
        for (var i = 0; i < properties.Count; i++)
        {
            var source = BuildInterfaceProperty(properties[i], supportsNullable);
            if (source.Length != 0)
            {
                parts[count] = source;
                count++;
            }
        }

        return count == 0 ? string.Empty : ConcatParts(parts, count);
    }

    /// <summary>Builds generated Refit method implementations.</summary>
    /// <param name="methods">The method models to emit.</param>
    /// <param name="isTopLevel">True if directly from the type we're generating for, false for methods found on base interfaces.</param>
    /// <param name="interfaceModel">The interface model being emitted.</param>
    /// <param name="uniqueNames">Contains the unique member names in the interface scope.</param>
    /// <param name="requestBuilderFieldName">The unique generated field name that stores the request builder.</param>
    /// <param name="settingsFieldName">The unique generated field name that stores Refit settings.</param>
    /// <param name="enumFormatterScope">The enum formatter scope for the interface.</param>
    /// <returns>The generated method implementations.</returns>
    private static string BuildRefitMethods(
        ImmutableEquatableArray<MethodModel> methods,
        bool isTopLevel,
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames,
        string requestBuilderFieldName,
        string settingsFieldName,
        EnumFormatterScope enumFormatterScope)
    {
        if (methods.Count == 0)
        {
            return string.Empty;
        }

        var parts = new string[methods.Count];
        for (var i = 0; i < methods.Count; i++)
        {
            parts[i] = BuildRefitMethod(
                methods[i],
                isTopLevel,
                interfaceModel,
                uniqueNames,
                requestBuilderFieldName,
                settingsFieldName,
                enumFormatterScope);
        }

        return ConcatParts(parts, parts.Length);
    }

    /// <summary>Builds generated non-Refit method stubs.</summary>
    /// <param name="methods">The non-Refit method models to emit.</param>
    /// <param name="supportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
    /// <returns>The generated method stubs.</returns>
    private static string BuildNonRefitMethods(
        ImmutableEquatableArray<MethodModel> methods,
        bool supportsNullable)
    {
        if (methods.Count == 0)
        {
            return string.Empty;
        }

        var parts = new string[methods.Count];
        for (var i = 0; i < methods.Count; i++)
        {
            parts[i] = BuildNonRefitMethod(methods[i], supportsNullable);
        }

        return ConcatParts(parts, parts.Length);
    }

    /// <summary>Converts a bool to a lowercase C# literal.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The lowercase bool literal.</returns>
    private static string ToLowerInvariantString(bool value) => value ? TrueLiteral : FalseLiteral;

    /// <summary>Converts a string into a C# string literal.</summary>
    /// <param name="value">The value to quote.</param>
    /// <returns>The escaped C# string literal.</returns>
    private static string ToCSharpStringLiteral(string value)
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
    private static bool NeedsCSharpEscaping(string value)
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
    private static string BuildNonRefitMethod(MethodModel methodModel, bool supportsNullable)
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
    private static string BuildInterfaceProperty(
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

    /// <summary>Builds the explicit IDisposable.Dispose implementation.</summary>
    /// <param name="shouldEmit">True when the dispose method should be emitted.</param>
    /// <returns>The generated dispose method, or an empty string.</returns>
    private static string BuildDisposableMethod(bool shouldEmit)
    {
        if (!shouldEmit)
        {
            return string.Empty;
        }

        var methodIndent = Indent(MethodMemberIndentation);
        var bodyIndent = Indent(MethodBodyIndentation);
        return $$"""

            {{methodIndent}}/// <inheritdoc />
            {{methodIndent}}void global::System.IDisposable.Dispose()
            {{methodIndent}}{
            {{bodyIndent}}Client?.Dispose();
            {{methodIndent}}}

            """;
    }
}
