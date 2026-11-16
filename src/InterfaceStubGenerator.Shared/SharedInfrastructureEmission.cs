// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator;

/// <summary>Emits the factory-registration module for a compilation.</summary>
internal static class SharedInfrastructureEmission
{
    /// <summary>Emits the generated factory-registration module initializer into the consumer compilation.</summary>
    /// <param name="model">The context generation model describing the interfaces.</param>
    /// <param name="generatedFileHeader">The shared generated file header.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    /// <param name="support">How the registration is hooked to assembly load in the consumer compilation.</param>
    /// <param name="needsDynamicDependencyPolyfill">Whether the consumer framework lacks the trimming attribute.</param>
    internal static void EmitGeneratedFactoryModule(
        ContextGenerationModel model,
        string generatedFileHeader,
        Action<string, SourceText> addSource,
        ModuleInitializerSupport support = ModuleInitializerSupport.Guarded,
        bool needsDynamicDependencyPolyfill = false)
    {
        // A compiler lowers [ModuleInitializer] into the module constructor on every target, so a target that lacks the
        // attribute only needs it declared. Without it, .NET Framework clients would never register.
        var guardStart = support == ModuleInitializerSupport.Guarded ? "#if NET5_0_OR_GREATER" : string.Empty;
        var guardEnd = support == ModuleInitializerSupport.Guarded ? "#endif" : string.Empty;
        var dependencies = Emitter.BuildGeneratedTrimmingDependencies(model.Interfaces);
        var generatedSource = $$"""
            {{generatedFileHeader}}
            namespace Refit.Implementation
            {
                /// <summary>Registers generated Refit factories for interfaces discovered at compile time.</summary>
                {{Emitter.GeneratedCodeAttribute}}
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.Diagnostics.DebuggerNonUserCode]
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
            {{dependencies}}        [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void Initialize()
                    {
            {{Emitter.BuildGeneratedFactoryRegistrations(model.Interfaces)}}        }
            {{guardEnd}}
                }
            }

            """;
        addSource("Generated.g.cs", Emitter.ToSourceText(generatedSource));

        if (needsDynamicDependencyPolyfill && dependencies.Length != 0)
        {
            EmitDynamicDependencyPolyfill(generatedFileHeader, addSource);
        }

        if (support == ModuleInitializerSupport.UnguardedWithPolyfill)
        {
            EmitModuleInitializerPolyfill(generatedFileHeader, addSource);
        }
    }

    /// <summary>Declares the trimming attribute for consumer frameworks that do not define it.</summary>
    /// <param name="generatedFileHeader">The shared generated file header.</param>
    /// <param name="addSource">Callback used to add generated source files.</param>
    internal static void EmitDynamicDependencyPolyfill(string generatedFileHeader, Action<string, SourceText> addSource)
    {
        var source = $$"""
            {{generatedFileHeader}}
            namespace System.Diagnostics.CodeAnalysis
            {
                /// <summary>Declares a member dependency understood by the .NET trimmer.</summary>
                {{Emitter.GeneratedCodeAttribute}}
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                [global::System.AttributeUsage(
                    global::System.AttributeTargets.Constructor | global::System.AttributeTargets.Field | global::System.AttributeTargets.Method,
                    AllowMultiple = true, Inherited = false)]
                internal sealed class DynamicDependencyAttribute : global::System.Attribute
                {
                    /// <summary>Initializes a dependency on the named members of a type.</summary>
                    /// <param name="memberSignature">The member name or documentation signature.</param>
                    /// <param name="type">The type declaring the members.</param>
                    public DynamicDependencyAttribute(string memberSignature, global::System.Type type)
                    {
                        MemberSignature = memberSignature;
                        Type = type;
                    }

                    /// <summary>Gets the member name or documentation signature.</summary>
                    public string MemberSignature { get; }

                    /// <summary>Gets the type declaring the members.</summary>
                    public global::System.Type Type { get; }
                }
            }

            """;
        addSource("DynamicDependencyAttribute.g.cs", Emitter.ToSourceText(source));
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
