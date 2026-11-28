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

    /// <summary>Runs an example of every diagnostic against an in-memory interface.</summary>
    /// <returns>The completion of the diagnostic check.</returns>
    internal static async Task RunAsync()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Text.Json.Serialization.Metadata;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            internal record PeoplePage(string[] Items, string? Next);

            internal interface IPeople
            {
                Task<string> MissingVerb();
                [Get(@"/bad\route")] Task<string> Backslash();
                [Get("/tokens")] Task<string> TwoTokens(CancellationToken first, CancellationToken second);
                [Get("/headers")] Task<string> InvalidHeaders([HeaderCollection] string headers);
                [Multipart, Post("/photo")] Task<string> UntypedPart(object payload);
                [Get(@"/bad\pages"), Paged(Next = "Next")] PagedEnumerable<PeoplePage, string> BadPageRoute([PageToken] string? token);
                [Get("/headers")] Task<string> TwoHeaders([HeaderCollection] IDictionary<string, string> first, [HeaderCollection] IDictionary<string, string> second);
                [Get("/auth")] Task<string> TwoTokensForAuth([Authorize] string first, [Authorize] string second);
                [Post("/people")] Task<string> TwoBodies([Body] string first, [Body] string second);
                [Multipart, Post("/photo")] Task<string> BodyAndParts([Body] string body);
                [Get("/pages")] PagedEnumerable<PeoplePage, string> MissingPageRules([PageToken] string? token);
                [Get("/people")] Task<PeoplePage> WrongMetadata(JsonTypeInfo<int> metadata);
                [Get("/people/{id}")] Task<string> MissingId();
                [Get("/people")] Task<string> MisnamedQuery([Query("client_id")] string clientId);
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);
        ToolingCompilation.RequireNoErrors(compilation);

        const string queryDelimiter = "RF016";
        ImmutableArray<Diagnostic> defaults = await DiagnoseAsync(compilation);
        Check.Require(!Contains(defaults, queryDelimiter), "The delimiter check is opt-in.");
        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(
            compilation.Options.SpecificDiagnosticOptions.SetItem(queryDelimiter, ReportDiagnostic.Warn)));

        RefitInterfaceAnalyzer analyzer = new();
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();
        foreach (DiagnosticDescriptor descriptor in analyzer.SupportedDiagnostics)
        {
            Check.Require(Contains(diagnostics, descriptor.Id), $"The sample must trigger {descriptor.Id}.");
            Console.WriteLine($"{descriptor.Id}: {descriptor.Title}");
        }
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
