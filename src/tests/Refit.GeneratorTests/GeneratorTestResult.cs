// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Refit.GeneratorTests;

/// <summary>Wraps the result of running the Refit source generator against a test compilation.</summary>
public sealed class GeneratorTestResult
{
    /// <summary>Initializes a new instance of the <see cref="GeneratorTestResult"/> class.</summary>
    /// <param name="driver">The generator driver after execution.</param>
    /// <param name="outputCompilation">The output compilation containing generated source.</param>
    /// <param name="generatorDiagnostics">The diagnostics produced by the generator driver.</param>
    public GeneratorTestResult(
        GeneratorDriver driver,
        Compilation outputCompilation,
        ImmutableArray<Diagnostic> generatorDiagnostics)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(outputCompilation);

        Driver = driver;
        OutputCompilation = outputCompilation;
        GeneratorDiagnostics = generatorDiagnostics;
        CompilationErrors =
        [
            .. outputCompilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        ];
        GeneratedSources = BuildGeneratedSources(driver);
    }

    /// <summary>Gets the generator driver after execution.</summary>
    public GeneratorDriver Driver { get; }

    /// <summary>Gets the output compilation containing generated source.</summary>
    public Compilation OutputCompilation { get; }

    /// <summary>Gets the diagnostics produced by the generator driver.</summary>
    public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

    /// <summary>Gets compilation diagnostics with error severity.</summary>
    public ImmutableArray<Diagnostic> CompilationErrors { get; }

    /// <summary>Gets generated source text by hint name.</summary>
    public IReadOnlyDictionary<string, string> GeneratedSources { get; }

    /// <summary>Gets a value indicating whether the generated output compiles without errors.</summary>
    public bool CompilesWithoutErrors => CompilationErrors.Length == 0;

    /// <summary>Builds generated source text by hint name.</summary>
    /// <param name="driver">The generator driver after execution.</param>
    /// <returns>The generated source map.</returns>
    private static Dictionary<string, string> BuildGeneratedSources(GeneratorDriver driver)
    {
        var generatedSources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var result in driver.GetRunResult().Results)
        {
            if (result.GeneratedSources.IsDefaultOrEmpty)
            {
                continue;
            }

            foreach (var source in result.GeneratedSources)
            {
                generatedSources[source.HintName] = source.SourceText.ToString();
            }
        }

        return generatedSources;
    }
}
