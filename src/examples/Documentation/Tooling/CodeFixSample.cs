// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Refit.CodeFixes;

namespace Refit.Documentation.Tooling;

/// <summary>Applies both supported code fixes and checks the corrected compilations.</summary>
internal static class CodeFixSample
{
    /// <summary>Exercises route and header-collection corrections.</summary>
    /// <returns>The completion of both correction checks.</returns>
    internal static async Task RunAsync()
    {
        await FixAsync(AnalyzerSample.BrokenRoute, "RF003");
        const string invalidHeaders = """
            internal interface IPeople
            {
                [Refit.Get("/people")]
                System.Threading.Tasks.Task<string> GetAsync([Refit.HeaderCollection] string headers);
            }
            """;
        await FixAsync(invalidHeaders, "RF005");
    }

    /// <summary>Registers a code action and applies its returned document edit.</summary>
    /// <param name="source">The intentionally incorrect C# interface.</param>
    /// <param name="id">The diagnostic that the correction must remove.</param>
    /// <returns>The completion of the correction check.</returns>
    /// <exception cref="InvalidOperationException">The host cannot obtain a compiler project or corrected document.</exception>
    private static async Task FixAsync(string source, string id)
    {
        using AdhocWorkspace workspace = new();
        ProjectInfo info = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "FixDemo",
            "FixDemo",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: ToolingCompilation.ParseOptions,
            metadataReferences: ToolingCompilation.References);
        Project project = workspace.AddProject(info);
        Document document = workspace.AddDocument(project.Id, "Api.cs", SourceText.From(source));
        Compilation compilation = await document.Project.GetCompilationAsync() ?? throw new InvalidOperationException("Missing compiler project.");
        ImmutableArray<Diagnostic> diagnostics = await AnalyzerSample.DiagnoseAsync(compilation);
        Diagnostic diagnostic = FindDiagnostic(diagnostics, id);

        RefitInterfaceCodeFixProvider provider = new();
        ImmutableArray<string> fixableIds = provider.FixableDiagnosticIds;
        FixAllProvider fixAll = provider.GetFixAllProvider();
        List<CodeAction> actions = [];
        CodeFixContext context = new(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);
        Check.Require(actions.Count > 0, "Registration offers a correction for the reported diagnostic.");
        ImmutableArray<CodeActionOperation> operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation change = FindChange(operations);
        Document fixedDocument = change.ChangedSolution.GetDocument(document.Id) ?? throw new InvalidOperationException("Missing corrected document.");
        Console.WriteLine(await fixedDocument.GetTextAsync());

        Check.Require(
            fixableIds.Length == 2 && fixableIds.Contains("RF003") && fixableIds.Contains("RF005") && fixAll is not null,
            "Code fix metadata names both supported corrections and supplies Fix All.");
        Compilation corrected = await fixedDocument.Project.GetCompilationAsync() ?? throw new InvalidOperationException("Missing corrected compilation.");
        ToolingCompilation.RequireNoErrors(corrected);
        ImmutableArray<Diagnostic> remaining = await AnalyzerSample.DiagnoseAsync(corrected);
        Check.Require(!AnalyzerSample.Contains(remaining, id), "The correction must remove its diagnostic.");
    }

    /// <summary>Fails when the correction's input diagnostic is absent.</summary>
    /// <param name="diagnostics">The analyzer's results.</param>
    /// <param name="id">The required diagnostic ID.</param>
    /// <returns>The diagnostic passed to the code-fix provider.</returns>
    /// <exception cref="InvalidOperationException">The analyzer did not report the expected diagnostic.</exception>
    private static Diagnostic FindDiagnostic(ImmutableArray<Diagnostic> diagnostics, string id)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Id == id)
            {
                return diagnostic;
            }
        }

        throw new InvalidOperationException("Expected diagnostic was not reported.");
    }

    /// <summary>Finds the updated solution supplied by a code action.</summary>
    /// <param name="operations">The action's returned operations.</param>
    /// <returns>The document-edit operation.</returns>
    /// <exception cref="InvalidOperationException">The action did not supply a document edit.</exception>
    private static ApplyChangesOperation FindChange(ImmutableArray<CodeActionOperation> operations)
    {
        foreach (CodeActionOperation operation in operations)
        {
            if (operation is ApplyChangesOperation change)
            {
                return change;
            }
        }

        throw new InvalidOperationException("The action must supply a document correction.");
    }
}
