// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Documentation.Tooling;

/// <summary>Builds compilations with runtime and Refit metadata references.</summary>
internal static class ToolingCompilation
{
    /// <summary>Gets compiler references excluding the host's aliased tooling assemblies.</summary>
    internal static ImmutableArray<MetadataReference> References { get; } = CreateReferences();

    /// <summary>Gets the C# 14 options used by the sample host.</summary>
    internal static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.CSharp14);

    /// <summary>Creates a library compilation for a demonstrated interface.</summary>
    /// <param name="source">The C# input text.</param>
    /// <returns>The compiler model before generation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CSharpCompilation Create(string source) => CSharpCompilation.Create(
        "ToolingDemonstration",
        [CSharpSyntaxTree.ParseText(source, ParseOptions)],
        References,
        new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    /// <summary>Fails with the actual compiler errors when generation or a correction is invalid.</summary>
    /// <param name="compilation">The compiler result to inspect.</param>
    internal static void RequireNoErrors(Compilation compilation)
    {
        List<string> errors = [];
        foreach (Diagnostic diagnostic in compilation.GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                errors.Add(diagnostic.ToString());
            }
        }

        Check.Require(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    /// <summary>Reads shared runtime paths and the copied Refit assembly.</summary>
    /// <returns>The metadata references for this compiler host.</returns>
    /// <exception cref="InvalidOperationException">The host has no ordinary runtime metadata paths.</exception>
    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("Run this compiler example with the .NET desktop runtime.");
        List<MetadataReference> references = [];
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator))
        {
            if (!path.StartsWith(AppContext.BaseDirectory, StringComparison.Ordinal))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        references.Add(MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "Refit.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "Refit.Xml.dll")));
        return [.. references];
    }
}
