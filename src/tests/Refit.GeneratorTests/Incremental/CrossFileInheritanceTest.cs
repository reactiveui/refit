// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

/// <summary>
/// Locks in the generator's cross-file base-interface dependency: a derived interface's implementation embeds
/// the Refit methods it inherits from its base interfaces, so editing a base interface in a different source
/// file must regenerate the derived interface even though the derived interface's own syntax is untouched. A
/// per-interface parse cache keyed only on the derived interface's syntax would serve stale output here.
/// </summary>
public class CrossFileInheritanceTest
{
    /// <summary>The simple name of the base interface.</summary>
    private const string BaseApiName = "IBaseApi";

    /// <summary>The simple name of the derived interface.</summary>
    private const string DerivedApiName = "IDerivedApi";

    /// <summary>The simple name of the first standalone interface.</summary>
    private const string FirstApiName = "IFirstApi";

    /// <summary>The simple name of the second standalone interface.</summary>
    private const string SecondApiName = "ISecondApi";

    /// <summary>The base Refit interface, placed in its own syntax tree.</summary>
    private const string BaseInterfaceSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IBaseApi
        {
            [Get("/base")]
            Task<string> GetBase();
        }
        """;

    /// <summary>The derived Refit interface inheriting the base, placed in its own syntax tree.</summary>
    private const string DerivedInterfaceSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IDerivedApi : IBaseApi
        {
            [Get("/derived")]
            Task<string> GetDerived();
        }
        """;

    /// <summary>The base interface after a new Refit method is added to it.</summary>
    private const string BaseInterfaceWithAddedMethod =
        """
        public interface IBaseApi
        {
            [Get("/base")]
            Task<string> GetBase();

            [Get("/base-extra")]
            Task<string> GetBaseExtra();
        }
        """;

    /// <summary>A first standalone Refit interface with no inheritance relationship to the second.</summary>
    private const string FirstUnrelatedSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IFirstApi
        {
            [Get("/first")]
            Task<string> GetFirst();
        }
        """;

    /// <summary>A second standalone Refit interface with no inheritance relationship to the first.</summary>
    private const string SecondUnrelatedSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface ISecondApi
        {
            [Get("/second")]
            Task<string> GetSecond();
        }
        """;

    /// <summary>The first standalone interface after a new Refit method is added to it.</summary>
    private const string FirstUnrelatedWithAddedMethod =
        """
        public interface IFirstApi
        {
            [Get("/first")]
            Task<string> GetFirst();

            [Get("/first-extra")]
            Task<string> GetFirstExtra();
        }
        """;

    /// <summary>Base and derived interfaces declared together in a single source file.</summary>
    private const string BaseAndDerivedInOneFileSource =
        """
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IBaseApi
        {
            [Get("/base")]
            Task<string> GetBase();
        }

        public interface IDerivedApi : IBaseApi
        {
            [Get("/derived")]
            Task<string> GetDerived();
        }
        """;

    /// <summary>Adding a Refit method to a base interface in another file regenerates the derived interface.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EditingBaseInterfaceInAnotherFileRegeneratesDerivedInterface()
    {
        var baseTree = CSharpSyntaxTree.ParseText(BaseInterfaceSource, CSharpParseOptions.Default);
        var derivedTree = CSharpSyntaxTree.ParseText(DerivedInterfaceSource, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(baseTree, derivedTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertBuildRefitReasonForInterface(driver1, BaseApiName, IncrementalStepRunReason.New);
        await TestHelper.AssertBuildRefitReasonForInterface(driver1, DerivedApiName, IncrementalStepRunReason.New);

        var compilation2 = TestHelper.ReplaceTypeDeclarationInTree(
            compilation1,
            baseTree,
            BaseApiName,
            BaseInterfaceWithAddedMethod);

        var driver2 = driver1.RunGenerators(compilation2);

        // The edited base regenerates, and the derived interface regenerates because it embeds the base's Refit
        // methods even though the derived interface's own file was not touched.
        await TestHelper.AssertBuildRefitReasonForInterface(driver2, BaseApiName, IncrementalStepRunReason.Modified);
        await TestHelper.AssertBuildRefitReasonForInterface(
            driver2,
            DerivedApiName,
            IncrementalStepRunReason.Modified);
    }

    /// <summary>Editing an unrelated interface in another file leaves the other interface's output unchanged.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EditingUnrelatedInterfaceInAnotherFileLeavesOtherInterfaceUnchanged()
    {
        var firstTree = CSharpSyntaxTree.ParseText(FirstUnrelatedSource, CSharpParseOptions.Default);
        var secondTree = CSharpSyntaxTree.ParseText(SecondUnrelatedSource, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(firstTree, secondTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertBuildRefitReasonForInterface(driver1, FirstApiName, IncrementalStepRunReason.New);
        await TestHelper.AssertBuildRefitReasonForInterface(driver1, SecondApiName, IncrementalStepRunReason.New);

        var compilation2 = TestHelper.ReplaceTypeDeclarationInTree(
            compilation1,
            firstTree,
            FirstApiName,
            FirstUnrelatedWithAddedMethod);

        var driver2 = driver1.RunGenerators(compilation2);

        // The edited interface regenerates; the untouched, unrelated interface's output is reused.
        await TestHelper.AssertBuildRefitReasonForInterface(driver2, FirstApiName, IncrementalStepRunReason.Modified);
        await TestHelper.AssertBuildRefitReasonForInterface(
            driver2,
            SecondApiName,
            IncrementalStepRunReason.Unchanged);
    }

    /// <summary>Adding a Refit method to a base interface in the same file regenerates the derived interface.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task EditingBaseInterfaceInSameFileRegeneratesDerivedInterface()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(BaseAndDerivedInOneFileSource, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertBuildRefitReasonForInterface(driver1, BaseApiName, IncrementalStepRunReason.New);
        await TestHelper.AssertBuildRefitReasonForInterface(driver1, DerivedApiName, IncrementalStepRunReason.New);

        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            BaseApiName,
            BaseInterfaceWithAddedMethod);

        var driver2 = driver1.RunGenerators(compilation2);

        // Editing the base's Refit surface must flow into the derived interface's embedded base methods.
        await TestHelper.AssertBuildRefitReasonForInterface(driver2, BaseApiName, IncrementalStepRunReason.Modified);
        await TestHelper.AssertBuildRefitReasonForInterface(
            driver2,
            DerivedApiName,
            IncrementalStepRunReason.Modified);
    }
}
