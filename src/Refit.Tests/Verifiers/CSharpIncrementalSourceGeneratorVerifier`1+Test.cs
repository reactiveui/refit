using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Refit.Tests;

public static partial class CSharpIncrementalSourceGeneratorVerifier<TIncrementalGenerator>
    where TIncrementalGenerator : IIncrementalGenerator, new()
{
    public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, DefaultVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add(
                (solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(
                            CSharpVerifierHelper.NullableWarnings
                        )
                    );
                    solution = solution.WithProjectCompilationOptions(
                        projectId,
                        compilationOptions
                    );

                    return solution;
                }
            );
        }

        /// <summary>
        /// Gets the source generators.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return new TIncrementalGenerator().AsSourceGenerator().GetGeneratorType();
        }

        /// <summary>
        /// Creates the parse options.
        /// </summary>
        /// <returns></returns>
        protected override ParseOptions CreateParseOptions()
        {
            var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
            return parseOptions.WithLanguageVersion(LanguageVersion.Preview);
        }
    }
}
