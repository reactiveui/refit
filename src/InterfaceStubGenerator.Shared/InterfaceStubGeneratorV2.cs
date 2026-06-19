// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refit.Generator;

/// <summary>An incremental source generator that produces Refit interface stub implementations.</summary>
[Generator]
public class InterfaceStubGeneratorV2 : IIncrementalGenerator
{
    /// <summary>The MSBuild property prefix used by analyzer config options.</summary>
    private const string BuildPropertyPrefix = "build_property.";

    /// <summary>The option that disables all Refit source generation.</summary>
    private const string DisableRefitSourceGeneratorOption = "DisableRefitSourceGenerator";

    /// <summary>The option that enables direct generated request construction.</summary>
    private const string RefitGeneratedRequestBuildingOption = "RefitGeneratedRequestBuilding";

    /// <summary>The option that overrides the generated internal namespace prefix.</summary>
    private const string RefitInternalNamespaceOption = "RefitInternalNamespace";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateMethodsProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (syntax, _) =>
                syntax
                    is MethodDeclarationSyntax
                    {
                        Parent: InterfaceDeclarationSyntax,
                        AttributeLists.Count: > 0
                    },
            (generatorContext, _) => (MethodDeclarationSyntax)generatorContext.Node);

        var candidateInterfacesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (syntax, _) =>
                syntax is InterfaceDeclarationSyntax { BaseList: not null },
            (generatorContext, _) => (InterfaceDeclarationSyntax)generatorContext.Node);

        var generatorOptions =
            context.AnalyzerConfigOptionsProvider.Select(
                static (analyzerConfigOptionsProvider, _) =>
                    CreateGeneratorOptions(analyzerConfigOptionsProvider.GlobalOptions));

        var candidateSyntax = candidateMethodsProvider
            .Collect()
            .Combine(candidateInterfacesProvider.Collect())
            .Select(static (combined, _) => CreateCandidateSyntax(combined));

        var syntaxAndOptions = candidateSyntax
            .Combine(generatorOptions)
            .Select(static (combined, _) => CreateSyntaxAndOptions(combined));

        var inputs = syntaxAndOptions
            .Combine(context.CompilationProvider)
            .Select(static (combined, _) => CreateGeneratorInputs(combined));

        var parseStep = inputs.Select(
            static (collectedValues, cancellationToken) =>
                collectedValues.DisableSourceGenerator
                    ? CreateDisabledGenerationResult()
                    : Parser.GenerateInterfaceStubs(
                        (CSharpCompilation)collectedValues.Compilation,
                        collectedValues.RefitInternalNamespace,
                        collectedValues.GeneratedRequestBuilding,
                        collectedValues.CandidateMethods,
                        collectedValues.CandidateInterfaces,
                        cancellationToken));

        var diagnostics = parseStep
            .Select(static (x, _) => x.diagnostics.ToImmutableEquatableArray())
            .WithTrackingName(RefitGeneratorStepName.ReportDiagnostics);
        context.ReportDiagnostics(diagnostics);

        var contextModel = parseStep.Select(static (x, _) => x.contextGenerationSpec);
        var interfaceModels = contextModel
            .SelectMany(static (x, _) => x.Interfaces)
            .WithTrackingName(RefitGeneratorStepName.BuildRefit);
        context.EmitSource(interfaceModels);

        context.RegisterImplementationSourceOutput(
            contextModel,
            static (spc, model) => Emitter.EmitSharedCode(model, spc.AddSource));
    }

    /// <summary>Creates the collected candidate syntax input.</summary>
    /// <param name="combined">The combined method and interface candidate collections.</param>
    /// <returns>The named candidate syntax input.</returns>
    private static CandidateSyntax CreateCandidateSyntax(
        (ImmutableArray<MethodDeclarationSyntax> Methods, ImmutableArray<InterfaceDeclarationSyntax> Interfaces) combined) =>
        new(combined.Methods, combined.Interfaces);

    /// <summary>Creates the generator options input.</summary>
    /// <param name="options">The global analyzer-config options.</param>
    /// <returns>The named generator options input.</returns>
    private static GeneratorOptions CreateGeneratorOptions(AnalyzerConfigOptions options)
    {
        TryGetGlobalOption(options, RefitInternalNamespaceOption, out var refitInternalNamespace);

        return new(
            refitInternalNamespace,
            GetBooleanOption(options, RefitGeneratedRequestBuildingOption, defaultValue: true),
            GetBooleanOption(options, DisableRefitSourceGeneratorOption, defaultValue: false));
    }

    /// <summary>Creates the combined syntax and options input.</summary>
    /// <param name="combined">The combined candidate syntax and generator options.</param>
    /// <returns>The named syntax-and-options input.</returns>
    private static SyntaxAndOptions CreateSyntaxAndOptions(
        (CandidateSyntax CandidateSyntax, GeneratorOptions Options) combined) =>
        new(combined.CandidateSyntax, combined.Options);

    /// <summary>Creates the final parser input.</summary>
    /// <param name="combined">The combined syntax/options input and compilation.</param>
    /// <returns>The named parser input.</returns>
    private static GeneratorInputs CreateGeneratorInputs(
        (SyntaxAndOptions SyntaxAndOptions, Compilation Compilation) combined) =>
        new(
            combined.SyntaxAndOptions.CandidateSyntax.CandidateMethods,
            combined.SyntaxAndOptions.CandidateSyntax.CandidateInterfaces,
            combined.SyntaxAndOptions.Options.RefitInternalNamespace,
            combined.SyntaxAndOptions.Options.GeneratedRequestBuilding,
            combined.SyntaxAndOptions.Options.DisableSourceGenerator,
            combined.Compilation);

    /// <summary>Creates an empty result when generation is explicitly disabled.</summary>
    /// <returns>The disabled generation result.</returns>
    private static (List<Diagnostic> diagnostics, ContextGenerationModel contextGenerationSpec)
        CreateDisabledGenerationResult() =>
        new(
            [],
            new(
                string.Empty,
                string.Empty,
                false,
                ImmutableEquatableArrayFactory.Empty<InterfaceModel>()));

    /// <summary>Reads a boolean analyzer-config option.</summary>
    /// <param name="options">The analyzer-config options.</param>
    /// <param name="name">The option name without the build-property prefix.</param>
    /// <param name="defaultValue">The value to use when the option is not present or cannot be parsed.</param>
    /// <returns>The parsed option value.</returns>
    private static bool GetBooleanOption(
        AnalyzerConfigOptions options,
        string name,
        bool defaultValue) =>
        TryGetGlobalOption(options, name, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;

    /// <summary>Reads a global analyzer-config option by bare name or MSBuild build-property name.</summary>
    /// <param name="options">The analyzer-config options.</param>
    /// <param name="name">The option name without the build-property prefix.</param>
    /// <param name="value">The option value when found.</param>
    /// <returns><see langword="true"/> when an option value was found.</returns>
    private static bool TryGetGlobalOption(
        AnalyzerConfigOptions options,
        string name,
        out string? value)
    {
        if (options.TryGetValue(BuildPropertyPrefix + name, out var buildPropertyValue))
        {
            value = buildPropertyValue;
            return true;
        }

        if (options.TryGetValue(name, out var analyzerConfigValue))
        {
            value = analyzerConfigValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>The collected syntax candidates for one generator pass.</summary>
    /// <param name="CandidateMethods">The candidate method declarations.</param>
    /// <param name="CandidateInterfaces">The candidate interface declarations.</param>
    private readonly record struct CandidateSyntax(
        ImmutableArray<MethodDeclarationSyntax> CandidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> CandidateInterfaces);

    /// <summary>The generator options visible through analyzer config.</summary>
    /// <param name="RefitInternalNamespace">The optional Refit internal namespace prefix.</param>
    /// <param name="GeneratedRequestBuilding">Whether inline request construction is enabled.</param>
    /// <param name="DisableSourceGenerator">Whether source generation is disabled.</param>
    private readonly record struct GeneratorOptions(
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding,
        bool DisableSourceGenerator);

    /// <summary>The collected syntax candidates plus generator options.</summary>
    /// <param name="CandidateSyntax">The candidate syntax input.</param>
    /// <param name="Options">The generator options input.</param>
    private readonly record struct SyntaxAndOptions(
        CandidateSyntax CandidateSyntax,
        GeneratorOptions Options);

    /// <summary>The full input consumed by the parser.</summary>
    /// <param name="CandidateMethods">The candidate method declarations.</param>
    /// <param name="CandidateInterfaces">The candidate interface declarations.</param>
    /// <param name="RefitInternalNamespace">The optional Refit internal namespace prefix.</param>
    /// <param name="GeneratedRequestBuilding">Whether inline request construction is enabled.</param>
    /// <param name="DisableSourceGenerator">Whether source generation is disabled.</param>
    /// <param name="Compilation">The current compilation.</param>
    private readonly record struct GeneratorInputs(
        ImmutableArray<MethodDeclarationSyntax> CandidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> CandidateInterfaces,
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding,
        bool DisableSourceGenerator,
        Compilation Compilation);
}
