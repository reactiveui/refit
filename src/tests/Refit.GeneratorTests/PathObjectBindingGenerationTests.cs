// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Verifies dotted <c>{param.Property}</c> path binding, including how residual properties flatten into the query.</summary>
public sealed class PathObjectBindingGenerationTests
{
    /// <summary>The generated client hint name.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflection request-builder call emitted when a method falls back.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>Verifies that dotted <c>{param.Property}</c> path placeholders are built inline, not via reflection.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratesInlineForDottedPathPlaceholders()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public record class Data(string Value);

            public interface IGeneratedClient
            {
                [Get("/a/{data.Value}")]
                Task Sample(Data data);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("@data.Value");
    }

    /// <summary>Verifies a property not bound to a dotted path placeholder flattens into the query string inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratesInlineForDottedPathWithResidualQueryProperty()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public record class Data(string Value, string Note);

            public interface IGeneratedClient
            {
                [Get("/a/{data.Value}")]
                Task Sample(Data data);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("@data.Value");
        await Assert.That(generated).Contains("@data.Note");
    }

    /// <summary>Verifies a residual property whose shape cannot flatten inline falls the whole parameter back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UsesFallbackForDottedPathWithUnsupportedResidualProperty()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public record class Data(string Value)
            {
                public object Extra { get; init; }
            }

            public interface IGeneratedClient
            {
                [Get("/a/{data.Value}")]
                Task Sample(Data data);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }
}
