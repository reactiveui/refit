// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Refit.Tests;

/// <summary>Verifier for source generator tests.</summary>
/// <typeparam name="TSourceGenerator">The source generator under test.</typeparam>
public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : ISourceGenerator, new()
{
    /// <summary>Test harness for the source generator.</summary>
    public class Test : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
    {
        /// <summary>Initializes a new instance of the <see cref="Test"/> class.</summary>
        public Test()
        {
            SolutionTransforms.Add(
                (solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId)!.CompilationOptions;
                    compilationOptions = compilationOptions!.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(
                            CSharpVerifierHelper.NullableWarnings));
                    return solution.WithProjectCompilationOptions(
                        projectId,
                        compilationOptions);
                });
        }
    }
}
