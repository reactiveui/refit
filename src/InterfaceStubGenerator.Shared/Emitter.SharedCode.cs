// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

/// <summary>Emits the shared preserve attribute and factory-registration module for a compilation.</summary>
internal static partial class Emitter
{
    /// <summary>Emits the generated <c>PreserveAttribute</c> source into the consumer compilation.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <param name="generatedFileHeader">The shared generated file header.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    private static void EmitPreserveAttribute(
        ContextGenerationModel model,
        string generatedFileHeader,
        Action<string, SourceText> addSource)
    {
        const string attributeUsageLine =
            "[global::System.AttributeUsage (global::System.AttributeTargets.Class | "
            + "global::System.AttributeTargets.Struct | global::System.AttributeTargets.Enum | "
            + "global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Method | "
            + "global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | "
            + "global::System.AttributeTargets.Event | global::System.AttributeTargets.Interface | "
            + "global::System.AttributeTargets.Delegate)]";

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
    }

    /// <summary>Emits the generated factory-registration module initializer into the consumer compilation.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <param name="generatedFileHeader">The shared generated file header.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    private static void EmitGeneratedFactoryModule(
        ContextGenerationModel model,
        string generatedFileHeader,
        Action<string, SourceText> addSource)
    {
        const string dynamicDependencyLine =
            "[System.Diagnostics.CodeAnalysis.DynamicDependency("
            + "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, "
            + "typeof(global::Refit.Implementation.Generated))]";

        var generatedCodeAttribute = GeneratedCodeAttribute;
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
}
