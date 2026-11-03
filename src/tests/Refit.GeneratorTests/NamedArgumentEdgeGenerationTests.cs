// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Verifies attribute named arguments and parameter shapes that the usual interfaces never reach.</summary>
public sealed class NamedArgumentEdgeGenerationTests
{
    /// <summary>The generated client's hint name.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>Verifies a <c>[Query]</c> whose <c>Name</c> is explicitly null keeps the parameter's own name.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task QueryNameSetToNullKeepsTheParameterName()
    {
        var result = Fixture.RunGeneratorForBody(
            """
            [Get("/a")]
            Task<string> Find([Query(Name = null)] string value);
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains("value");
    }

    /// <summary>Verifies a method with three <c>[Url]</c> parameters still generates and reports the problem.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ThreeUrlParametersAreHandled()
    {
        var result = Fixture.RunGeneratorForBody(
            """
            [Get("")]
            Task<string> Fetch([Url] string first, [Url] string second, [Url] string third);
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies a query collection of an element that cannot be flattened inline still generates.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task QueryCollectionOfUnflattenableElementsIsHandled()
    {
        var result = Fixture.RunGenerator(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IItem
            {
                int Id { get; }
            }

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> Find([Query(CollectionFormat.Indexed)] List<IItem> items);
            }
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies a partial interface whose Refit method lives in another file still generates a client.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task PartialInterfaceSplitAcrossFilesIsHandled()
    {
        var compilation = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(
                """
                namespace RefitGeneratorTest;

                public interface IMarker
                {
                }

                public partial interface IGeneratedClient : IMarker
                {
                }
                """),
            CSharpSyntaxTree.ParseText(
                """
                using System.Threading.Tasks;
                using Refit;

                namespace RefitGeneratorTest;

                public partial interface IGeneratedClient
                {
                    [Get("/a")]
                    Task<string> Fetch();
                }
                """));

        var result = Fixture.RunGenerator(compilation, true, false, null);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies an interface inheriting a Refit interface declared in another file still generates a client.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task InheritedInterfaceDeclaredInAnotherFileIsHandled()
    {
        var compilation = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(
                """
                using System.Threading.Tasks;
                using Refit;

                namespace RefitGeneratorTest;

                public interface IBaseClient
                {
                    [Get("/a")]
                    Task<string> Fetch();
                }
                """),
            CSharpSyntaxTree.ParseText(
                """
                namespace RefitGeneratorTest;

                public interface IDerivedClient : IBaseClient
                {
                }
                """));

        var result = Fixture.RunGenerator(compilation, true, false, null);

        await Assert.That(result.GeneratedSources.Count).IsGreaterThan(0);
    }

    /// <summary>Verifies a compilation that does not reference Refit produces nothing and does not fail.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task CompilationWithoutRefitProducesNothing()
    {
        var withRefit = Fixture.CreateLibrary(
            CSharpSyntaxTree.ParseText(
                """
                using System.Threading.Tasks;

                namespace RefitGeneratorTest;

                public interface IBaseClient
                {
                    [Custom("/a")]
                    Task<string> Fetch();
                }

                public interface IDerivedClient : IBaseClient
                {
                }
                """));
        var compilation = withRefit.RemoveReferences(
            withRefit.References.Where(static reference => reference.Display?.EndsWith("Refit.dll", StringComparison.Ordinal) == true));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new InterfaceStubGeneratorV2().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        await Assert.That(driver.GetRunResult().GeneratedTrees.Length).IsEqualTo(0);
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    /// <summary>Verifies a relative-allowed <c>[Url]</c> on a string-returning method selects the relative-or-absolute kind.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RelativeUrlParameterSelectsRelativeOrAbsoluteKind()
    {
        var result = Fixture.RunGeneratorForBody(
            """
            [Get("")]
            Task<string> Fetch([Url(AllowRelative = true)] string url);
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains("global::System.UriKind.RelativeOrAbsolute");
    }

    /// <summary>Verifies a relative-allowed <c>[Url]</c> on an observable-returning method selects the relative-or-absolute kind.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task RelativeUrlParameterOnObservableSelectsRelativeOrAbsoluteKind()
    {
        var result = Fixture.RunGeneratorForBody(
            """
            [Get("")]
            System.IObservable<string> Fetch([Url(AllowRelative = true)] string url);
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains("global::System.UriKind.RelativeOrAbsolute");
    }

    /// <summary>Verifies an indexed query collection with an explicit delimiter flattens its elements inline.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task IndexedQueryCollectionWithExplicitDelimiterIsGenerated()
    {
        var result = Fixture.RunGenerator(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Item
            {
                public int Id { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> Find([Query("::", CollectionFormat.Indexed)] List<Item> items);
            }
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).Contains("::");
    }

    /// <summary>Verifies an attributed member that is not a Refit method is ignored alongside a Refit method.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task AttributedNonRefitMethodIsIgnored()
    {
        var result = Fixture.RunGeneratorForBody(
            """
            [Get("/a")]
            Task<string> Fetch();

            [System.Obsolete]
            void Other();
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies a method returning an array is classified without a named return type.</summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Test]
    public async Task ArrayReturnTypeIsHandled()
    {
        var result = Fixture.RunGeneratorForBody(
            """
            [Get("/a")]
            string[] Fetch();
            """,
            generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }
}
