// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

/// <summary>Emits the shared preserve attribute and factory-registration module for a compilation.</summary>
internal static class SharedInfrastructureEmission
{
    /// <summary>Emits the generated <c>PreserveAttribute</c> source into the consumer compilation.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <param name="generatedFileHeader">The shared generated file header.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    internal static void EmitPreserveAttribute(
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
        var attributeText = $$"""
            {{generatedFileHeader}}
            namespace {{model.RefitInternalNamespace}}
            {
                /// <summary>Identifies generated members that should be preserved by tools that honor this attribute.</summary>
                {{Emitter.GeneratedCodeAttribute}}
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
    /// <param name="support">How the registration is hooked to assembly load in the consumer compilation.</param>
    internal static void EmitGeneratedFactoryModule(
        ContextGenerationModel model,
        string generatedFileHeader,
        Action<string, SourceText> addSource,
        ModuleInitializerSupport support = ModuleInitializerSupport.Guarded)
    {
        // A compiler lowers [ModuleInitializer] into the module constructor on every target, so a target that lacks the
        // attribute only needs it declared. Without it, .NET Framework clients would never register.
        var guardStart = support == ModuleInitializerSupport.Guarded ? "#if NET5_0_OR_GREATER" : string.Empty;
        var guardEnd = support == ModuleInitializerSupport.Guarded ? "#endif" : string.Empty;
        var generatedSource = $$"""
            {{generatedFileHeader}}
            namespace Refit.Implementation
            {
                /// <summary>Registers generated Refit factories for interfaces discovered at compile time.</summary>
                {{Emitter.GeneratedCodeAttribute}}
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.Diagnostics.DebuggerNonUserCode]
                [{{model.PreserveAttributeDisplayName}}]
                [global::System.Reflection.Obfuscation(Exclude=true)]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static partial class {{model.GeneratedClassName}}
                {
            {{guardStart}}
                    /// <summary>Registers generated Refit factories when the assembly is loaded.</summary>
                    [global::System.Diagnostics.CodeAnalysis.SuppressMessage(
                        "Usage",
                        "CA2255:The ModuleInitializer attribute should not be used in libraries",
                        Justification = "ModuleInitializer is used intentionally so generated Refit factories are registered when the assembly loads.")]
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void Initialize()
                    {
            {{Emitter.BuildGeneratedFactoryRegistrations(model.Interfaces)}}        }
            {{guardEnd}}
                }
            }

            """;
        addSource("Generated.g.cs", Emitter.ToSourceText(generatedSource));

        if (support == ModuleInitializerSupport.UnguardedWithPolyfill)
        {
            EmitModuleInitializerPolyfill(generatedFileHeader, addSource);
        }
    }

    /// <summary>Declares <c>ModuleInitializerAttribute</c> for targets whose framework does not define it.</summary>
    /// <param name="generatedFileHeader">The shared generated file header.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    internal static void EmitModuleInitializerPolyfill(
        string generatedFileHeader,
        Action<string, SourceText> addSource)
    {
        var polyfillSource = $$"""
            {{generatedFileHeader}}
            #if !NET5_0_OR_GREATER
            namespace System.Runtime.CompilerServices
            {
                /// <summary>Marks a method the compiler runs when the module loads.</summary>
                {{Emitter.GeneratedCodeAttribute}}
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
                internal sealed class ModuleInitializerAttribute : global::System.Attribute
                {
                }
            }
            #endif

            """;
        addSource("ModuleInitializerAttribute.g.cs", Emitter.ToSourceText(polyfillSource));
    }
}
