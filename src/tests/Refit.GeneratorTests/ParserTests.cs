// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Tests empty generation passes and deterministic extern alias ordering.</summary>
public sealed class ParserTests
{
    /// <summary>Verifies repeated interface names receive distinct file names without losing their methods.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubs_DuplicateSimpleNames_PreservesEveryInterface(CancellationToken cancellationToken)
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            using Refit;
            using System.Threading.Tasks;
            namespace First { public interface IApi { [Get("/first")] Task<string> First(); } }
            namespace Second { public interface IApi { [Get("/second")] Task<string> Second(); } }
            namespace Third { public interface IApi { [Get("/third")] Task<string> Third(); } }
            """,
            cancellationToken: cancellationToken);
        var root = await tree.GetRootAsync(cancellationToken);
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToImmutableArray();
        var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToImmutableArray();

        var (diagnostics, model) = Parser.GenerateInterfaceStubs(
            Fixture.CreateLibrary(tree),
            null,
            generatedRequestBuilding: true,
            emitGeneratedCodeMarkers: true,
            methods,
            interfaces,
            cancellationToken);

        string[] fileNames = ["IApi.g.cs", "IApi1.g.cs", "IApi11.g.cs"];
        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(model.Interfaces.Select(static item => item.FileName).SequenceEqual(fileNames)).IsTrue();
        await Assert.That(model.Interfaces.SelectMany(static item => item.Members.RefitMethods).Count()).IsEqualTo(methods.Length);
    }

    /// <summary>Verifies a referenced Refit assembly alone does not cause interface generation.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenerateInterfaceStubs_NoCandidates_ReturnsEmptyModel(CancellationToken cancellationToken)
    {
        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText("public interface IUnused { }", cancellationToken: cancellationToken));

        var (diagnostics, model) = Parser.GenerateInterfaceStubs(
            compilation,
            "Client",
            generatedRequestBuilding: false,
            emitGeneratedCodeMarkers: false,
            [],
            [],
            cancellationToken);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(model.Interfaces).IsEmpty();
        await Assert.That(model.GeneratedRequestBuilding).IsFalse();
        await Assert.That(model.EmitGeneratedCodeMarkers).IsFalse();
    }

    /// <summary>Verifies aliases are sorted ordinally without mutating the input set.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BuildSortedExternAliases_MixedCase_SortsOrdinally()
    {
        HashSet<string> aliases = ["zeta", "alpha", "Alpha"];

        var result = Parser.BuildSortedExternAliases(aliases);

        string[] expected = ["Alpha", "alpha", "zeta"];
        await Assert.That(result.SequenceEqual(expected)).IsTrue();
        await Assert.That(aliases.Count).IsEqualTo(expected.Length);
    }
}
