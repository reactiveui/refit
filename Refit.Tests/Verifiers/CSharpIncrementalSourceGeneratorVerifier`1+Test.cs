using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Refit.Tests
{
    public static partial class CSharpIncrementalSourceGeneratorVerifier<TIncrementalGenerator>
        where TIncrementalGenerator : IIncrementalGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, XUnitVerifier>
        {
            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });
            }

            protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
            {
                yield return new TIncrementalGenerator().AsSourceGenerator();
            }

            protected override ParseOptions CreateParseOptions()
            {
                var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
                return parseOptions.WithLanguageVersion(LanguageVersion.Preview);
            }
        }
    }
}
