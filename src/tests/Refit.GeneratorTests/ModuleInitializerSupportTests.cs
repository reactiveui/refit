// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies how the generated factory registration is hooked to assembly load.</summary>
public sealed class ModuleInitializerSupportTests
{
    /// <summary>The generated factory module's hint name.</summary>
    private const string FactoryModuleHintName = "Generated.g.cs";

    /// <summary>The generated polyfill's hint name.</summary>
    private const string PolyfillHintName = "ModuleInitializerAttribute.g.cs";

    /// <summary>The framework check the guarded registration carries.</summary>
    private const string FrameworkCheck = "#if NET5_0_OR_GREATER";

    /// <summary>Verifies a compilation that defines the attribute needs no polyfill.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CompilationWithTheAttributeIsUnguarded()
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("namespace Sample; public sealed class Marker { }"));

        await Assert.That(InterfaceStubGeneratorV2.DetectModuleInitializerSupport(compilation)).IsEqualTo(ModuleInitializerSupport.Unguarded);
    }

    /// <summary>Verifies a compilation without the attribute asks for the polyfill.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CompilationWithoutTheAttributeNeedsThePolyfill()
    {
        var compilation = CSharpCompilation.Create(
            "NoReferences",
            [CSharpSyntaxTree.ParseText("namespace Sample { public sealed class Marker { } }")]);

        await Assert.That(InterfaceStubGeneratorV2.DetectModuleInitializerSupport(compilation)).IsEqualTo(ModuleInitializerSupport.UnguardedWithPolyfill);
    }

    /// <summary>Verifies a language older than C# 9 keeps the registration guarded.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task OldLanguageVersionStaysGuarded()
    {
        var tree = CSharpSyntaxTree.ParseText(
            "namespace Sample { public sealed class Marker { } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));
        var compilation = CSharpCompilation.Create("OldLanguage", [tree]);

        await Assert.That(InterfaceStubGeneratorV2.DetectModuleInitializerSupport(compilation)).IsEqualTo(ModuleInitializerSupport.Guarded);
    }

    /// <summary>Verifies the guarded registration keeps the framework check and declares no polyfill.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task GuardedModuleKeepsTheFrameworkCheck()
    {
        var sources = Emit(ModuleInitializerSupport.Guarded);

        await Assert.That(sources[FactoryModuleHintName]).Contains(FrameworkCheck);
        await Assert.That(sources.ContainsKey(PolyfillHintName)).IsFalse();
    }

    /// <summary>Verifies the unguarded registration drops the framework check.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task UnguardedModuleDropsTheFrameworkCheck()
    {
        var sources = Emit(ModuleInitializerSupport.Unguarded);

        await Assert.That(sources[FactoryModuleHintName]).DoesNotContain(FrameworkCheck);
        await Assert.That(sources[FactoryModuleHintName]).Contains("ModuleInitializer");
        await Assert.That(sources.ContainsKey(PolyfillHintName)).IsFalse();
    }

    /// <summary>Verifies the polyfill is declared only for targets that lack the attribute.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task PolyfillModuleDeclaresTheAttributeForOlderTargets()
    {
        var sources = Emit(ModuleInitializerSupport.UnguardedWithPolyfill);

        await Assert.That(sources[FactoryModuleHintName]).DoesNotContain(FrameworkCheck);
        await Assert.That(sources[PolyfillHintName]).Contains("#if !NET5_0_OR_GREATER");
        await Assert.That(sources[PolyfillHintName]).Contains("class ModuleInitializerAttribute");
    }

    /// <summary>Emits the factory module for an empty set of interfaces.</summary>
    /// <param name="support">The module initializer support to emit for.</param>
    /// <returns>The emitted sources by hint name.</returns>
    private static Dictionary<string, string> Emit(ModuleInitializerSupport support)
    {
        var model = new ContextGenerationModel("Consumer", "global::Preserve", "Generated", true, true, []);
        Dictionary<string, string> sources = [];

        SharedInfrastructureEmission.EmitGeneratedFactoryModule(
            model,
            "// header",
            (name, text) => sources[name] = text.ToString(),
            support);

        return sources;
    }
}
