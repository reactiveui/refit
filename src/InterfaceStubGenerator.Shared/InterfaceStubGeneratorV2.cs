// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

/// <summary>An incremental source generator that produces Refit interface stub implementations.</summary>
[Generator]
public class InterfaceStubGeneratorV2 : IIncrementalGenerator
{
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

        var refitInternalNamespace =
            context.AnalyzerConfigOptionsProvider.Select((analyzerConfigOptionsProvider, _) =>
                analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    "build_property.RefitInternalNamespace",
                    out var refitInternalNamespace)
                    ? refitInternalNamespace
                    : null);

        var generatedRequestBuilding =
            context.AnalyzerConfigOptionsProvider.Select((analyzerConfigOptionsProvider, _) =>
                analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    "build_property.RefitGeneratedRequestBuilding",
                    out var propertyValue)
                && bool.TryParse(propertyValue, out var parsed)
                && parsed);

        var candidateSyntax = candidateMethodsProvider
            .Collect()
            .Combine(candidateInterfacesProvider.Collect())
            .Select(static (combined, _) => CreateCandidateSyntax(combined));

        var generatorOptions = refitInternalNamespace
            .Combine(generatedRequestBuilding)
            .Select(static (combined, _) => CreateGeneratorOptions(combined));

        var syntaxAndOptions = candidateSyntax
            .Combine(generatorOptions)
            .Select(static (combined, _) => CreateSyntaxAndOptions(combined));

        var inputs = syntaxAndOptions
            .Combine(context.CompilationProvider)
            .Select(static (combined, _) => CreateGeneratorInputs(combined));

        var parseStep = inputs.Select((collectedValues, cancellationToken) =>
            Parser.GenerateInterfaceStubs(
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
    /// <param name="combined">The combined analyzer-config options.</param>
    /// <returns>The named generator options input.</returns>
    private static GeneratorOptions CreateGeneratorOptions(
        (string? RefitInternalNamespace, bool GeneratedRequestBuilding) combined) =>
        new(combined.RefitInternalNamespace, combined.GeneratedRequestBuilding);

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
            combined.Compilation);

    /// <summary>The collected syntax candidates for one generator pass.</summary>
    /// <param name="CandidateMethods">The candidate method declarations.</param>
    /// <param name="CandidateInterfaces">The candidate interface declarations.</param>
    private readonly record struct CandidateSyntax(
        ImmutableArray<MethodDeclarationSyntax> CandidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> CandidateInterfaces);

    /// <summary>The generator options visible through analyzer config.</summary>
    /// <param name="RefitInternalNamespace">The optional Refit internal namespace prefix.</param>
    /// <param name="GeneratedRequestBuilding">Whether inline request construction is enabled.</param>
    private readonly record struct GeneratorOptions(
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding);

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
    /// <param name="Compilation">The current compilation.</param>
    private readonly record struct GeneratorInputs(
        ImmutableArray<MethodDeclarationSyntax> CandidateMethods,
        ImmutableArray<InterfaceDeclarationSyntax> CandidateInterfaces,
        string? RefitInternalNamespace,
        bool GeneratedRequestBuilding,
        Compilation Compilation);
}
