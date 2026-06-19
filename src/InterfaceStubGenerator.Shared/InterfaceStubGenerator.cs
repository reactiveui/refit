// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator;

/// <summary>An incremental source generator that produces Refit interface stub implementations.</summary>
[Generator]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Documentation",
    "SST1649:The file name should match the first type name",
    Justification = "File name retained for compatibility with the shared project (.projitems) and public docs.")]
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
            (context, _) => (MethodDeclarationSyntax)context.Node);

        var candidateInterfacesProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (syntax, _) =>
                syntax is InterfaceDeclarationSyntax { BaseList: not null },
            (context, _) => (InterfaceDeclarationSyntax)context.Node);

        var refitInternalNamespace =
            context.AnalyzerConfigOptionsProvider.Select((analyzerConfigOptionsProvider, _) =>
                analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    "build_property.RefitInternalNamespace",
                    out var refitInternalNamespace)
                    ? refitInternalNamespace
                    : null);

        var inputs = candidateMethodsProvider
            .Collect()
            .Combine(candidateInterfacesProvider.Collect())
            .Select((combined, _) =>
                (candidateMethods: combined.Left, candidateInterfaces: combined.Right))
            .Combine(refitInternalNamespace)
            .Combine(context.CompilationProvider)
            .Select((combined, _) =>
                (
                    combined.Left.Left.candidateMethods,
                    combined.Left.Left.candidateInterfaces,
                    refitInternalNamespace: combined.Left.Right,
                    compilation: combined.Right
                ));

        var parseStep = inputs.Select((collectedValues, cancellationToken) =>
            {
                return Parser.GenerateInterfaceStubs(
                    (CSharpCompilation)collectedValues.compilation,
                    collectedValues.refitInternalNamespace,
                    collectedValues.candidateMethods,
                    collectedValues.candidateInterfaces,
                    cancellationToken);
            });

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
            static (spc, model) => Emitter.EmitSharedCode(model, (name, code) => spc.AddSource(name, code)));
    }
}
