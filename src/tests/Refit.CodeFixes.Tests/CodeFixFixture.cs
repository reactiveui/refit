// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Refit.Analyzers;

namespace Refit.CodeFixes.Tests;

/// <summary>Helpers for applying Refit code fixes to in-memory source.</summary>
[UnconditionalSuppressMessage(
    "SingleFile",
    "IL3000:Avoid accessing Assembly file path when publishing as a single file",
    Justification = "Compiles code-fix inputs against on-disk assemblies; never run as a single-file app.")]
internal static class CodeFixFixture
{
    /// <summary>The runtime data key containing framework assemblies available to the current test host.</summary>
    private const string TrustedPlatformAssemblies = "TRUSTED_PLATFORM_ASSEMBLIES";

    /// <summary>Applies the first available code fix for a diagnostic ID.</summary>
    /// <param name="source">The source to fix.</param>
    /// <param name="diagnosticId">The diagnostic ID to fix.</param>
    /// <returns>The updated source.</returns>
    public static async Task<string> ApplyFirstFix(string source, string diagnosticId)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace
            .AddProject("CodeFixTest", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default)
            .AddMetadataReferences(GetMetadataReferences());
        var document = project.AddDocument("Test.cs", SourceText.From(source));

        var compilation = await document.Project.GetCompilationAsync()
                          ?? throw new InvalidOperationException("Could not create compilation for code fix test.");

        var diagnostics = await compilation
            .WithAnalyzers([new RefitInterfaceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
        var diagnostic = diagnostics.SingleOrDefault(x => x.Id == diagnosticId)
                         ?? throw new InvalidOperationException(
                             "Expected diagnostic was not produced. Compiler diagnostics: "
                             + string.Join(", ", compilation.GetDiagnostics().Select(x => x.ToString()))
                             + ". Analyzer diagnostics: "
                             + string.Join(", ", diagnostics.Select(x => x.ToString())));

        var actions = new List<CodeAction>();
        var provider = new RefitInterfaceCodeFixProvider();
        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);

        if (actions.Count == 0)
        {
            throw new InvalidOperationException($"No code fix was registered for {diagnosticId}.");
        }

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
        var changedSolution = operations
            .OfType<ApplyChangesOperation>()
            .Single()
            .ChangedSolution;
        var changedDocument = changedSolution.GetDocument(document.Id)
                              ?? throw new InvalidOperationException("Code fix did not preserve the document.");
        var changedText = await changedDocument.GetTextAsync();
        return changedText.ToString();
    }

    /// <summary>Gets metadata references for code-fix test compilations.</summary>
    /// <returns>The metadata references.</returns>
    private static List<MetadataReference> GetMetadataReferences()
    {
        var referencePaths = new HashSet<string>(StringComparer.Ordinal);
        AddTrustedPlatformAssemblies(referencePaths);

        foreach (var assembly in GetAssemblyReferences())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                referencePaths.Add(assembly.Location);
            }
        }

        var references = new List<MetadataReference>(referencePaths.Count);
        foreach (var referencePath in referencePaths)
        {
            references.Add(MetadataReference.CreateFromFile(referencePath));
        }

        return references;
    }

    /// <summary>Gets the assemblies referenced when compiling code-fix test input.</summary>
    /// <returns>The distinct, non-dynamic assemblies to reference.</returns>
    private static Assembly[] GetAssemblyReferences() =>
    [
        .. AppDomain.CurrentDomain
            .GetAssemblies()
            .Concat(
            [
                typeof(Refit.GetAttribute).Assembly,
                typeof(Task).Assembly,
                typeof(Dictionary<string, string>).Assembly,
                typeof(IDisposable).Assembly
            ])
            .Distinct()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
    ];

    /// <summary>Adds runtime framework references used by Roslyn in-memory compilations.</summary>
    /// <param name="referencePaths">The reference path set to populate.</param>
    private static void AddTrustedPlatformAssemblies(HashSet<string> referencePaths)
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData(TrustedPlatformAssemblies);
        if (string.IsNullOrEmpty(trustedPlatformAssemblies))
        {
            return;
        }

        foreach (var referencePath in trustedPlatformAssemblies.Split(Path.PathSeparator))
        {
            if (!string.IsNullOrEmpty(referencePath))
            {
                referencePaths.Add(referencePath);
            }
        }
    }
}
