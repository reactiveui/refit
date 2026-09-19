// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias AnalyzerTooling;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using RefitInterfaceAnalyzer = AnalyzerTooling::Refit.Analyzers.RefitInterfaceAnalyzer;

namespace Refit.Documentation.Tooling;

/// <summary>Runs the shipped interface analyzer against compiler input.</summary>
internal static class AnalyzerSample
{
    /// <summary>A route that intentionally triggers the slash diagnostic.</summary>
    internal const string BrokenRoute = """
        internal interface IPeople
        {
            [Refit.Get(@"\people")]
            System.Threading.Tasks.Task<string> GetAsync();
        }
        """;

    /// <summary>Checks analyzer initialization and its supported descriptor list.</summary>
    /// <returns>The completion of the diagnostic check.</returns>
    internal static async Task RunAsync()
    {
        CSharpCompilation compilation = ToolingCompilation.Create(BrokenRoute);
        ToolingCompilation.RequireNoErrors(compilation);

        RefitInterfaceAnalyzer analyzer = new();
        const string routeDiagnostic = "RF003";
        ImmutableArray<DiagnosticDescriptor> supported = analyzer.SupportedDiagnostics;
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
        Console.WriteLine(Contains(diagnostics, routeDiagnostic)); // True

        const int supportedCount = 9;
        Check.Require(supported.Length == supportedCount, "Analyzer descriptor list must contain all nine supported IDs.");
        HashSet<string> expectedIds = new(comparer: StringComparer.Ordinal) { "RF001", routeDiagnostic, "RF004", "RF005", "RF006", "RF008", "RF009", "RF011", "RF012" };
        foreach (DiagnosticDescriptor descriptor in supported)
        {
            Check.Require(expectedIds.Remove(descriptor.Id), "Every supported descriptor has a distinct documented ID.");
            Check.Require(descriptor.DefaultSeverity == DiagnosticSeverity.Warning && descriptor.IsEnabledByDefault, "Supported diagnostics are warnings enabled by default.");
        }

        Check.Require(expectedIds.Count == 0, "All documented descriptors are exposed by the shipped analyzer.");
        Check.Require(Contains(diagnostics, routeDiagnostic), "Backslash route must produce RF003.");
    }

    /// <summary>Uses the analyzer host to collect Refit diagnostics.</summary>
    /// <param name="compilation">The source and references to check.</param>
    /// <returns>The analyzer's diagnostic messages.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Task<ImmutableArray<Diagnostic>> DiagnoseAsync(Compilation compilation) => compilation
        .WithAnalyzers([new RefitInterfaceAnalyzer()]).GetAnalyzerDiagnosticsAsync();

    /// <summary>Finds a diagnostic without hiding its identity in the demonstration.</summary>
    /// <param name="diagnostics">The analyzer's results.</param>
    /// <param name="id">The expected diagnostic ID.</param>
    /// <returns>Whether the analyzer reported the ID.</returns>
    internal static bool Contains(ImmutableArray<Diagnostic> diagnostics, string id)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Id == id)
            {
                return true;
            }
        }

        return false;
    }
}
