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

    /// <summary>Indentation level for generated nested implementation classes.</summary>
    private const int ClassMemberIndentation = 2;

    /// <summary>Indentation level for generated method members.</summary>
    private const int MethodMemberIndentation = 3;

    /// <summary>Indentation level for generated method constraints and statements.</summary>
    private const int MethodBodyIndentation = 4;

    /// <summary>The generated attribute that identifies source produced by this generator.</summary>
    private static readonly string GeneratedCodeAttribute = BuildGeneratedCodeAttribute();

#if NETSTANDARD2_0
    /// <summary>Delegate used to fill a generated string buffer.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="state">The caller supplied state.</param>
    private delegate void GeneratedStringAction<in TState>(char[] destination, TState state);
#else
    /// <summary>Delegate used to fill a generated string buffer.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="state">The caller supplied state.</param>
    private delegate void GeneratedStringAction<in TState>(Span<char> destination, TState state);
#endif

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

        const string attributeUsageLine =
            "[global::System.AttributeUsage (global::System.AttributeTargets.Class | "
            + "global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | "
            + "global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | "
            + "global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | "
            + "global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | "
            + "global::System.AttributeTargets.Delegate)]";

        var generatedFileHeader = BuildSharedGeneratedFileHeader(model.Interfaces, model.EmitGeneratedCodeMarkers);
        var generatedCodeAttribute = GeneratedCodeAttribute;
        var attributeText = $$"""
            {{generatedFileHeader}}
            namespace {{model.RefitInternalNamespace}}
            {
                /// <summary>Identifies generated members that should be preserved by tools that honor this attribute.</summary>
                {{generatedCodeAttribute}}
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                {{attributeUsageLine}}
                internal sealed class PreserveAttribute : global::System.Attribute
                {
                    /// <summary>Gets or sets a value indicating whether all members should be preserved.</summary>
                    public bool AllMembers { get; set; }

                    /// <summary>Gets or sets a value indicating whether preservation should be conditional.</summary>
                    public bool Conditional { get; set; }
                }
            }

            """;

        // add the attribute text
        addSource("PreserveAttribute.g.cs", SourceText.From(attributeText, Encoding.UTF8));

        const string dynamicDependencyLine =
            "[System.Diagnostics.CodeAnalysis.DynamicDependency("
            + "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, "
            + "typeof(global::Refit.Implementation.Generated))]";

        var generatedSource = $$"""
            {{generatedFileHeader}}
            namespace Refit.Implementation
            {
                /// <summary>Registers generated Refit factories for interfaces discovered at compile time.</summary>
                {{generatedCodeAttribute}}
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [{{model.PreserveAttributeDisplayName}}]
                [global::System.Reflection.Obfuscation(Exclude=true)]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static partial class Generated
                {
            #if NET5_0_OR_GREATER
                    /// <summary>Registers generated Refit factories when the assembly is loaded.</summary>
                    [global::System.Diagnostics.CodeAnalysis.SuppressMessage(
                        "Usage",
                        "CA2255:The ModuleInitializer attribute should not be used in libraries",
                        Justification = "ModuleInitializer is used intentionally so generated Refit factories are registered when the assembly loads.")]
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    {{dynamicDependencyLine}}
                    internal static void Initialize()
                    {
            {{BuildGeneratedFactoryRegistrations(model.Interfaces)}}        }
            #endif
                }
            }

            """;
        addSource("Generated.g.cs", ToSourceText(generatedSource));
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
        var propertySource = BuildInterfaceProperties(model.Properties, model.SupportsNullable);
        var refitMethodSource = BuildRefitMethods(
            model.RefitMethods,
            true,
            model,
            uniqueNames,
            requestBuilderFieldName,
            settingsFieldName);
        var derivedRefitMethodSource = BuildRefitMethods(
            model.DerivedRefitMethods,
            false,
            model,
            uniqueNames,
            requestBuilderFieldName,
            settingsFieldName);
        var nonRefitMethodSource = BuildNonRefitMethods(model.NonRefitMethods, model.SupportsNullable);
        var disposableSource = BuildDisposableMethod(model.DisposeMethod);
        var memberSource = propertySource + refitMethodSource + derivedRefitMethodSource + nonRefitMethodSource + disposableSource;
        var typeParameterDocs = BuildTypeParameterDocumentation(model.Constraints, ClassMemberIndentation);
        var generatedCodeAttribute = GeneratedCodeAttribute;
        var settingsConstructorSource = BuildSettingsConstructor(model, settingsFieldName);
        var requestBuilderFieldType = model.SupportsNullable
            ? "global::Refit.IRequestBuilder?"
            : "global::Refit.IRequestBuilder";
        var source = $$"""
            {{BuildGeneratedFileHeader(model.Nullability, model.EmitGeneratedCodeMarkers)}}
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

    /// <summary>Creates source text from generated source.</summary>
    /// <param name="source">The generated source.</param>
    /// <returns>The generated source text.</returns>
    private static SourceText ToSourceText(string source) => SourceText.From(source, Encoding.UTF8);

    /// <summary>Builds the generated code attribute using this generator assembly identity.</summary>
    /// <returns>The generated attribute source.</returns>
    private static string BuildGeneratedCodeAttribute()
    {
        var assemblyName = typeof(Emitter).Assembly.GetName();
        return
            "[global::System.CodeDom.Compiler.GeneratedCodeAttribute("
            + ToCSharpStringLiteral(assemblyName.Name ?? "Refit.Generator")
            + ", "
            + ToCSharpStringLiteral(assemblyName.Version?.ToString() ?? "0.0.0.0")
            + ")]";
    }

    /// <summary>Escapes text for generated XML documentation comments.</summary>
    /// <param name="value">The text to escape.</param>
    /// <returns>The escaped XML documentation text.</returns>
    private static string ToXmlDocumentationText(string value)
    {
        var builder = new StringBuilder(value.Length);
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
        var registrations = new string[interfaces.Count * 2];
        var count = 0;
        for (var i = 0; i < interfaces.Count; i++)
        {
            var interfaceModel = interfaces[i];
            if (interfaceModel.ClassDeclaration.Contains("<"))
            {
                continue;
            }

            var generatedType = $"global::Refit.Implementation.Generated.{interfaceModel.Ns}{interfaceModel.ClassSuffix}";
            registrations[count++] = $$"""
                                    global::Refit.RestService.RegisterGeneratedFactory<{{interfaceModel.InterfaceDisplayName}}>(
                                        (client, requestBuilder) => new {{generatedType}}(client, requestBuilder));

                        """;

            if (CanUseGeneratedSettingsFactory(interfaceModel))
            {
                registrations[count++] = $$"""
                                        global::Refit.RestService.RegisterGeneratedSettingsFactory<{{interfaceModel.InterfaceDisplayName}}>(
                                            (client, settings) => new {{generatedType}}(client, settings));

                            """;
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
                parts[count++] = source;
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
    /// <returns>The generated method implementations.</returns>
    private static string BuildRefitMethods(
        ImmutableEquatableArray<MethodModel> methods,
        bool isTopLevel,
        InterfaceModel interfaceModel,
        UniqueNameBuilder uniqueNames,
        string requestBuilderFieldName,
        string settingsFieldName)
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
                settingsFieldName);
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
        var builder = new StringBuilder(value.Length + 2);
        _ = builder.Append('"');
        foreach (var c in value)
        {
            AppendEscapedCharacter(builder, c);
        }

        _ = builder.Append('"');
        return builder.ToString();
    }

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
        var length = prefix.Length + suffix.Length + ((parameters.Length - 1) * 2);
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

                    destination[position++] = '@';
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
        var length = prefix.Length + suffix.Length + ((constraints.Length - 1) * 2);
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
                    destination[position++] = ')';
                }

                AppendText(destination, suffix, ref position);
            });
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

        var length = (parameters.Count - 1) * 2;
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
                    destination[position++] = ')';
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

    /// <summary>Concatenates populated source fragments without allocating a trimmed array.</summary>
    /// <param name="parts">The source fragments.</param>
    /// <param name="count">The populated fragment count.</param>
    /// <returns>The concatenated source.</returns>
    private static string ConcatParts(string[] parts, int count)
    {
        var length = 0;
        for (var i = 0; i < count; i++)
        {
            length += parts[i].Length;
        }

        return CreateGeneratedString(
            length,
            (Parts: parts, Count: count),
            static (destination, state) =>
            {
                var position = 0;
                for (var i = 0; i < state.Count; i++)
                {
                    AppendText(destination, state.Parts[i], ref position);
                }
            });
    }

    /// <summary>Joins populated source fragments without allocating a trimmed array.</summary>
    /// <param name="parts">The source fragments.</param>
    /// <param name="count">The populated fragment count.</param>
    /// <param name="separator">The separator text.</param>
    /// <returns>The joined source.</returns>
    private static string JoinParts(string[] parts, int count, string separator)
    {
        if (count == 0)
        {
            return string.Empty;
        }

        var length = separator.Length * (count - 1);
        for (var i = 0; i < count; i++)
        {
            length += parts[i].Length;
        }

        return CreateGeneratedString(
            length,
            (Parts: parts, Count: count, Separator: separator),
            static (destination, state) =>
            {
                var position = 0;
                for (var i = 0; i < state.Count; i++)
                {
                    if (i > 0)
                    {
                        AppendText(destination, state.Separator, ref position);
                    }

                    AppendText(destination, state.Parts[i], ref position);
                }
            });
    }

    /// <summary>Builds a generated indentation string.</summary>
    /// <param name="level">The indentation level.</param>
    /// <returns>The generated indentation.</returns>
    private static string Indent(int level) => new(' ', level * CharsPerIndentation);

#if NETSTANDARD2_0
    /// <summary>Creates a generated string using a pre-sized buffer.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="length">The string length.</param>
    /// <param name="state">The caller supplied state.</param>
    /// <param name="action">The buffer fill callback.</param>
    /// <returns>The generated string.</returns>
    private static string CreateGeneratedString<TState>(
        int length,
        TState state,
        GeneratedStringAction<TState> action)
    {
        var destination = new char[length];
        action(destination, state);
        return new(destination);
    }

    /// <summary>Appends text into a generated string buffer.</summary>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="text">The text to append.</param>
    /// <param name="position">The current write position.</param>
    private static void AppendText(char[] destination, string text, ref int position)
    {
        text.CopyTo(0, destination, position, text.Length);
        position += text.Length;
    }
#else
    /// <summary>Creates a generated string using <see cref="string.Create{TState}(int, TState, System.Buffers.SpanAction{char, TState})"/>.</summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <param name="length">The string length.</param>
    /// <param name="state">The caller supplied state.</param>
    /// <param name="action">The buffer fill callback.</param>
    /// <returns>The generated string.</returns>
    private static string CreateGeneratedString<TState>(
        int length,
        TState state,
        GeneratedStringAction<TState> action) =>
        string.Create(
            length,
            (State: state, Action: action),
            static (destination, context) => context.Action(destination, context.State));

    /// <summary>Appends text into a generated string buffer.</summary>
    /// <param name="destination">The target character buffer.</param>
    /// <param name="text">The text to append.</param>
    /// <param name="position">The current write position.</param>
    private static void AppendText(Span<char> destination, string text, ref int position)
    {
        text.AsSpan().CopyTo(destination[position..]);
        position += text.Length;
    }
#endif
}
