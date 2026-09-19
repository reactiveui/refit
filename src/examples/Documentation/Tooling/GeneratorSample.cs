// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias GeneratorTooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using InterfaceStubGeneratorV2 = GeneratorTooling::Refit.Generator.InterfaceStubGeneratorV2;

namespace Refit.Documentation.Tooling;

/// <summary>Runs the shipped generator and checks the resulting compilation.</summary>
internal static class GeneratorSample
{
    /// <summary>Demonstrates generator initialization through a Roslyn driver.</summary>
    internal static void Run()
    {
        const string source = """
            internal interface IPeople
            {
                [Refit.Get("/people/{id}")]
                System.Threading.Tasks.Task<string> GetAsync(int id);
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);

        InterfaceStubGeneratorV2 generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: new(LanguageVersion.CSharp14));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation generated, out _);
        GeneratorDriverRunResult result = driver.GetRunResult();
        Console.WriteLine(!result.GeneratedTrees.IsEmpty); // True

        ToolingCompilation.RequireNoErrors(generated);
        foreach (Diagnostic diagnostic in result.Diagnostics)
        {
            Check.Require(diagnostic.Severity != DiagnosticSeverity.Error, "Generator reported an error.");
        }

        Check.Require(!result.GeneratedTrees.IsEmpty, "Generator must emit a client.");
        INamedTypeSymbol? contract = generated.GetTypeByMetadataName("IPeople");
        INamedTypeSymbol? implementation = generated.GetTypeByMetadataName("Refit.Implementation.GeneratedToolingDemonstration+IPeople");
        Check.Require(contract is not null && implementation is not null, "Generation emits the expected interface implementation identity.");
        bool implementsContract = false;
        foreach (INamedTypeSymbol implemented in implementation!.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(implemented, contract))
            {
                continue;
            }

            implementsContract = true;
            break;
        }

        Check.Require(implementsContract, "The emitted client implements the actual compiler input interface.");
    }
}
