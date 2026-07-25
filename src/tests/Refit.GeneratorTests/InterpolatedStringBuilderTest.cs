// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Tests interpolated strings emitted by the generator.</summary>
public sealed class InterpolatedStringBuilderTest
{
    /// <summary>Verifies literal interpolation braces are doubled.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task BuilderEscapesLiteralBraces()
    {
        var actual = new Emitter.InterpolatedStringBuilder()
            .AppendLiteral("{zip}")
            .AppendExpression("value")
            .Build();

        await Assert.That(actual).IsEqualTo("$\"{{zip}}{value}\"");
    }

    /// <summary>Verifies braces in a nested alias produce valid generated code.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public Task NestedAliasContainingBracesCompiles() =>
        AssertGeneratedCodeCompiles(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner
            {
                [AliasAs("{zip}")]
                public string Zip { get; set; } = "";
            }

            public sealed class QueryModel
            {
                public Inner Nested { get; set; } = new();
            }

            public interface IGeneratedClient
            {
                [Get("/query")]
                Task<string> Find([Query] QueryModel query);
            }
            """);

    /// <summary>Verifies braces in a serializer name produce valid generated code.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public Task NestedSerializerNameContainingBracesCompiles() =>
        AssertGeneratedCodeCompiles(
            """
            using System.Text.Json.Serialization;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner
            {
                [JsonPropertyName("{zip}")]
                public string Zip { get; set; } = "";
            }

            public sealed class QueryModel
            {
                public Inner Nested { get; set; } = new();
            }

            public interface IGeneratedClient
            {
                [Get("/query")]
                Task<string> Find([Query] QueryModel query);
            }
            """);

    /// <summary>Verifies braces in query prefixes and delimiters produce valid generated code.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public Task QueryPrefixAndDelimiterContainingBracesCompile() =>
        AssertGeneratedCodeCompiles(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner
            {
                public string Value { get; set; } = "";
            }

            public sealed class QueryModel
            {
                [Query("{delimiter}", "{prefix}")]
                public Inner Nested { get; set; } = new();
            }

            public interface IGeneratedClient
            {
                [Get("/query")]
                Task<string> Find([Query] QueryModel query);
            }
            """);

    /// <summary>Verifies braces in authorization schemes produce valid generated code.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public Task AuthorizationSchemeContainingBracesCompiles() =>
        AssertGeneratedCodeCompiles(
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/query")]
                Task<string> Find([Authorize("{Bearer}")] string token);
            }
            """);

    /// <summary>Runs the generator and verifies its output compiles.</summary>
    /// <param name="source">The source being compiled.</param>
    /// <returns>A task representing the assertion.</returns>
    private static async Task AssertGeneratedCodeCompiles(string source)
    {
        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilationErrors).IsEmpty();
    }
}
