// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Verifies how the generator annotates methods that fall back to the reflection request builder: it mirrors
/// the interface member's <c>[RequiresUnreferencedCode]</c>/<c>[RequiresDynamicCode]</c> when present, and otherwise
/// suppresses the unactionable IL2026/IL3050 (issue #2200).</summary>
public sealed class ReflectionFallbackAnnotationTests
{
    /// <summary>A method with a dynamic query-map (<c>object</c>) parameter is not inline-eligible, so it falls back to reflection.</summary>
    private const string UnannotatedSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace ConsumerNs;

        public interface IFallbackApi
        {
            [Get("/thing")]
            Task<string> GetThing(object filters);
        }
        """;

    /// <summary>The same fallback method, but the interface member declares the trim/AOT annotations.</summary>
    private const string AnnotatedSource =
        """
        using System.Diagnostics.CodeAnalysis;
        using System.Threading.Tasks;
        using Refit;

        namespace ConsumerNs;

        public interface IFallbackApi
        {
            [Get("/thing")]
            [RequiresUnreferencedCode("Uses the reflection request builder.")]
            [RequiresDynamicCode("Uses the reflection request builder.")]
            Task<string> GetThing(object filters);
        }
        """;

    /// <summary>Verifies an unannotated fallback method suppresses the IL2026/IL3050 the consumer cannot act on.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnannotatedFallbackMethodSuppressesTrimWarnings()
    {
        var generated = RunAndConcatenate(UnannotatedSource);

        await Assert.That(generated).Contains("\"IL2026\"");
        await Assert.That(generated).Contains("\"IL3050\"");
        await Assert.That(generated).DoesNotContain("RequiresUnreferencedCode(");
        await Assert.That(generated).DoesNotContain("RequiresDynamicCode(");
    }

    /// <summary>Verifies an annotated fallback method mirrors the interface's trim/AOT attributes rather than suppressing,
    /// so it satisfies the interface/implementation annotation-matching rule and propagates the contract to callers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AnnotatedFallbackMethodMirrorsTrimAttributes()
    {
        var generated = RunAndConcatenate(AnnotatedSource);

        await Assert.That(generated).Contains("global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(");
        await Assert.That(generated).Contains("global::System.Diagnostics.CodeAnalysis.RequiresDynamicCode(");
        await Assert.That(generated).DoesNotContain("\"IL2026\"");
        await Assert.That(generated).DoesNotContain("\"IL3050\"");
    }

    /// <summary>Runs the generator over the source and concatenates every generated source for text assertions.</summary>
    /// <param name="source">The consumer interface source.</param>
    /// <returns>The concatenation of every generated source.</returns>
    private static string RunAndConcatenate(string source)
    {
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        return string.Join("\n", result.GeneratedSources.Values);
    }
}
