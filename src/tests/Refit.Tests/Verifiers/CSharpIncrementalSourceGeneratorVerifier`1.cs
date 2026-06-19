// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Refit.Tests;

/// <summary>Verifier for incremental source generator tests.</summary>
/// <typeparam name="TIncrementalGenerator">The incremental generator under test.</typeparam>
public static class CSharpIncrementalSourceGeneratorVerifier<TIncrementalGenerator>
    where TIncrementalGenerator : IIncrementalGenerator, new()
{
    /// <summary>Test harness for the incremental source generator.</summary>
    public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, DefaultVerifier>
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

        /// <summary>Gets the source generators.</summary>
        /// <returns>The source generators to run.</returns>
        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return new TIncrementalGenerator().AsSourceGenerator().GetGeneratorType();
        }

        /// <summary>Creates the parse options.</summary>
        /// <returns>The parse options configured with the preview language version.</returns>
        protected override ParseOptions CreateParseOptions()
        {
            var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
            return parseOptions.WithLanguageVersion(LanguageVersion.Preview);
        }
    }
}
