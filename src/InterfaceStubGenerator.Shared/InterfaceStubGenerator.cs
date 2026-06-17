using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Refit.Generator
{
    /// <summary>
    /// InterfaceStubGenerator.
    /// </summary>
    [Generator]
    public class InterfaceStubGeneratorV2 : IIncrementalGenerator
    {
        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidateMethodsProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (syntax, cancellationToken) =>
                    syntax
                        is MethodDeclarationSyntax
                        {
                            Parent: InterfaceDeclarationSyntax,
                            AttributeLists.Count: > 0
                        },
                (context, cancellationToken) => (MethodDeclarationSyntax)context.Node
            );

            var candidateInterfacesProvider = context.SyntaxProvider.CreateSyntaxProvider(
                (syntax, cancellationToken) =>
                    syntax is InterfaceDeclarationSyntax { BaseList: not null },
                (context, cancellationToken) => (InterfaceDeclarationSyntax)context.Node
            );

            var refitInternalNamespace = context.AnalyzerConfigOptionsProvider.Select(
                (analyzerConfigOptionsProvider, cancellationToken) =>
                    analyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                        "build_property.RefitInternalNamespace",
                        out var refitInternalNamespace
                    )
                        ? refitInternalNamespace
                        : null
            );

            var inputs = candidateMethodsProvider
                .Collect()
                .Combine(candidateInterfacesProvider.Collect())
                .Select(
                    (combined, cancellationToken) =>
                        (candidateMethods: combined.Left, candidateInterfaces: combined.Right)
                )
                .Combine(refitInternalNamespace)
                .Combine(context.CompilationProvider)
                .Select(
                    (combined, cancellationToken) =>
                        (
                            combined.Left.Left.candidateMethods,
                            combined.Left.Left.candidateInterfaces,
                            refitInternalNamespace: combined.Left.Right,
                            compilation: combined.Right
                        )
                );

            var parseStep = inputs.Select(
                (collectedValues, cancellationToken) =>
                {
                    return Parser.GenerateInterfaceStubs(
                        (CSharpCompilation)collectedValues.compilation,
                        collectedValues.refitInternalNamespace,
                        collectedValues.candidateMethods,
                        collectedValues.candidateInterfaces,
                        cancellationToken
                    );
                }
            );

            var diagnostics = parseStep
                .Select(static (x, _) => x.diagnostics.ToImmutableEquatableArray())
                .WithTrackingName(RefitGeneratorStepName.ReportDiagnostics);
            context.ReportDiagnostics(diagnostics);

            var contextModel = parseStep.Select(static (x, _) => x.Item2);
            var interfaceModels = contextModel
                .SelectMany(static (x, _) => x.Interfaces)
                .WithTrackingName(RefitGeneratorStepName.BuildRefit);
            context.EmitSource(interfaceModels);

            context.RegisterImplementationSourceOutput(
                contextModel,
                static (spc, model) =>
                {
                    Emitter.EmitSharedCode(model, (name, code) => spc.AddSource(name, code));
                }
            );
        }
    }

    internal static class RefitGeneratorStepName
    {
        public const string ReportDiagnostics = "ReportDiagnostics";
        public const string BuildRefit = "BuildRefit";
    }
}
