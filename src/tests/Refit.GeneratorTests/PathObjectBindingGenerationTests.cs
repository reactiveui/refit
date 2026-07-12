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

    /// <summary>The inline path fragment emitted when the <c>data.Value</c> placeholder binds.</summary>
    private const string BoundValuePlaceholder = "@data.Value";

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
        await Assert.That(generated).Contains(BoundValuePlaceholder);
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
        await Assert.That(generated).Contains(BoundValuePlaceholder);
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

    /// <summary>Verifies a separate scalar path placeholder alongside a dotted binding does not bind to the object
    /// parameter, while the dotted property still binds inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathIgnoresForeignPlaceholderAndBindsInline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public record class Data(string Value, string Note);

            public interface IGeneratedClient
            {
                [Get("/a/{data.Value}/{id}")]
                Task Sample(Data data, int id);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(BoundValuePlaceholder);
    }

    /// <summary>Verifies a dotted placeholder binds a property inherited from a base class.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathBindsInheritedPropertyInline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class BaseData { public string Value { get; set; } = ""; }

            public class Data : BaseData { }

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
        await Assert.That(generated).Contains(BoundValuePlaceholder);
    }

    /// <summary>Verifies a dotted placeholder naming a non-existent property falls the whole parameter back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathWithUnknownPropertyFallsBack()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public record class Data(string Value);

            public interface IGeneratedClient
            {
                [Get("/a/{data.Missing}")]
                Task Sample(Data data);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a dotted placeholder with an empty property name binds nothing and falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathWithEmptyPropertyNameFallsBack()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public record class Data(string Value);

            public interface IGeneratedClient
            {
                [Get("/a/{data.}")]
                Task Sample(Data data);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a dotted placeholder bound to an enum property with duplicate constants renders through the
    /// formatter while still binding inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathBindsDuplicateEnumPropertyInline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Duplicated { First = 1, Alias = 1 }

            public record class Data(Duplicated Code);

            public interface IGeneratedClient
            {
                [Get("/a/{data.Code}")]
                Task Sample(Data data);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("@data.Code");
    }
}
