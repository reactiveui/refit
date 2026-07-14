// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Focused tests that drive request parsing and inline emission branches to full coverage.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>The generated implementation source hint name used by these tests.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>Verifies a template with repeated and multiple placeholders is parsed and generated inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DuplicateAndMultiplePathPlaceholdersGenerateInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a/{id}/b/{id}/c/{name}")]
                Task<string> Get(int id, string name);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("BuildRequestPath");
    }

    /// <summary>Verifies dynamic header parameters with valid, null, and whitespace names are parsed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeaderParametersCoverNameValidation()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/valid")]
                Task<string> Valid([Header("X-Valid")] string value);

                [Get("/missing")]
                Task<string> Missing([Header(null)] string value);

                [Get("/blank")]
                Task<string> Blank([Header("   ")] string value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("SetHeader");
    }

    /// <summary>Verifies an HTTP method attribute with an unresolved path argument degrades gracefully.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnresolvedHttpAttributeArgumentProducesEmptyPath()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get(UndefinedRoute)]
                Task<string> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies a header parameter with an unresolved name argument degrades gracefully.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnresolvedHeaderArgumentFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/values")]
                Task<string> Get([Header(UndefinedHeader)] string value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies repeated custom parameter attributes emit a grouped attribute provider with all argument kinds.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RepeatedPathParameterAttributesEmitGroupedProvider()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
            public sealed class TagAttribute : Attribute
            {
                public TagAttribute(Type marker, int[] codes, DayOfWeek day)
                {
                }
            }

            public interface IGeneratedClient
            {
                [Get("/things/{id}")]
                Task<string> Get(
                    [Tag(typeof(int), new[] { 1, 2 }, DayOfWeek.Monday)]
                    [Tag(typeof(string), new[] { 3 }, DayOfWeek.Friday)]
                    int id);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("GeneratedParameterAttributeProvider");
        await Assert.That(generated).Contains("new global::RefitGeneratorTest.TagAttribute(typeof(int), new[] { 1, 2 }");
        await Assert.That(generated).Contains("new global::RefitGeneratorTest.TagAttribute(typeof(string), new[] { 3 }");
        await Assert.That(generated).Contains("(global::System.DayOfWeek)");
    }

    /// <summary>Verifies a parameter attribute carrying a null argument renders the null literal.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterAttributeWithNullArgumentRendersNullLiteral()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/values/{value}")]
                Task<string> Get([AliasAs(null)] int value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("AliasAsAttribute(null)");
    }

    /// <summary>Verifies a non-<c>[Encoded]</c> round-trip catch-all path parameter of any type generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTripCatchAllPathGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public readonly record struct RepoPath(string Value)
            {
                public override string ToString() => Value;
            }

            public interface IGeneratedClient
            {
                [Get("/repos/{**value}/contents")] Task<string> Get(RepoPath value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("RoundTripEscapePath");
    }

    /// <summary>Verifies an <c>[Authorize]</c> parameter generates an inline Authorization header with its scheme.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AuthorizeParameterGeneratesInlineAuthorizationHeader()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")] Task<string> DefaultScheme([Authorize] string token);
                [Get("/b")] Task<string> ExplicitScheme([Authorize("Token")] string token);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("\"Authorization\"");
        await Assert.That(generated).Contains("\"Bearer \"");
        await Assert.That(generated).Contains("\"Token \"");
    }

    /// <summary>Verifies a dotted path placeholder whose intermediate segment property does not exist falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathWithMissingNestedPropertyFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner { public string Slug { get; set; } }

            public sealed class Outer { public Inner Inner { get; set; } }

            public interface IGeneratedClient
            {
                [Get("/x/{route.Inner.Missing}")]
                Task<string> Get(Outer route);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a dotted path placeholder whose final property is a complex type falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DottedPathWithComplexFinalPropertyFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner { public string Slug { get; set; } }

            public sealed class Outer { public Inner Inner { get; set; } }

            public interface IGeneratedClient
            {
                [Get("/x/{route.Inner}")]
                Task<string> Get(Outer route);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }
}
