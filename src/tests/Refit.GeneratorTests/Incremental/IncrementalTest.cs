// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Tests the incremental behavior of the Refit source generator.</summary>
public class IncrementalTest
{
    /// <summary>The default interface source used as the test baseline.</summary>
    private const string DefaultInterface =
        """
        #nullable enabled
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IGitHubApi
        {
            [Get("/users/{user}")]
            Task<string> GetUser(string user);
        }
        """;

    /// <summary>Verifies that adding an unrelated type does not regenerate output.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddUnrelatedTypeDoesntRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    /// <summary>Verifies that an inconsequential change does not regenerate output.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task SmallChangeDoesntRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // update syntax tree by replacing interface with itself
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGitHubApi",
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    /// <summary>Verifies that adding a new member does regenerate output.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddNewMemberDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add unrelated member, don't change the method
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGitHubApi",
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);

                private record Temp();
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }
}
