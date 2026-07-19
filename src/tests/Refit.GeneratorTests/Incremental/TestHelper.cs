// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refit.Generator;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Helper methods for driving the Refit generator in incremental tracking tests.</summary>
internal static class TestHelper
{
    /// <summary>The driver options that enable incremental step tracking.</summary>
    private static readonly GeneratorDriverOptions _enableIncrementalTrackingDriverOptions =
        new(IncrementalGeneratorOutputKind.None, true);

    /// <summary>Runs the generator against a compilation with incremental tracking enabled.</summary>
    /// <param name="compilation">The compilation to run the generator against.</param>
    /// <returns>The generator driver after running.</returns>
    internal static GeneratorDriver GenerateTracked(Compilation compilation)
    {
        var generator = new InterfaceStubGeneratorV2();

        var driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: _enableIncrementalTrackingDriverOptions);
        return driver.RunGenerators(compilation);
    }

    /// <summary>Replaces a type declaration in the compilation with new source.</summary>
    /// <param name="compilation">The compilation to modify.</param>
    /// <param name="memberName">The name of the type declaration to replace.</param>
    /// <param name="newMember">The replacement type declaration source.</param>
    /// <returns>The updated compilation.</returns>
    internal static CSharpCompilation ReplaceMemberDeclaration(
        CSharpCompilation compilation,
        string memberName,
        string newMember)
    {
        var syntaxTree = compilation.SyntaxTrees.Single();
        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Single(x => x.Identifier.Text == memberName);
        var updatedMemberDeclaration = SyntaxFactory.ParseMemberDeclaration(newMember)!;

        var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(memberDeclaration, updatedMemberDeclaration);
        var newTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

        return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees[0], newTree);
    }

    /// <summary>Replaces a named type declaration inside one syntax tree of a multi-tree compilation.</summary>
    /// <param name="compilation">The multi-tree compilation to modify.</param>
    /// <param name="tree">The syntax tree that contains the type declaration to replace.</param>
    /// <param name="typeName">The name of the type declaration to replace.</param>
    /// <param name="newDeclaration">The replacement type declaration source.</param>
    /// <returns>The updated compilation with the other syntax trees left untouched.</returns>
    internal static CSharpCompilation ReplaceTypeDeclarationInTree(
        CSharpCompilation compilation,
        SyntaxTree tree,
        string typeName,
        string newDeclaration)
    {
        var root = tree.GetCompilationUnitRoot();
        var typeDeclaration = root
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Single(x => x.Identifier.Text == typeName);
        var updatedTypeDeclaration = SyntaxFactory.ParseMemberDeclaration(newDeclaration)!;

        var newRoot = root.ReplaceNode(typeDeclaration, updatedTypeDeclaration);
        var newTree = tree.WithRootAndOptions(newRoot, tree.Options);

        return compilation.ReplaceSyntaxTree(tree, newTree);
    }

    /// <summary>Replaces a local variable declaration in the compilation with new source.</summary>
    /// <param name="compilation">The compilation to modify.</param>
    /// <param name="variableName">The name of the local variable to replace.</param>
    /// <param name="newDeclaration">The replacement declaration statement source.</param>
    /// <returns>The updated compilation.</returns>
    internal static CSharpCompilation ReplaceLocalDeclaration(
        CSharpCompilation compilation,
        string variableName,
        string newDeclaration)
    {
        var syntaxTree = compilation.SyntaxTrees.Single();

        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<LocalDeclarationStatementSyntax>()
            .Single(x => x.Declaration.Variables.Any(x => x.Identifier.ToString() == variableName));
        var updatedMemberDeclaration = SyntaxFactory.ParseStatement(newDeclaration)!;

        var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(memberDeclaration, updatedMemberDeclaration);
        var newTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

        return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees[0], newTree);
    }

    /// <summary>Asserts that the generator steps ran for the expected reasons.</summary>
    /// <param name="driver">The generator driver to inspect.</param>
    /// <param name="reasons">The expected run reasons for each tracked step.</param>
    /// <param name="outputIndex">The index of the output to inspect.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0)
    {
        var runResult = driver.GetRunResult().Results[0];

        await AssertRunReason(
            runResult,
            RefitGeneratorStepName.ReportDiagnostics,
            reasons.ReportDiagnosticsStep,
            outputIndex);
        await AssertRunReason(runResult, RefitGeneratorStepName.BuildRefit, reasons.BuildRefitStep, outputIndex);
    }

    /// <summary>Asserts the build-Refit run reason for the interface with the given simple name.</summary>
    /// <remarks>
    /// The build-Refit step emits one output per interface, so a per-interface assertion is required when a
    /// compilation contains more than one interface. The output is located by matching the interface's display
    /// name rather than a positional index, because the emission order follows the parser's collection order.
    /// </remarks>
    /// <param name="driver">The generator driver to inspect.</param>
    /// <param name="interfaceName">The simple (unqualified) interface name whose output is inspected.</param>
    /// <param name="expectedReason">The expected build-Refit run reason for that interface.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal static async Task AssertBuildRefitReasonForInterface(
        GeneratorDriver driver,
        string interfaceName,
        IncrementalStepRunReason expectedReason)
    {
        var qualifiedSuffix = "." + interfaceName;
        var interfaceOutput = driver
            .GetRunResult()
            .Results[0]
            .TrackedSteps[RefitGeneratorStepName.BuildRefit]
            .SelectMany(static step => step.Outputs)
            .Single(output => output.Value is InterfaceModel model
                && model.InterfaceDisplayName.EndsWith(qualifiedSuffix, StringComparison.Ordinal));

        await Assert.That(interfaceOutput.Reason).IsEqualTo(expectedReason);
    }

    /// <summary>Asserts that a single tracked step ran for the expected reason.</summary>
    /// <param name="runResult">The generator run result to inspect.</param>
    /// <param name="stepName">The name of the tracked step.</param>
    /// <param name="expectedStepReason">The expected run reason for the step.</param>
    /// <param name="outputIndex">The index of the output to inspect.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task AssertRunReason(
        GeneratorRunResult runResult,
        string stepName,
        IncrementalStepRunReason expectedStepReason,
        int outputIndex)
    {
        var actualStepReason = runResult
            .TrackedSteps[stepName]
            .SelectMany(static x => x.Outputs)
            .ElementAt(outputIndex)
            .Reason;
        await Assert.That(actualStepReason).IsEqualTo(expectedStepReason);
    }
}
