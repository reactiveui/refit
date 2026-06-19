// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Incremental generator tests covering generic interfaces.</summary>
public class GenericTest
{
    /// <summary>The source for a generic Refit interface used as the baseline for the tests.</summary>
    private const string GenericInterface =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IGeneratedInterface<T1>
        {
            [Get("/users")]
            Task<string> Get();
        }
        """;

    /// <summary>The name of the interface declaration replaced in these tests.</summary>
    private const string InterfaceName = "IGeneratedInterface";

    /// <summary>Verifies that renaming a generic type parameter regenerates the source.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RenameGenericTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // rename generic type
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            InterfaceName,
            """
            public interface IGeneratedInterface<T>
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies that adding a generic constraint regenerates the source.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddGenericConstraintDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add generic constraint
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            InterfaceName,
            """
            public interface IGeneratedInterface<T1>
                where T1 : class
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);

        // add new generic constraint
        var compilation3 = TestHelper.ReplaceMemberDeclaration(
            compilation2,
            InterfaceName,
            """
            public interface IGeneratedInterface<T1>
                where T1 : class, new()
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
        var driver3 = driver2.RunGenerators(compilation3);
        await TestHelper.AssertRunReasons(driver3, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies that adding an interface generic constraint regenerates the source.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddObjectGenericConstraintDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add object generic constraint
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            InterfaceName,
            """
            public interface IGeneratedInterface<T1>
                where T1 : IDisposable
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies that adding a second generic type parameter regenerates the source.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddGenericTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add second generic type
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            InterfaceName,
            """
            public interface IGeneratedInterface<T1, T2>
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }
}
