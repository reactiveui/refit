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
    // * Search for all Interfaces, find the method definitions
    //   and make sure there's at least one Refit attribute on one
    // * Generate the data we need for the template based on interface method
    //   defn's

    /// <summary>
    /// InterfaceStubGeneratorV2.
    /// </summary>
    [Generator]
#if ROSLYN_4
    public class InterfaceStubGeneratorV2 : IIncrementalGenerator
#else
    public class InterfaceStubGenerator : ISourceGenerator
#endif
    {
        private const string TypeParameterVariableName = "______typeParameters";

#if !ROSLYN_4

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.RefitInternalNamespace",
                out var refitInternalNamespace
            );

            var parseStep = Parser.GenerateInterfaceStubs(
                (CSharpCompilation)context.Compilation,
                refitInternalNamespace,
                receiver.CandidateMethods.ToImmutableArray(),
                receiver.CandidateInterfaces.ToImmutableArray(),
                context.CancellationToken
            );

            // Emit diagnostics
            foreach (var diagnostic in parseStep.diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            // Emit interface stubs
            foreach (var interfaceModel in parseStep.contextGenerationSpec.Interfaces)
            {
                var interfaceText = Emitter.EmitInterface(interfaceModel);
                context.AddSource(
                    interfaceModel.FileName,
                    interfaceText
                );
            }

            // Emit PreserveAttribute and Generated.Initialize
            Emitter.EmitSharedCode(
                parseStep.contextGenerationSpec,
                (name, code) => context.AddSource(name, code)
            );
        }
#endif

#if ROSLYN_4

        /// <summary>
        /// Initializes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // We're looking for methods with an attribute that are in an interface
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

            // We also look for interfaces that derive from others, so we can see if any base methods contain
            // Refit methods
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

            // output the diagnostics
            // use `ImmutableEquatableArray` to cache cases where there are no diagnostics
            // otherwise the subsequent steps will always rerun.
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

#else

        /// <summary>
        /// Initializes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<MethodDeclarationSyntax> CandidateMethods { get; } = [];

            public List<InterfaceDeclarationSyntax> CandidateInterfaces { get; } = [];

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // We're looking for methods with an attribute that are in an interfaces
                if (
                    syntaxNode is MethodDeclarationSyntax methodDeclarationSyntax
                    && methodDeclarationSyntax.Parent is InterfaceDeclarationSyntax
                    && methodDeclarationSyntax.AttributeLists.Count > 0
                )
                {
                    CandidateMethods.Add(methodDeclarationSyntax);
                }

                // We also look for interfaces that derive from others, so we can see if any base methods contain
                // Refit methods
                if (syntaxNode is InterfaceDeclarationSyntax iface && iface.BaseList is not null)
                {
                    CandidateInterfaces.Add(iface);
                }
            }
        }

#endif
    }

    internal static class RefitGeneratorStepName
    {
        public const string ReportDiagnostics = "ReportDiagnostics";
        public const string BuildRefit = "BuildRefit";
    }
}
