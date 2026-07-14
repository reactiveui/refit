// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>
/// Verifies how the source generator handles a <c>[Url]</c> parameter: a string or <see cref="Uri"/> value with an
/// empty path template generates an absolute-URI request inline, while unsupported shapes fall back to the reflection
/// request builder, whose validation throws.
/// </summary>
public sealed class UrlAttributeGenerationTests
{
    /// <summary>The generated client hint name shared by every scenario.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflection request-builder call that marks a method as falling back rather than inlined.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>The runner call that validates a <c>[Url]</c> value as an absolute URI.</summary>
    private const string RequireAbsoluteUrlCall = "global::Refit.GeneratedRequestRunner.RequireAbsoluteUrl";

    /// <summary>Verifies a string <c>[Url]</c> parameter builds an absolute-URI request inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StringUrlParameterGeneratesAbsoluteUriInline()
    {
        var generated = Generate("[Get(\"\")] Task<string> Fetch([Url] string url);");

        await Assert.That(generated).Contains(RequireAbsoluteUrlCall);
        await Assert.That(generated).Contains("global::System.UriKind.Absolute");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a <see cref="Uri"/> <c>[Url]</c> parameter builds an absolute-URI request inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UriUrlParameterGeneratesAbsoluteUriInline()
    {
        var generated = Generate("[Get(\"\")] Task<string> Fetch([Url] System.Uri url);");

        await Assert.That(generated).Contains(RequireAbsoluteUrlCall);
        await Assert.That(generated).Contains("global::System.UriKind.Absolute");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a <c>[Query]</c> parameter is appended to the absolute URL through the query builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryParameterAppendsToAbsoluteUrlInline()
    {
        var generated = Generate("[Get(\"\")] Task<string> Fetch([Url] string url, [Query] string token);");

        await Assert.That(generated).Contains(RequireAbsoluteUrlCall);
        await Assert.That(generated).Contains("GeneratedQueryStringBuilder");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a <c>[Url]</c> method that also declares a path template falls back to the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlParameterWithPathTemplateFallsBack()
    {
        var generated = Generate("[Get(\"/foo\")] Task<string> Fetch([Url] string url);");

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(RequireAbsoluteUrlCall);
    }

    /// <summary>Verifies a <c>[Url]</c> parameter that is neither a string nor a <see cref="Uri"/> falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonStringOrUriUrlParameterFallsBack()
    {
        var generated = Generate("[Get(\"\")] Task<string> Fetch([Url] int url);");

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(RequireAbsoluteUrlCall);
    }

    /// <summary>Verifies a method with more than one <c>[Url]</c> parameter falls back to the reflection builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipleUrlParametersFallBack()
    {
        var generated = Generate("[Get(\"\")] Task<string> Fetch([Url] string first, [Url] string second);");

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(RequireAbsoluteUrlCall);
    }

    /// <summary>Runs the generator over a single-method interface and returns the generated client source.</summary>
    /// <param name="methodDeclaration">The interface method declaration to generate from.</param>
    /// <returns>The generated client source.</returns>
    private static string Generate(string methodDeclaration)
    {
        var source =
            $$"""
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                {{methodDeclaration}}
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        return result.GeneratedSources[GeneratedClientHintName];
    }
}
