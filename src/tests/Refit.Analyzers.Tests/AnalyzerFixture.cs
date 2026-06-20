// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refit.Analyzers.Tests;

/// <summary>Helpers for running Refit analyzers against in-memory source.</summary>
[UnconditionalSuppressMessage(
    "SingleFile",
    "IL3000:Avoid accessing Assembly file path when publishing as a single file",
    Justification = "Compiles analyzer inputs against on-disk assemblies; never run as a single-file app.")]
internal static class AnalyzerFixture
{
    /// <summary>The runtime data key containing framework assemblies available to the current test host.</summary>
    private const string TrustedPlatformAssemblies = "TRUSTED_PLATFORM_ASSEMBLIES";

    /// <summary>Runs the Refit interface analyzer over an interface body snippet.</summary>
    /// <param name="body">The interface body source.</param>
    /// <returns>The diagnostics produced by the analyzer.</returns>
    public static Task<ImmutableArray<Diagnostic>> RunForBody(string body) =>
        Run(BuildBodySource(body));

    /// <summary>Runs the Refit interface analyzer over a complete source string.</summary>
    /// <param name="source">The source to analyze.</param>
    /// <returns>The diagnostics produced by the analyzer.</returns>
    public static Task<ImmutableArray<Diagnostic>> Run(string source)
    {
        var compilation = CreateLibrary(source);
        var analyzer = new RefitInterfaceAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [analyzer]);
        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    /// <summary>Creates a compilation for analyzer tests.</summary>
    /// <param name="source">The source to compile.</param>
    /// <returns>The created compilation.</returns>
    private static CSharpCompilation CreateLibrary(string source)
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

        return CSharpCompilation.Create(
            "compilation",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Gets the assemblies referenced when compiling analyzer test input.</summary>
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

    /// <summary>Builds a source file containing a Refit interface body.</summary>
    /// <param name="body">The interface body source.</param>
    /// <returns>The complete source file.</returns>
    private static string BuildBodySource(string body) =>
        $$"""
          using System;
          using System.Collections.Generic;
          using System.Threading;
          using System.Threading.Tasks;
          using Refit;

          namespace RefitAnalyzerTest;

          public interface IGeneratedClient
          {
          {{body}}
          }
          """;
}
